using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace SelectiveBuild.Services
{
    public class AppInfo
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public string CwprojPath { get; set; }
        public string AppPath { get; set; }
        /// <summary>Position of the project in the .sln (declaration order), used as the IDE's tie-breaker.</summary>
        public int SolutionIndex { get; set; }

        /// <summary>GUIDs from the .sln's ProjectSection(ProjectDependencies) (what the Project Dependency Editor writes).</summary>
        public List<string> DependsOnGuids { get; set; }

        /// <summary>GUIDs of solution projects referenced by &lt;ProjectReference&gt; items in the .cwproj.</summary>
        public List<string> ReferencedGuids { get; set; }

        /// <summary>True if the .cwproj has any &lt;ProjectReference&gt;, resolved or not (mirrors the IDE's HasReferences).</summary>
        public bool HasProjectReferences { get; set; }

        public AppInfo()
        {
            DependsOnGuids = new List<string>();
            ReferencedGuids = new List<string>();
        }
    }

    /// <summary>
    /// Parses the active .sln to find Clarion app projects (type GUID 12B76EC0-1D7B-4FA7-A7D0-C524288B48A1),
    /// resolves each one's .app file on disk, and reads the ProjectDependencies the IDE's
    /// "Project Dependencies" dialog writes into the .sln, so builds can respect that order.
    /// </summary>
    public class SolutionService
    {
        private const string ClarionProjectTypeGuid = "12B76EC0-1D7B-4FA7-A7D0-C524288B48A1";

        private static readonly Regex ProjectLineRegex = new Regex(
            "^Project\\(\"\\{(?<type>[0-9A-Fa-f-]+)\\}\"\\)\\s*=\\s*\"(?<name>[^\"]+)\"\\s*,\\s*\"(?<path>[^\"]+)\"\\s*,\\s*\"\\{(?<guid>[0-9A-Fa-f-]+)\\}\"",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DependencyLineRegex = new Regex(
            "^\\s*\\{(?<guid>[0-9A-Fa-f-]+)\\}\\s*=\\s*\\{[0-9A-Fa-f-]+\\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the currently open solution's .sln path from the IDE via reflection
        /// (ICSharpCode.SharpDevelop.Project.ProjectService.OpenSolution), avoiding a hard
        /// compile-time dependency that could break across Clarion IDE builds.
        /// </summary>
        public string GetActiveSolutionPath()
        {
            try
            {
                var projectServiceType = Type.GetType(
                    "ICSharpCode.SharpDevelop.Project.ProjectService, ICSharpCode.SharpDevelop");
                if (projectServiceType == null) return null;

                var openSolutionProp = projectServiceType.GetProperty("OpenSolution", BindingFlags.Public | BindingFlags.Static);
                if (openSolutionProp == null) return null;
                var solution = openSolutionProp.GetValue(null, null);
                if (solution == null) return null;

                var fileNameProp = solution.GetType().GetProperty("FileName");
                if (fileNameProp == null) return null;
                return fileNameProp.GetValue(solution, null) as string;
            }
            catch
            {
                return null;
            }
        }

        public List<AppInfo> GetApps(string slnPath)
        {
            var result = new List<AppInfo>();
            if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
                return result;

            string solutionDir = Path.GetDirectoryName(slnPath);
            string[] lines = File.ReadAllLines(slnPath);

            AppInfo current = null;
            bool inDependenciesSection = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                var m = ProjectLineRegex.Match(line);
                if (m.Success)
                {
                    inDependenciesSection = false;
                    current = null;

                    if (string.Equals(m.Groups["type"].Value, ClarionProjectTypeGuid, StringComparison.OrdinalIgnoreCase))
                    {
                        string name = m.Groups["name"].Value;
                        string relPath = m.Groups["path"].Value;
                        string cwprojPath = Path.Combine(solutionDir, relPath);

                        current = new AppInfo
                        {
                            Name = name,
                            Guid = m.Groups["guid"].Value,
                            CwprojPath = cwprojPath,
                            AppPath = ResolveAppFile(solutionDir, name),
                            SolutionIndex = result.Count
                        };
                        result.Add(current);
                    }
                    continue;
                }

                if (trimmed.StartsWith("EndProject", StringComparison.OrdinalIgnoreCase))
                {
                    inDependenciesSection = false;
                    current = null;
                    continue;
                }

                if (current == null) continue;

                if (trimmed.StartsWith("ProjectSection(ProjectDependencies)", StringComparison.OrdinalIgnoreCase))
                {
                    inDependenciesSection = true;
                    continue;
                }

                if (trimmed.StartsWith("EndProjectSection", StringComparison.OrdinalIgnoreCase))
                {
                    inDependenciesSection = false;
                    continue;
                }

                if (inDependenciesSection)
                {
                    var dm = DependencyLineRegex.Match(line);
                    if (dm.Success)
                    {
                        current.DependsOnGuids.Add(dm.Groups["guid"].Value);
                    }
                }
            }

            ResolveProjectReferences(result);

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        /// <summary>
        /// Reads each app's .cwproj &lt;ProjectReference&gt; items and maps them to solution GUIDs,
        /// by referenced path first and then by the reference's &lt;Project&gt; GUID.
        /// </summary>
        private void ResolveProjectReferences(List<AppInfo> apps)
        {
            var byPath = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
            var byGuid = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in apps)
            {
                string full = SafeFullPath(app.CwprojPath);
                if (full != null) byPath[full] = app;
                if (!string.IsNullOrEmpty(app.Guid)) byGuid[app.Guid] = app;
            }

            foreach (var app in apps)
            {
                if (!File.Exists(app.CwprojPath)) continue;
                XmlDocument doc = new XmlDocument();
                try { doc.Load(app.CwprojPath); }
                catch { continue; }

                string projDir = Path.GetDirectoryName(app.CwprojPath);
                foreach (XmlNode node in doc.GetElementsByTagName("ProjectReference"))
                {
                    var element = node as XmlElement;
                    if (element == null) continue;
                    app.HasProjectReferences = true;

                    AppInfo target = null;
                    string include = element.GetAttribute("Include");
                    if (!string.IsNullOrEmpty(include))
                    {
                        string full = SafeFullPath(Path.Combine(projDir, include));
                        if (full != null) byPath.TryGetValue(full, out target);
                    }
                    if (target == null)
                    {
                        foreach (XmlNode child in element.ChildNodes)
                        {
                            if (child.LocalName != "Project") continue;
                            string guid = child.InnerText.Trim().Trim('{', '}');
                            byGuid.TryGetValue(guid, out target);
                            break;
                        }
                    }
                    if (target != null) app.ReferencedGuids.Add(target.Guid);
                }
            }
        }

        private static string SafeFullPath(string path)
        {
            try { return Path.GetFullPath(path); }
            catch { return null; }
        }

        /// <summary>
        /// Orders the selected apps exactly like Clarion's Project Dependency Editor
        /// (SoftVelocity.Common.ProjectDependencyEditorHelper.Refresh/AddProject in CommonSources.dll):
        /// first every app with no references/dependencies in .sln declaration order, then a
        /// depth-first walk over all apps in declaration order that emits each app after its
        /// .cwproj ProjectReferences and its .sln ProjectDependencies. The global order is then
        /// filtered to the selected apps. Reordering in that dialog only rewrites the .sln's
        /// ProjectDependencies, so this always matches what the dialog shows.
        /// </summary>
        public List<AppInfo> OrderByDependencies(List<AppInfo> allApps, List<AppInfo> selectedApps)
        {
            var solutionOrder = new List<AppInfo>(allApps);
            solutionOrder.Sort((a, b) => a.SolutionIndex.CompareTo(b.SolutionIndex));

            var byGuid = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in solutionOrder)
                if (!string.IsNullOrEmpty(app.Guid) && !byGuid.ContainsKey(app.Guid)) byGuid[app.Guid] = app;

            var sorted = new List<AppInfo>();
            var added = new HashSet<AppInfo>();

            foreach (var app in solutionOrder)
            {
                if (!app.HasProjectReferences && !HasSolutionDependencies(app, byGuid))
                {
                    sorted.Add(app);
                    added.Add(app);
                }
            }

            foreach (var app in solutionOrder)
            {
                AddProject(app, byGuid, sorted, added, new List<AppInfo>());
            }

            var selected = new HashSet<AppInfo>(selectedApps);
            var ordered = new List<AppInfo>();
            foreach (var app in sorted)
                if (selected.Contains(app)) ordered.Add(app);
            return ordered;
        }

        private static bool HasSolutionDependencies(AppInfo app, Dictionary<string, AppInfo> byGuid)
        {
            foreach (var guid in app.DependsOnGuids)
                if (byGuid.ContainsKey(guid)) return true;
            return false;
        }

        private void AddProject(AppInfo app, Dictionary<string, AppInfo> byGuid, List<AppInfo> sorted,
            HashSet<AppInfo> added, List<AppInfo> stack)
        {
            if (app == null || added.Contains(app)) return;

            stack.Add(app);

            foreach (var guid in app.ReferencedGuids)
            {
                AppInfo dep;
                if (byGuid.TryGetValue(guid, out dep) && !stack.Contains(dep))
                    AddProject(dep, byGuid, sorted, added, stack);
            }

            foreach (var guid in app.DependsOnGuids)
            {
                AppInfo dep;
                if (byGuid.TryGetValue(guid, out dep) && !stack.Contains(dep))
                    AddProject(dep, byGuid, sorted, added, stack);
            }

            stack.RemoveAt(stack.Count - 1);
            sorted.Add(app);
            added.Add(app);
        }

        private string ResolveAppFile(string solutionDir, string appName)
        {
            try
            {
                var matches = Directory.GetFiles(solutionDir, appName + ".*");
                foreach (var f in matches)
                {
                    if (string.Equals(Path.GetExtension(f), ".app", StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            catch { }
            return Path.Combine(solutionDir, appName + ".app");
        }
    }
}
