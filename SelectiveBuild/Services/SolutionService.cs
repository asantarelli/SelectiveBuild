using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SelectiveBuild.Services
{
    public class AppInfo
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public string CwprojPath { get; set; }
        public string AppPath { get; set; }
        public List<string> DependsOnGuids { get; set; }

        public AppInfo()
        {
            DependsOnGuids = new List<string>();
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
                            AppPath = ResolveAppFile(solutionDir, name)
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

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        /// <summary>
        /// Orders the selected apps so that every app builds after everything it depends on
        /// (per the .sln's Project Dependencies), using a stable topological sort. Dependencies
        /// that are not themselves selected are skipped (assumed already built / not requested).
        /// </summary>
        public List<AppInfo> OrderByDependencies(List<AppInfo> allApps, List<AppInfo> selectedApps)
        {
            var byGuid = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in allApps)
                if (!string.IsNullOrEmpty(app.Guid)) byGuid[app.Guid] = app;

            var selectedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in selectedApps)
                if (!string.IsNullOrEmpty(app.Guid)) selectedGuids.Add(app.Guid);

            var ordered = new List<AppInfo>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var app in selectedApps)
            {
                Visit(app, byGuid, selectedGuids, visited, visiting, ordered);
            }

            return ordered;
        }

        private void Visit(AppInfo app, Dictionary<string, AppInfo> byGuid, HashSet<string> selectedGuids,
            HashSet<string> visited, HashSet<string> visiting, List<AppInfo> ordered)
        {
            if (app == null || string.IsNullOrEmpty(app.Guid)) return;
            if (visited.Contains(app.Guid)) return;
            if (visiting.Contains(app.Guid)) return; // circular dependency in the .sln, skip re-entry

            visiting.Add(app.Guid);

            foreach (var depGuid in app.DependsOnGuids)
            {
                if (!selectedGuids.Contains(depGuid)) continue; // not selected, don't pull it in
                AppInfo dep;
                if (byGuid.TryGetValue(depGuid, out dep))
                {
                    Visit(dep, byGuid, selectedGuids, visited, visiting, ordered);
                }
            }

            visiting.Remove(app.Guid);
            visited.Add(app.Guid);
            ordered.Add(app);
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
