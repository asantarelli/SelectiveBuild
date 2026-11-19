using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SelectiveBuild.Setup
{
    /// <summary>Asistente de una sola pantalla: elegir la instalación de Clarion, ver los requisitos e instalar.</summary>
    internal class SetupForm : Form
    {
        private readonly Options _opts;
        private ComboBox _cboRoot;
        private Button _btnBrowse;
        private Button _btnInstall;
        private Button _btnUninstall;
        private Button _btnClose;
        private ListView _lvChecks;
        private Label _lblTarget;
        private Label _lblInstalled;

        public SetupForm(Options opts)
        {
            _opts = opts;
            BuildUi();
            LoadRoots();
            RefreshChecks();

            // Relanzado con permisos de administrador: sigue directo con la instalación.
            if (_opts.Auto)
                Shown += delegate { if (_btnInstall.Enabled) DoInstall(); };
        }

        private void BuildUi()
        {
            Text = "Instalador de SelectiveBuild " + Installer.Version;
            ClientSize = new Size(560, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            Label title = new Label();
            title.Text = "SelectiveBuild " + Installer.Version;
            title.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            title.Location = new Point(16, 12);
            title.AutoSize = true;
            Controls.Add(title);

            Label desc = new Label();
            desc.Text = "Addin para el IDE de Clarion 11 que compila solo los apps que elijas de una solución multi-DLL.";
            desc.Location = new Point(18, 44);
            desc.Size = new Size(526, 32);
            Controls.Add(desc);

            Label lblRoot = new Label();
            lblRoot.Text = "Instalación de Clarion:";
            lblRoot.Location = new Point(18, 84);
            lblRoot.AutoSize = true;
            Controls.Add(lblRoot);

            _cboRoot = new ComboBox();
            _cboRoot.DropDownStyle = ComboBoxStyle.DropDown;
            _cboRoot.Location = new Point(18, 104);
            _cboRoot.Size = new Size(430, 24);
            _cboRoot.TextChanged += delegate { RefreshChecks(); };
            Controls.Add(_cboRoot);

            _btnBrowse = new Button();
            _btnBrowse.Text = "Examinar...";
            _btnBrowse.Location = new Point(456, 102);
            _btnBrowse.Size = new Size(88, 26);
            _btnBrowse.Click += delegate { Browse(); };
            Controls.Add(_btnBrowse);

            _lvChecks = new ListView();
            _lvChecks.View = View.Details;
            _lvChecks.FullRowSelect = true;
            _lvChecks.HeaderStyle = ColumnHeaderStyle.None;
            _lvChecks.Location = new Point(18, 140);
            _lvChecks.Size = new Size(526, 160);
            _lvChecks.Columns.Add("Estado", 60);
            _lvChecks.Columns.Add("Requisito", 140);
            _lvChecks.Columns.Add("Detalle", 300);
            Controls.Add(_lvChecks);

            _lblTarget = new Label();
            _lblTarget.Location = new Point(18, 308);
            _lblTarget.Size = new Size(526, 32);
            Controls.Add(_lblTarget);

            _lblInstalled = new Label();
            _lblInstalled.Location = new Point(18, 342);
            _lblInstalled.Size = new Size(526, 18);
            _lblInstalled.ForeColor = SystemColors.GrayText;
            Controls.Add(_lblInstalled);

            _btnUninstall = new Button();
            _btnUninstall.Text = "Desinstalar";
            _btnUninstall.Location = new Point(18, 364);
            _btnUninstall.Size = new Size(96, 28);
            _btnUninstall.Click += delegate { DoUninstall(); };
            Controls.Add(_btnUninstall);

            _btnInstall = new Button();
            _btnInstall.Text = "Instalar";
            _btnInstall.Location = new Point(358, 364);
            _btnInstall.Size = new Size(88, 28);
            _btnInstall.Click += delegate { DoInstall(); };
            Controls.Add(_btnInstall);

            _btnClose = new Button();
            _btnClose.Text = "Cerrar";
            _btnClose.Location = new Point(456, 364);
            _btnClose.Size = new Size(88, 28);
            _btnClose.DialogResult = DialogResult.Cancel;
            Controls.Add(_btnClose);

            AcceptButton = _btnInstall;
            CancelButton = _btnClose;
        }

        private void LoadRoots()
        {
            List<string> roots = Installer.DetectClarionRoots();
            if (!string.IsNullOrEmpty(_opts.Root) && !roots.Contains(_opts.Root)) roots.Insert(0, _opts.Root);
            foreach (string r in roots) _cboRoot.Items.Add(r);
            if (roots.Count > 0) _cboRoot.SelectedIndex = !string.IsNullOrEmpty(_opts.Root) ? roots.IndexOf(_opts.Root) : 0;
        }

        private void Browse()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "Elegí la carpeta donde está instalado Clarion (la que contiene bin\\Clarion.exe).";
                d.ShowNewFolderButton = false;
                if (Directory.Exists(_cboRoot.Text)) d.SelectedPath = _cboRoot.Text;
                if (d.ShowDialog(this) == DialogResult.OK) _cboRoot.Text = d.SelectedPath;
            }
        }

        private string Root
        {
            get { return _cboRoot.Text.Trim().TrimEnd('\\'); }
        }

        private void RefreshChecks()
        {
            List<Check> checks = Installer.CheckRequirements(Root);

            _lvChecks.BeginUpdate();
            _lvChecks.Items.Clear();
            foreach (Check c in checks)
            {
                string state = c.Level == Level.Ok ? "OK" : (c.Level == Level.Warn ? "Aviso" : "Falta");
                ListViewItem item = new ListViewItem(state);
                item.SubItems.Add(c.Name);
                item.SubItems.Add(c.Detail);
                item.ForeColor = c.Level == Level.Ok ? Color.FromArgb(0, 120, 0)
                    : (c.Level == Level.Warn ? Color.FromArgb(190, 110, 0) : Color.Firebrick);
                _lvChecks.Items.Add(item);
            }
            _lvChecks.EndUpdate();

            bool ok = !Installer.HasFailure(checks);
            _btnInstall.Enabled = ok;

            if (ok)
            {
                bool legacy;
                string dir = Installer.TargetDir(Root, out legacy);
                _lblTarget.Text = (legacy ? "Se actualizará la copia existente en: " : "Se instalará en: ") + dir;
            }
            else
            {
                _lblTarget.Text = "No se puede instalar hasta resolver los requisitos marcados como \"Falta\".";
            }

            InstalledInfo inst = Installer.ReadInstalled();
            _btnUninstall.Visible = inst != null;
            _btnInstall.Text = inst == null ? "Instalar" : "Actualizar";
            _lblInstalled.Text = inst == null
                ? string.Empty
                : "Versión instalada: " + inst.Version + (inst.Version == Installer.Version ? " (la misma)" : " → pasará a " + Installer.Version);
        }

        private void DoInstall()
        {
            string root = Root;

            while (Installer.IsIdeRunning(root))
            {
                DialogResult r = MessageBox.Show(
                    "El IDE de Clarion está abierto y tiene bloqueada la DLL.\n\nCerralo por completo y presioná Reintentar.",
                    "SelectiveBuild", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                if (r == DialogResult.Cancel) return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                Installer.Install(root, new Action<string>(Program.WriteLog));
            }
            catch (UnauthorizedAccessException)
            {
                Cursor = Cursors.Default;
                if (!Installer.IsAdmin())
                {
                    DialogResult r = MessageBox.Show(
                        "Esa carpeta requiere permisos de administrador.\n\n¿Reintentar como administrador?",
                        "SelectiveBuild", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (r == DialogResult.Yes && Installer.Relaunch("/AUTO /D=\"" + root + "\""))
                        Close();
                }
                else
                {
                    MessageBox.Show("No se pudo escribir en la carpeta de Clarion.", "SelectiveBuild",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show("No se pudo instalar:\n\n" + ex.Message, "SelectiveBuild",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            MessageBox.Show(
                "SelectiveBuild " + Installer.Version + " se instaló correctamente.\n\n" +
                "Abrí (o reiniciá) el IDE de Clarion y buscá el pad en el menú Tools → Selective Build.",
                "SelectiveBuild", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void DoUninstall()
        {
            Options o = new Options();
            o.Uninstall = true;
            // La desinstalación pregunta y avisa por su cuenta.
            Program.RunUninstall(o);
            RefreshChecks();
            if (Installer.ReadInstalled() == null) Close();
        }
    }
}
