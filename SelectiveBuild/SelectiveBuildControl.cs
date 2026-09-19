using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SelectiveBuild.Services;

namespace SelectiveBuild
{
    public partial class SelectiveBuildControl : UserControl
    {
        private readonly SolutionService _solutionService;
        private readonly BuildService _buildService;
        private readonly SettingsService _settingsService;

        private AppInfo[] _apps = new AppInfo[0];
        private string _clarionCLPath;
        private BackgroundWorker _worker;

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

            RefreshApps();
        }

        public void RefreshContent()
        {
            RefreshApps();
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

            txtLog.Clear();
            btnBuild.Enabled = false;
            lblStatus.Text = "Compilando...";
            AppendLog("Orden de compilación (por dependencias): " + string.Join(" -> ", selectedApps.Select(a => a.Name).ToArray()));

            _worker = new BackgroundWorker();
            _worker.DoWork += (s, e) =>
            {
                int ok = 0, failed = 0;
                foreach (var app in selectedApps)
                {
                    _buildService.OutputReceived += AppendLogThreadSafe;
                    var result = _buildService.Build(app, _clarionCLPath, configuration, platform, 300);
                    _buildService.OutputReceived -= AppendLogThreadSafe;

                    if (result.Success) ok++; else failed++;
                }
                e.Result = new Tuple<int, int>(ok, failed);
            };
            _worker.RunWorkerCompleted += (s, e) =>
            {
                btnBuild.Enabled = true;
                if (e.Error != null)
                {
                    lblStatus.Text = "Error: " + e.Error.Message;
                    return;
                }
                var counts = (Tuple<int, int>)e.Result;
                lblStatus.Text = "Listo. OK: " + counts.Item1 + "  Error: " + counts.Item2;
            };
            _worker.RunWorkerAsync();
        }

        private void AppendLogThreadSafe(string line)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => AppendLog(line)));
            }
            else
            {
                AppendLog(line);
            }
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText(line + Environment.NewLine);
        }
    }
}
