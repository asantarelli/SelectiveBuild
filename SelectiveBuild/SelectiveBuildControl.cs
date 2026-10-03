using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SelectiveBuild.Services;

namespace SelectiveBuild
{
    public partial class SelectiveBuildControl : UserControl
    {
        /// <summary>How much of the build output goes to the Log tab. Matches cmbDetail's item order.</summary>
        private enum DetailLevel
        {
            Minimum = 0,
            ErrorsOnly = 1,
            All = 2
        }

        private const string DetailLevelSettingKey = "DetailLevel";

        private readonly SolutionService _solutionService;
        private readonly BuildService _buildService;
        private readonly SettingsService _settingsService;

        private AppInfo[] _apps = new AppInfo[0];
        private string _clarionCLPath;
        private BackgroundWorker _worker;

        private readonly List<BuildResult> _results = new List<BuildResult>();
        private string _lastSolutionName = "";
        private string _lastConfiguration = "";
        private string _lastPlatform = "";
        private string _lastOrder = "";

        public SelectiveBuildControl()
        {
            InitializeComponent();

            _solutionService = new SolutionService();
            _buildService = new BuildService();
            _settingsService = new SettingsService();

            _clarionCLPath = ResolveClarionCLPath();

            btnRefresh.Click += (s, e) => RefreshApps();
            btnSelectAll.Click += (s, e) => SetAllChecked(true);
            btnSelectNone.Click += (s, e) => SetAllChecked(false);
            btnBuild.Click += (s, e) => StartBuild();
            btnCopyErrors.Click += (s, e) => CopyReport(true);
            btnCopyLog.Click += (s, e) => CopyReport(false);
            lvResults.SelectedIndexChanged += (s, e) => ShowSelectedResultDetail();
            cmbDetail.SelectedIndexChanged += (s, e) =>
                _settingsService.Set(DetailLevelSettingKey, cmbDetail.SelectedIndex.ToString(CultureInfo.InvariantCulture));

            RestoreDetailLevel();
            RefreshApps();
        }

        public void RefreshContent()
        {
            RefreshApps();
        }

        private void RestoreDetailLevel()
        {
            int saved;
            string value = _settingsService.Get(DetailLevelSettingKey);
            if (int.TryParse(value, out saved) && saved >= 0 && saved < cmbDetail.Items.Count)
                cmbDetail.SelectedIndex = saved;
        }

        private DetailLevel SelectedDetailLevel
        {
            get { return (DetailLevel)cmbDetail.SelectedIndex; }
        }

        private string ResolveClarionCLPath()
        {
            // ClarionCL.exe lives next to the IDE's own executable (bin folder of the Clarion install).
            try
            {
                string ideDir = Path.GetDirectoryName(Application.ExecutablePath);
                string candidate = Path.Combine(ideDir ?? "", "ClarionCL.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
            return "ClarionCL.exe";
        }

        private void RefreshApps()
        {
            string slnPath = _solutionService.GetActiveSolutionPath();
            if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
            {
                lblStatus.Text = "No hay solución abierta.";
                clbApps.Items.Clear();
                _apps = new AppInfo[0];
                return;
            }

            _apps = _solutionService.GetApps(slnPath).ToArray();
            var lastSelection = _settingsService.GetList("LastSelection:" + Path.GetFileName(slnPath));

            clbApps.Items.Clear();
            foreach (var app in _apps)
            {
                bool wasChecked = lastSelection.Contains(app.Name, StringComparer.OrdinalIgnoreCase);
                clbApps.Items.Add(app.Name, wasChecked);
            }

            lblStatus.Text = _apps.Length + " app(s) en " + Path.GetFileName(slnPath);
        }

        private void SetAllChecked(bool value)
        {
            for (int i = 0; i < clbApps.Items.Count; i++)
                clbApps.SetItemChecked(i, value);
        }

        private void StartBuild()
        {
            if (_worker != null && _worker.IsBusy) return;

            var selectedNames = clbApps.CheckedItems.Cast<object>().Select(o => o.ToString()).ToArray();
            if (selectedNames.Length == 0)
            {
                MessageBox.Show("Seleccioná al menos un app.", "Selective Build",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string slnPath = _solutionService.GetActiveSolutionPath();
            if (!string.IsNullOrEmpty(slnPath))
            {
                _settingsService.SetList("LastSelection:" + Path.GetFileName(slnPath), selectedNames);
            }

            var selectedApps = _apps.Where(a => selectedNames.Contains(a.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            selectedApps = _solutionService.OrderByDependencies(_apps.ToList(), selectedApps);
            string configuration = cmbConfiguration.SelectedItem != null ? cmbConfiguration.SelectedItem.ToString() : "Release";
            string platform = "Win32";

            _results.Clear();
            lvResults.Items.Clear();
            txtDetail.Clear();
            txtLog.Clear();
            btnBuild.Enabled = false;
            lblStatus.Text = "Compilando...";

            _lastSolutionName = string.IsNullOrEmpty(slnPath) ? "(sin solución)" : Path.GetFileName(slnPath);
            _lastConfiguration = configuration;
            _lastPlatform = platform;
            _lastOrder = string.Join(" -> ", selectedApps.Select(a => a.Name).ToArray());

            AppendLog("Orden de compilación (por dependencias): " + _lastOrder);

            int total = selectedApps.Count;
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += (s, e) =>
            {
                int done = 0;
                foreach (var app in selectedApps)
                {
                    _buildService.OutputReceived += AppendLogThreadSafe;
                    var result = _buildService.Build(app, _clarionCLPath, configuration, platform, 300);
                    _buildService.OutputReceived -= AppendLogThreadSafe;

                    done++;
                    _worker.ReportProgress(total == 0 ? 100 : done * 100 / total, result);
                }
            };
            _worker.ProgressChanged += (s, e) =>
            {
                var result = e.UserState as BuildResult;
                if (result == null) return;

                _results.Add(result);
                AddResultRow(result);
                AppendLog("--> " + DescribeResult(result));
                lblStatus.Text = "Compilando... " + _results.Count + "/" + total;
            };
            _worker.RunWorkerCompleted += (s, e) =>
            {
                btnBuild.Enabled = true;
                if (e.Error != null)
                {
                    lblStatus.Text = "Error: " + e.Error.Message;
                    return;
                }

                lblStatus.Text = SummaryLine();
                AppendLog("");
                AppendLog("=== Resumen === " + SummaryLine());
                tabsBottom.SelectedTab = tabSummary;
                SelectFirstFailedResult();
            };
            _worker.RunWorkerAsync();
        }

        // ---------- resumen ----------

        private void AddResultRow(BuildResult result)
        {
            var item = new ListViewItem(result.AppName);
            item.SubItems.Add(StatusText(result.Status));
            item.SubItems.Add(result.Errors.Count.ToString(CultureInfo.CurrentCulture));
            item.SubItems.Add(result.Warnings.Count.ToString(CultureInfo.CurrentCulture));
            item.SubItems.Add(FormatDuration(result.TotalTime));
            item.Tag = result;

            if (result.Status == BuildStatus.Error) item.ForeColor = Color.Firebrick;
            else if (result.Status == BuildStatus.Skipped) item.ForeColor = Color.DarkOrange;
            else if (result.Warnings.Count > 0) item.ForeColor = Color.DarkGoldenrod;
            else item.ForeColor = Color.DarkGreen;

            lvResults.Items.Add(item);
        }

        /// <summary>Opens the detail of the first app that failed, so an error is visible without hunting for it.</summary>
        private void SelectFirstFailedResult()
        {
            foreach (ListViewItem item in lvResults.Items)
            {
                var result = item.Tag as BuildResult;
                if (result != null && result.Status != BuildStatus.Ok)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    return;
                }
            }
            if (lvResults.Items.Count > 0) lvResults.Items[0].Selected = true;
        }

        private void ShowSelectedResultDetail()
        {
            if (lvResults.SelectedItems.Count == 0)
            {
                txtDetail.Clear();
                return;
            }

            var result = lvResults.SelectedItems[0].Tag as BuildResult;
            if (result == null)
            {
                txtDetail.Clear();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(DescribeResult(result));

            if (result.Errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Errores:");
                foreach (var line in result.Errors) sb.AppendLine("  " + line);
            }

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Advertencias:");
                foreach (var line in result.Warnings) sb.AppendLine("  " + line);
            }

            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Sin errores ni advertencias.");
            }

            txtDetail.Text = sb.ToString();
            txtDetail.SelectionStart = 0;
            txtDetail.ScrollToCaret();
        }

        private static string StatusText(BuildStatus status)
        {
            if (status == BuildStatus.Ok) return "OK";
            if (status == BuildStatus.Skipped) return "Omitido";
            return "Error";
        }

        private static string DescribeResult(BuildResult result)
        {
            var sb = new StringBuilder();
            sb.Append(result.AppName).Append(": ").Append(StatusText(result.Status));

            if (result.Errors.Count > 0 || result.Warnings.Count > 0)
            {
                sb.Append(" — ");
                sb.Append(result.Errors.Count).Append(result.Errors.Count == 1 ? " error" : " errores");
                if (result.Warnings.Count > 0)
                {
                    sb.Append(", ").Append(result.Warnings.Count);
                    sb.Append(result.Warnings.Count == 1 ? " advertencia" : " advertencias");
                }
            }

            sb.Append(" (").Append(FormatDuration(result.TotalTime)).Append(")");
            return sb.ToString();
        }

        private string SummaryLine()
        {
            int ok = _results.Count(r => r.Status == BuildStatus.Ok);
            int failed = _results.Count(r => r.Status == BuildStatus.Error);
            int skipped = _results.Count(r => r.Status == BuildStatus.Skipped);
            int errors = _results.Sum(r => r.Errors.Count);
            int warnings = _results.Sum(r => r.Warnings.Count);

            var total = TimeSpan.Zero;
            foreach (var r in _results) total += r.TotalTime;

            var sb = new StringBuilder();
            sb.Append("OK: ").Append(ok).Append("  Error: ").Append(failed);
            if (skipped > 0) sb.Append("  Omitido: ").Append(skipped);
            sb.Append("  (").Append(errors).Append(" errores, ").Append(warnings).Append(" advertencias)");
            sb.Append("  Total: ").Append(FormatDuration(total));
            return sb.ToString();
        }

        private static string FormatDuration(TimeSpan span)
        {
            if (span.TotalSeconds < 60)
                return span.TotalSeconds.ToString("0.0", CultureInfo.CurrentCulture) + " s";
            return ((int)span.TotalMinutes) + ":" + span.Seconds.ToString("00", CultureInfo.CurrentCulture);
        }

        // ---------- copiar al portapapeles ----------

        private void CopyReport(bool onlyErrors)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("Todavía no hay resultados para copiar.", "Selective Build",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string report = BuildReport(onlyErrors);
            try
            {
                Clipboard.SetText(report);
                lblStatus.Text = (onlyErrors ? "Errores copiados" : "Log completo copiado") +
                    " al portapapeles. " + SummaryLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo copiar al portapapeles: " + ex.Message, "Selective Build",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Builds the text report for the clipboard: context, per-app status and either just the
        /// error/warning lines or each app's full output.
        /// </summary>
        private string BuildReport(bool onlyErrors)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SelectiveBuild v" + GetVersion() + " — " +
                (onlyErrors ? "errores de compilación" : "log de compilación"));
            sb.AppendLine("Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture));
            sb.AppendLine("Solución: " + _lastSolutionName);
            sb.AppendLine("Configuración: " + _lastConfiguration + " | Plataforma: " + _lastPlatform);
            sb.AppendLine("Orden: " + _lastOrder);
            sb.AppendLine("Resultado: " + SummaryLine());

            foreach (var result in _results)
            {
                sb.AppendLine();
                sb.AppendLine("[" + StatusText(result.Status) + "] " + DescribeResult(result));

                if (onlyErrors)
                {
                    foreach (var line in result.Errors) sb.AppendLine("  " + line);
                    foreach (var line in result.Warnings) sb.AppendLine("  (adv) " + line);
                }
                else
                {
                    sb.AppendLine(result.Output);
                }
            }

            return sb.ToString();
        }

        private static string GetVersion()
        {
            try
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.ToString(3);
            }
            catch
            {
                return "?";
            }
        }

        // ---------- log ----------

        private void AppendLogThreadSafe(string line)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => AppendFilteredLog(line)));
            }
            else
            {
                AppendFilteredLog(line);
            }
        }

        /// <summary>Appends a line from the build processes, honouring the chosen detail level.</summary>
        private void AppendFilteredLog(string line)
        {
            LineKind kind = BuildService.Classify(line);
            DetailLevel level = SelectedDetailLevel;

            if (level == DetailLevel.Minimum && kind != LineKind.Header) return;
            if (level == DetailLevel.ErrorsOnly && kind == LineKind.Info) return;

            AppendLog(line);
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText(line + Environment.NewLine);
        }
    }
}
