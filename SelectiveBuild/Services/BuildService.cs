using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SelectiveBuild.Services
{
    /// <summary>Outcome of building one app.</summary>
    public enum BuildStatus
    {
        Ok,
        Error,
        /// <summary>Not built on purpose (e.g. the .app is open in the IDE), which is not the same as a failure.</summary>
        Skipped
    }

    /// <summary>What a log line says, used to filter the log and to collect the error/warning lists.</summary>
    public enum LineKind
    {
        Info,
        Header,
        Warning,
        Error
    }

    public class BuildResult
    {
        public string AppName { get; set; }
        public BuildStatus Status { get; set; }
        public string Output { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        /// <summary>Time spent in ClarionCL generating the source.</summary>
        public TimeSpan GenerateTime { get; set; }
        /// <summary>Time spent in MSBuild compiling and linking.</summary>
        public TimeSpan CompileTime { get; set; }

        public BuildResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public TimeSpan TotalTime
        {
            get { return GenerateTime + CompileTime; }
        }

        public bool Success
        {
            get { return Status == BuildStatus.Ok; }
        }
    }

    /// <summary>
    /// Builds a single Clarion app: ClarionCL generates the .clw/.inc source from the .app,
    /// then MSBuild compiles/links the .cwproj. Same two-step pipeline the Clarion IDE itself runs.
    /// </summary>
    public class BuildService
    {
        private static readonly string MsBuildPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Microsoft.NET", "Framework", "v4.0.30319", "MSBuild.exe");

        // Matches how the Clarion toolchain reports problems, e.g.
        //   "Sdgidata.exp(1,1): error : $ACCESS:... is unresolved for export"
        //   "SDGI1.cwproj(614,3): error MSB4019: ..."
        //   " warning CLCE004: Command Line Switch ... does not exist!"
        private static readonly Regex ErrorRegex = new Regex(
            @"(^|[\s)])(fatal\s+)?error\s*[A-Za-z]*\d*\s*:",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WarningRegex = new Regex(
            @"(^|[\s)])warning\s*[A-Za-z]*\d*\s*:",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public event Action<string> OutputReceived;

        /// <summary>
        /// Classifies one log line. Public so the UI can filter the log by detail level using
        /// exactly the same rules that built the error and warning lists.
        /// </summary>
        public static LineKind Classify(string line)
        {
            if (string.IsNullOrEmpty(line)) return LineKind.Info;

            if (line.StartsWith("=== ", StringComparison.Ordinal)) return LineKind.Header;

            // Messages written by SelectiveBuild itself.
            if (line.StartsWith("ERROR", StringComparison.Ordinal)) return LineKind.Error;
            if (line.StartsWith("ADVERTENCIA", StringComparison.Ordinal)) return LineKind.Warning;

            if (ErrorRegex.IsMatch(line)) return LineKind.Error;
            if (WarningRegex.IsMatch(line)) return LineKind.Warning;

            return LineKind.Info;
        }

        public BuildResult Build(AppInfo app, string clarionCLPath, string configuration, string platform, int timeoutSeconds)
        {
            var sb = new StringBuilder();
            var result = new BuildResult { AppName = app.Name, Status = BuildStatus.Error };

            Log(result, sb, "=== " + app.Name + " ===");

            if (!File.Exists(app.AppPath))
            {
                Log(result, sb, "ERROR: no se encontró el archivo .app: " + app.AppPath);
                return Finish(result, sb);
            }

            // ClarionCL /ag needs exclusive access to the .app; if it's open (typically in the IDE), skip it
            // with a clear message instead of letting ClarionCL fail with a less obvious one.
            string lockError = CheckExclusiveAccess(app.AppPath);
            if (lockError != null)
            {
                Log(result, sb, "ERROR: " + lockError + " Se omite " + app.Name + ".");
                result.Status = BuildStatus.Skipped;
                return Finish(result, sb);
            }

            // Step 1: generate source from the .app
            var watch = Stopwatch.StartNew();
            bool ok = RunProcess(clarionCLPath, "/ag \"" + app.AppPath + "\" /au", Path.GetDirectoryName(app.AppPath), timeoutSeconds, result, sb);
            watch.Stop();
            result.GenerateTime = watch.Elapsed;
            if (!ok)
            {
                Log(result, sb, "ERROR generando fuente para " + app.Name);
                return Finish(result, sb);
            }

            // Step 2: compile/link via MSBuild
            if (!File.Exists(app.CwprojPath))
            {
                Log(result, sb, "ERROR: no se encontró el proyecto: " + app.CwprojPath);
                return Finish(result, sb);
            }

            // NoDependency=true: build only this project, not its <ProjectReference>s. The IDE passes the
            // same flag for "build project only"; SelectiveBuild already builds the selected apps in
            // dependency order, so rebuilding references would just repeat work (and errors).
            string msbuildArgs = "\"" + app.CwprojPath + "\" /p:Configuration=" + configuration + " /p:Platform=" + platform + " /p:NoDependency=true /nologo /v:minimal";

            // .cwproj files import "$(ClarionBinPath)\SoftVelocity.Build.Clarion.targets". The IDE sets
            // ClarionBinPath as a global property (the folder of its own executable, see CWBinding.dll),
            // so a standalone MSBuild needs it passed explicitly. ClarionCL.exe lives in that same folder.
            string clarionBinPath = Path.GetDirectoryName(Path.GetFullPath(clarionCLPath));
            if (File.Exists(Path.Combine(clarionBinPath, "SoftVelocity.Build.Clarion.targets")))
            {
                msbuildArgs += " \"/p:ClarionBinPath=" + clarionBinPath.TrimEnd('\\') + "\"";
            }
            else
            {
                Log(result, sb, "ADVERTENCIA: no se encontró SoftVelocity.Build.Clarion.targets en " + clarionBinPath);
            }

            watch = Stopwatch.StartNew();
            ok = RunProcess(MsBuildPath, msbuildArgs, Path.GetDirectoryName(app.CwprojPath), timeoutSeconds, result, sb);
            watch.Stop();
            result.CompileTime = watch.Elapsed;

            if (ok) result.Status = BuildStatus.Ok;
            return Finish(result, sb);
        }

        private static BuildResult Finish(BuildResult result, StringBuilder sb)
        {
            result.Output = sb.ToString();
            return result;
        }

        /// <summary>
        /// Returns null if the file can be opened exclusively for read/write (as ClarionCL needs),
        /// otherwise a message explaining why not. The handle is released immediately.
        /// </summary>
        private static string CheckExclusiveAccess(string path)
        {
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return Path.GetFileName(path) + " es de solo lectura o no hay permisos de escritura.";
            }
            catch (IOException)
            {
                return Path.GetFileName(path) + " está en uso (¿abierto en el IDE?).";
            }
        }

        private bool RunProcess(string exePath, string arguments, string workingDir, int timeoutSeconds, BuildResult result, StringBuilder log)
        {
            if (!File.Exists(exePath))
            {
                Log(result, log, "ERROR: no se encontró el ejecutable: " + exePath);
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.Arguments = arguments;
                psi.WorkingDirectory = workingDir;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                // Console tools (MSBuild, ClarionCL) write redirected output in the OEM code page
                // (e.g. 850), not the ANSI one; decode accordingly so "Asegúrese" isn't "Aseg£rese".
                Encoding oem = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                psi.StandardOutputEncoding = oem;
                psi.StandardErrorEncoding = oem;

                using (var proc = new Process())
                {
                    proc.StartInfo = psi;
                    proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) Log(result, log, e.Data); };
                    proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) Log(result, log, e.Data); };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    bool exited = proc.WaitForExit(timeoutSeconds * 1000);
                    if (!exited)
                    {
                        try { proc.Kill(); } catch { }
                        Log(result, log, "ERROR: tiempo de espera agotado (" + timeoutSeconds + "s)");
                        return false;
                    }

                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Log(result, log, "ERROR ejecutando " + Path.GetFileName(exePath) + ": " + ex.Message);
                return false;
            }
        }

        private void Log(BuildResult result, StringBuilder sb, string line)
        {
            sb.AppendLine(line);

            LineKind kind = Classify(line);
            if (kind == LineKind.Error) result.Errors.Add(line.Trim());
            else if (kind == LineKind.Warning) result.Warnings.Add(line.Trim());

            if (OutputReceived != null) OutputReceived(line);
        }
    }
}
