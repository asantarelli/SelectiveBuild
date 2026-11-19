using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

// Instalador de SelectiveBuild. Un único .exe (sin dependencias externas) que lleva embebidos
// SelectiveBuild.dll y SelectiveBuild.addin. Se compila con el csc de .NET 4, igual que el addin,
// por lo que NO se puede usar C# 6+ (nada de $"", ?., => en miembros, etc.).
//
// Línea de comandos:
//   Setup.exe                      asistente gráfico
//   Setup.exe /S [/D=<carpeta>]    instalación silenciosa en la carpeta de Clarion indicada (o la única detectada)
//   Setup.exe /UNINSTALL [/S]      desinstala
//   /LOG=<archivo>                 escribe el detalle en un archivo
// Códigos de salida: 0 ok, 1 error, 2 faltan requisitos, 3 el IDE está abierto,
//                    4 no se encontró Clarion, 5 se necesitan permisos de administrador.
namespace SelectiveBuild.Setup
{
    internal enum Level
    {
        Ok,
        Warn,
        Fail
    }

    internal class Check
    {
        public Level Level;
        public string Name;
        public string Detail;

        public Check(Level level, string name, string detail)
        {
            Level = level;
            Name = name;
            Detail = detail;
        }
    }

    internal class InstalledInfo
    {
        public string Version;
        public string Root;
        public string Dir;
        public string[] Files;
        public string Uninstaller;
        public bool Machine;
    }

    internal class Options
    {
        public bool Silent;
        public bool Uninstall;
        public bool Auto;
        public string Root;
        public string LogFile;

        public static Options Parse(string[] args)
        {
            Options o = new Options();
            foreach (string raw in args)
            {
                string a = raw.Trim();
                string u = a.ToUpperInvariant();
                if (u == "/S" || u == "/SILENT" || u == "/VERYSILENT") o.Silent = true;
                else if (u == "/UNINSTALL") o.Uninstall = true;
                else if (u == "/AUTO") o.Auto = true;
                else if (u.StartsWith("/D=")) o.Root = a.Substring(3).Trim('"');
                else if (u.StartsWith("/LOG=")) o.LogFile = a.Substring(5).Trim('"');
            }
            return o;
        }

        /// <summary>Argumentos para relanzar el instalador elevado con el mismo pedido.</summary>
        public string ToArguments(string root)
        {
            string s = string.Empty;
            if (Uninstall) s += "/UNINSTALL ";
            else s += "/AUTO ";
            if (!string.IsNullOrEmpty(root)) s += "/D=\"" + root + "\" ";
            return s.Trim();
        }
    }

    internal static class Installer
    {
        public const string DllName = "SelectiveBuild.dll";
        public const string AddinName = "SelectiveBuild.addin";
        public const string ProductName = "SelectiveBuild";
        private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SelectiveBuild";

        public static string Version
        {
            get
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major + "." + v.Minor + "." + v.Build;
            }
        }

        public static bool IsAdmin()
        {
            using (WindowsIdentity id = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        // ---------------------------------------------------------------- detección de Clarion

        /// <summary>Instalaciones de Clarion registradas (HKLM/HKCU\SOFTWARE\SoftVelocity\Clarion*\root).</summary>
        public static List<string> DetectClarionRoots()
        {
            List<string> roots = new List<string>();
            RegistryKey[] hives = { Registry.LocalMachine, Registry.CurrentUser };
            string[] bases = { @"SOFTWARE\WOW6432Node\SoftVelocity", @"SOFTWARE\SoftVelocity" };

            foreach (RegistryKey hive in hives)
            {
                foreach (string b in bases)
                {
                    using (RegistryKey k = hive.OpenSubKey(b))
                    {
                        if (k == null) continue;
                        foreach (string sub in k.GetSubKeyNames())
                        {
                            if (!sub.StartsWith("Clarion", StringComparison.OrdinalIgnoreCase)) continue;
                            using (RegistryKey sk = k.OpenSubKey(sub))
                            {
                                string r = sk == null ? null : sk.GetValue("root") as string;
                                if (string.IsNullOrEmpty(r)) continue;
                                r = r.Trim().TrimEnd('\\');
                                if (!File.Exists(Path.Combine(r, @"bin\ICSharpCode.SharpDevelop.dll"))) continue;
                                if (!roots.Exists(delegate(string x) { return string.Equals(x, r, StringComparison.OrdinalIgnoreCase); }))
                                    roots.Add(r);
                            }
                        }
                    }
                }
            }
            return roots;
        }

        private static string MsBuildPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe");
        }

        /// <summary>
        /// Verifica todo lo que SelectiveBuild necesita de la máquina: las DLL del IDE contra las que
        /// está compilado, ClarionCL.exe y el MSBuild de .NET 4 con el que compila cada app.
        /// </summary>
        public static List<Check> CheckRequirements(string root)
        {
            List<Check> l = new List<Check>();

            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                l.Add(new Check(Level.Fail, "Carpeta de Clarion", "No se encontró la carpeta indicada."));
                return l;
            }

            string bin = Path.Combine(root, "bin");
            string ide = Path.Combine(bin, "Clarion.exe");
            if (!File.Exists(ide))
            {
                l.Add(new Check(Level.Fail, "IDE de Clarion", @"No se encontró bin\Clarion.exe: no parece una instalación de Clarion."));
            }
            else
            {
                FileVersionInfo v = FileVersionInfo.GetVersionInfo(ide);
                if (v.FileMajorPart == 11)
                    l.Add(new Check(Level.Ok, "IDE de Clarion", "Clarion " + v.FileVersion));
                else
                    l.Add(new Check(Level.Warn, "IDE de Clarion",
                        "Clarion " + v.FileVersion + ": SelectiveBuild solo se probó con Clarion 11."));
            }

            bool core = File.Exists(Path.Combine(bin, "ICSharpCode.Core.dll"));
            bool sd = File.Exists(Path.Combine(bin, "ICSharpCode.SharpDevelop.dll"));
            if (core && sd)
                l.Add(new Check(Level.Ok, "Ensamblados del IDE", "ICSharpCode.Core y ICSharpCode.SharpDevelop"));
            else
                l.Add(new Check(Level.Fail, "Ensamblados del IDE", @"Faltan ICSharpCode.Core.dll / ICSharpCode.SharpDevelop.dll en bin\."));

            if (File.Exists(Path.Combine(bin, "ClarionCL.exe")))
                l.Add(new Check(Level.Ok, "ClarionCL.exe", "Genera el código fuente de cada app"));
            else
                l.Add(new Check(Level.Fail, "ClarionCL.exe", @"No se encontró bin\ClarionCL.exe."));

            if (File.Exists(MsBuildPath()))
                l.Add(new Check(Level.Ok, ".NET Framework 4", "MSBuild.exe v4.0.30319"));
            else
                l.Add(new Check(Level.Fail, ".NET Framework 4", "No se encontró MSBuild.exe de .NET Framework 4.0.30319."));

            if (IsIdeRunning(root))
                l.Add(new Check(Level.Warn, "IDE abierto", "Hay que cerrarlo para instalar; se te va a pedir al instalar."));

            bool legacy;
            string dir = TargetDir(root, out legacy);
            string other = legacy
                ? Path.Combine(root, @"accessory\addins\SelectiveBuild")
                : Path.Combine(root, @"bin\Addins");
            if (File.Exists(Path.Combine(other, DllName)))
                l.Add(new Check(Level.Warn, "Copia duplicada",
                    "Hay otra copia en " + other + ". Dejá solo una o el IDE va a avisar de un addin repetido."));

            return l;
        }

        public static bool HasFailure(List<Check> checks)
        {
            foreach (Check c in checks)
                if (c.Level == Level.Fail) return true;
            return false;
        }

        /// <summary>True si hay un IDE abierto que usa esta instalación de Clarion (bloquea la DLL).</summary>
        public static bool IsIdeRunning(string root)
        {
            string bin = Path.GetFullPath(Path.Combine(root, "bin")).TrimEnd('\\') + "\\";
            foreach (Process p in Process.GetProcessesByName("Clarion"))
            {
                try
                {
                    string f = p.MainModule.FileName;
                    if (f.StartsWith(bin, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch (Exception)
                {
                    // No se pudo leer el módulo (otro usuario / elevado): por las dudas, se considera abierto.
                    return true;
                }
                finally
                {
                    p.Dispose();
                }
            }
            return false;
        }

        /// <summary>
        /// Carpeta de destino. Si ya hay una copia manual en bin\Addins (donde algunos instalan a mano)
        /// se actualiza ahí para no duplicar el addin; si no, va a accessory\addins\SelectiveBuild,
        /// que es la carpeta de addins de terceros de Clarion.
        /// </summary>
        public static string TargetDir(string root, out bool legacy)
        {
            string legacyDir = Path.Combine(root, @"bin\Addins");
            legacy = File.Exists(Path.Combine(legacyDir, DllName)) || File.Exists(Path.Combine(legacyDir, AddinName));
            return legacy ? legacyDir : Path.Combine(root, @"accessory\addins\SelectiveBuild");
        }

        // ---------------------------------------------------------------- instalar

        public static void Install(string root, Action<string> log)
        {
            bool legacy;
            string dir = TargetDir(root, out legacy);
            log("Destino: " + dir + (legacy ? " (se actualiza la copia existente en bin\\Addins)" : string.Empty));

            Directory.CreateDirectory(dir);
            string dll = Path.Combine(dir, DllName);
            string addin = Path.Combine(dir, AddinName);

            ExtractResource(DllName, dll);
            log("Copiado " + DllName);
            ExtractResource(AddinName, addin);
            log("Copiado " + AddinName);

            string unins = CopyUninstaller();
            RegisterUninstall(root, dir, new string[] { dll, addin }, unins);
            log("Registrado en Aplicaciones instaladas.");
        }

        private static void ExtractResource(string name, string destination)
        {
            if (File.Exists(destination))
                File.SetAttributes(destination, FileAttributes.Normal);

            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) throw new InvalidOperationException("El instalador está dañado: falta el recurso " + name + ".");
                using (FileStream f = new FileStream(destination, FileMode.Create, FileAccess.Write))
                {
                    byte[] buf = new byte[81920];
                    int n;
                    while ((n = s.Read(buf, 0, buf.Length)) > 0) f.Write(buf, 0, n);
                }
            }
        }

        private static string UninstallerDir()
        {
            string baseDir = Environment.GetFolderPath(IsAdmin()
                ? Environment.SpecialFolder.CommonApplicationData
                : Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, ProductName);
        }

        private static string CopyUninstaller()
        {
            string dir = UninstallerDir();
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, "Uninstall.exe");
            string self = Application.ExecutablePath;
            if (!string.Equals(Path.GetFullPath(self), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                File.Copy(self, target, true);
            return target;
        }

        private static void RegisterUninstall(string root, string dir, string[] files, string unins)
        {
            RegistryKey hive = IsAdmin() ? Registry.LocalMachine : Registry.CurrentUser;
            using (RegistryKey k = hive.CreateSubKey(UninstallKeyPath))
            {
                k.SetValue("DisplayName", "SelectiveBuild (addin para Clarion)");
                k.SetValue("DisplayVersion", Version);
                k.SetValue("Publisher", "Adrián Santarelli");
                k.SetValue("URLInfoAbout", "https://github.com/asantarelli/SelectiveBuild");
                k.SetValue("InstallLocation", dir);
                k.SetValue("DisplayIcon", unins);
                k.SetValue("UninstallString", "\"" + unins + "\" /UNINSTALL");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("ClarionRoot", root);
                k.SetValue("Uninstaller", unins);
                k.SetValue("Files", files, RegistryValueKind.MultiString);
            }
        }

        // ---------------------------------------------------------------- desinstalar

        public static InstalledInfo ReadInstalled()
        {
            RegistryKey[] hives = { Registry.LocalMachine, Registry.CurrentUser };
            foreach (RegistryKey hive in hives)
            {
                using (RegistryKey k = hive.OpenSubKey(UninstallKeyPath))
                {
                    if (k == null) continue;
                    InstalledInfo i = new InstalledInfo();
                    i.Machine = ReferenceEquals(hive, Registry.LocalMachine);
                    i.Version = k.GetValue("DisplayVersion") as string;
                    i.Root = k.GetValue("ClarionRoot") as string;
                    i.Dir = k.GetValue("InstallLocation") as string;
                    i.Uninstaller = k.GetValue("Uninstaller") as string;
                    i.Files = k.GetValue("Files") as string[] ?? new string[0];
                    return i;
                }
            }
            return null;
        }

        public static void Uninstall(InstalledInfo info, Action<string> log)
        {
            // Primero el registro: si no hay permisos para borrarlo, falla antes de tocar archivos.
            RegistryKey hive = info.Machine ? Registry.LocalMachine : Registry.CurrentUser;
            using (RegistryKey probe = hive.OpenSubKey(UninstallKeyPath, true))
            {
                if (probe == null) throw new UnauthorizedAccessException();
            }

            foreach (string f in info.Files)
            {
                if (!File.Exists(f)) continue;
                File.SetAttributes(f, FileAttributes.Normal);
                File.Delete(f);
                log("Eliminado " + f);
            }

            // Solo se borra la carpeta propia (accessory\addins\SelectiveBuild), nunca bin\Addins.
            if (!string.IsNullOrEmpty(info.Dir) && Directory.Exists(info.Dir)
                && string.Equals(Path.GetFileName(info.Dir.TrimEnd('\\')), ProductName, StringComparison.OrdinalIgnoreCase)
                && Directory.GetFileSystemEntries(info.Dir).Length == 0)
            {
                Directory.Delete(info.Dir);
                log("Eliminada la carpeta " + info.Dir);
            }

            hive.DeleteSubKeyTree(UninstallKeyPath, false);
            log("Quitado de Aplicaciones instaladas.");

            ScheduleSelfDelete(info.Uninstaller);
        }

        /// <summary>El desinstalador no puede borrarse mientras corre: lo borra un cmd que espera a que termine.</summary>
        private static void ScheduleSelfDelete(string uninstaller)
        {
            if (string.IsNullOrEmpty(uninstaller)) return;
            string self = Path.GetFullPath(Application.ExecutablePath);
            if (!string.Equals(self, Path.GetFullPath(uninstaller), StringComparison.OrdinalIgnoreCase)) return;

            string dir = Path.GetDirectoryName(self);
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                "/C ping 127.0.0.1 -n 3 > nul & del /F /Q \"" + self + "\" & rmdir \"" + dir + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
        }

        // ---------------------------------------------------------------- elevación

        public static bool Relaunch(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath, arguments);
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                return true;
            }
            catch (Win32Exception)
            {
                return false; // el usuario canceló el aviso de UAC
            }
        }
    }

    internal static class Program
    {
        private static string _logFile;

        [STAThread]
        private static int Main(string[] args)
        {
            Options opts = Options.Parse(args);
            _logFile = opts.LogFile;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                if (opts.Uninstall) return RunUninstall(opts);
                if (opts.Silent) return RunSilentInstall(opts);

                Application.Run(new SetupForm(opts));
                return Environment.ExitCode;
            }
            catch (Exception ex)
            {
                WriteLog("ERROR: " + ex);
                if (!opts.Silent)
                    MessageBox.Show("Ocurrió un error inesperado:\n\n" + ex.Message, "SelectiveBuild",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        internal static void WriteLog(string line)
        {
            if (string.IsNullOrEmpty(_logFile)) return;
            try
            {
                File.AppendAllText(_logFile, DateTime.Now.ToString("HH:mm:ss") + " " + line + Environment.NewLine);
            }
            catch (Exception)
            {
                // El log es opcional.
            }
        }

        private static int RunSilentInstall(Options opts)
        {
            string root = opts.Root;
            if (string.IsNullOrEmpty(root))
            {
                List<string> found = Installer.DetectClarionRoots();
                if (found.Count != 1)
                {
                    WriteLog(found.Count == 0
                        ? "No se encontró Clarion. Indicá la carpeta con /D=<carpeta>."
                        : "Hay varias instalaciones de Clarion. Indicá una con /D=<carpeta>.");
                    return 4;
                }
                root = found[0];
            }

            List<Check> checks = Installer.CheckRequirements(root);
            foreach (Check c in checks) WriteLog(c.Level + " | " + c.Name + " | " + c.Detail);
            if (Installer.HasFailure(checks)) return 2;
            if (Installer.IsIdeRunning(root))
            {
                WriteLog("El IDE de Clarion está abierto. Cerralo e intentá de nuevo.");
                return 3;
            }

            try
            {
                Installer.Install(root, new Action<string>(WriteLog));
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                WriteLog("Sin permisos para escribir en la carpeta de Clarion. Ejecutá como administrador.");
                return 5;
            }
        }

        internal static int RunUninstall(Options opts)
        {
            InstalledInfo info = Installer.ReadInstalled();
            if (info == null)
            {
                if (!opts.Silent)
                    MessageBox.Show("SelectiveBuild no está instalado.", "SelectiveBuild",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            if (!opts.Silent)
            {
                DialogResult r = MessageBox.Show(
                    "¿Desinstalar SelectiveBuild " + info.Version + "?\n\nSe borran los archivos del addin. " +
                    "Tus preferencias (%APPDATA%\\SelectiveBuild) se conservan.",
                    "SelectiveBuild", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return 0;
            }

            if (!string.IsNullOrEmpty(info.Root))
            {
                while (Installer.IsIdeRunning(info.Root))
                {
                    if (opts.Silent) return 3;
                    DialogResult r = MessageBox.Show("El IDE de Clarion está abierto. Cerralo para continuar.",
                        "SelectiveBuild", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                    if (r == DialogResult.Cancel) return 3;
                }
            }

            try
            {
                Installer.Uninstall(info, new Action<string>(WriteLog));
            }
            catch (UnauthorizedAccessException)
            {
                if (opts.Silent) return 5;
                if (!Installer.IsAdmin() && Installer.Relaunch(opts.ToArguments(info.Root))) return 0;
                MessageBox.Show("Se necesitan permisos de administrador para desinstalar.", "SelectiveBuild",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 5;
            }

            if (!opts.Silent)
                MessageBox.Show("SelectiveBuild se desinstaló. Reiniciá el IDE de Clarion.", "SelectiveBuild",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
    }
}
