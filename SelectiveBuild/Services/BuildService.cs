using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SelectiveBuild.Services
{
    public class BuildResult
    {
        public string AppName { get; set; }
        public bool Success { get; set; }
        public string Output { get; set; }
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

        public event Action<string> OutputReceived;

        public BuildResult Build(AppInfo app, string clarionCLPath, string configuration, string platform, int timeoutSeconds)
        {
            var sb = new StringBuilder();
            bool ok = true;

            Log(sb, "=== " + app.Name + " ===");

            if (!File.Exists(app.AppPath))
            {
                Log(sb, "ERROR: no se encontró el archivo .app: " + app.AppPath);
                return new BuildResult { AppName = app.Name, Success = false, Output = sb.ToString() };
            }

            // ClarionCL /ag needs exclusive access to the .app; if it's open (typically in the IDE), skip it
            // with a clear message instead of letting ClarionCL fail with a less obvious one.
            string lockError = CheckExclusiveAccess(app.AppPath);
            if (lockError != null)
            {
                Log(sb, "ERROR: " + lockError + " Se omite " + app.Name + ".");
                return new BuildResult { AppName = app.Name, Success = false, Output = sb.ToString() };
            }

            // Step 1: generate source from the .app
            ok = RunProcess(clarionCLPath, "/ag \"" + app.AppPath + "\" /au", Path.GetDirectoryName(app.AppPath), timeoutSeconds, sb);
            if (!ok)
            {
                Log(sb, "ERROR generando fuente para " + app.Name);
                return new BuildResult { AppName = app.Name, Success = false, Output = sb.ToString() };
            }

            // Step 2: compile/link via MSBuild
            if (!File.Exists(app.CwprojPath))
            {
                Log(sb, "ERROR: no se encontró el proyecto: " + app.CwprojPath);
                return new BuildResult { AppName = app.Name, Success = false, Output = sb.ToString() };
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
                Log(sb, "ADVERTENCIA: no se encontró SoftVelocity.Build.Clarion.targets en " + clarionBinPath);
            }
            ok = RunProcess(MsBuildPath, msbuildArgs, Path.GetDirectoryName(app.CwprojPath), timeoutSeconds, sb);

            return new BuildResult { AppName = app.Name, Success = ok, Output = sb.ToString() };
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

        private bool RunProcess(string exePath, string arguments, string workingDir, int timeoutSeconds, StringBuilder log)
        {
            if (!File.Exists(exePath))
            {
                Log(log, "ERROR: no se encontró el ejecutable: " + exePath);
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
                    proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) Log(log, e.Data); };
                    proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) Log(log, e.Data); };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    bool exited = proc.WaitForExit(timeoutSeconds * 1000);
                    if (!exited)
                    {
                        try { proc.Kill(); } catch { }
                        Log(log, "ERROR: tiempo de espera agotado (" + timeoutSeconds + "s)");
                        return false;
                    }

                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Log(log, "ERROR ejecutando " + Path.GetFileName(exePath) + ": " + ex.Message);
                return false;
            }
        }

        private void Log(StringBuilder sb, string line)
        {
            sb.AppendLine(line);
            if (OutputReceived != null) OutputReceived(line);
        }
    }
}
