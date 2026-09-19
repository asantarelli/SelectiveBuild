namespace SelectiveBuild
{
    partial class SelectiveBuildControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.CheckedListBox clbApps;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnSelectNone;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.ComboBox cmbConfiguration;
        private System.Windows.Forms.Label lblConfiguration;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.FlowLayoutPanel pnlRowSelection;
        private System.Windows.Forms.FlowLayoutPanel pnlRowConfig;
        private System.Windows.Forms.FlowLayoutPanel pnlRowBuild;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.clbApps = new System.Windows.Forms.CheckedListBox();
            this.pnlRowSelection = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnSelectNone = new System.Windows.Forms.Button();
            this.pnlRowConfig = new System.Windows.Forms.FlowLayoutPanel();
            this.lblConfiguration = new System.Windows.Forms.Label();
            this.cmbConfiguration = new System.Windows.Forms.ComboBox();
            this.pnlRowBuild = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBuild = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.pnlRowSelection.SuspendLayout();
            this.pnlRowConfig.SuspendLayout();
            this.pnlRowBuild.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            //
            // splitContainer.Panel1
            //
            this.splitContainer.Panel1.Controls.Add(this.clbApps);
            this.splitContainer.Panel1.Controls.Add(this.pnlTop);
            //
            // splitContainer.Panel2
            //
            this.splitContainer.Panel2.Controls.Add(this.txtLog);
            this.splitContainer.Size = new System.Drawing.Size(400, 500);
            this.splitContainer.SplitterDistance = 220;
            this.splitContainer.TabIndex = 0;
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.pnlRowSelection);
            this.pnlTop.Controls.Add(this.pnlRowConfig);
            this.pnlTop.Controls.Add(this.pnlRowBuild);
            this.pnlTop.Controls.Add(this.lblStatus);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlTop.Location = new System.Drawing.Point(0, 90);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(400, 130);
            this.pnlTop.TabIndex = 1;
            //
            // clbApps
            //
            this.clbApps.CheckOnClick = true;
            this.clbApps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbApps.FormattingEnabled = true;
            this.clbApps.Location = new System.Drawing.Point(0, 0);
            this.clbApps.Name = "clbApps";
            this.clbApps.Size = new System.Drawing.Size(400, 90);
            this.clbApps.TabIndex = 0;
            //
            // pnlRowSelection (Refrescar / Todos / Ninguno)
            //
            this.pnlRowSelection.AutoSize = true;
            this.pnlRowSelection.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRowSelection.Location = new System.Drawing.Point(0, 0);
            this.pnlRowSelection.Name = "pnlRowSelection";
            this.pnlRowSelection.Size = new System.Drawing.Size(400, 30);
            this.pnlRowSelection.TabIndex = 0;
            this.pnlRowSelection.Controls.Add(this.btnRefresh);
            this.pnlRowSelection.Controls.Add(this.btnSelectAll);
            this.pnlRowSelection.Controls.Add(this.btnSelectNone);
            //
            // btnRefresh
            //
            this.btnRefresh.AutoSize = true;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Text = "Refrescar";
            this.btnRefresh.UseVisualStyleBackColor = true;
            //
            // btnSelectAll
            //
            this.btnSelectAll.AutoSize = true;
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Text = "Todos";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            //
            // btnSelectNone
            //
            this.btnSelectNone.AutoSize = true;
            this.btnSelectNone.Name = "btnSelectNone";
            this.btnSelectNone.Text = "Ninguno";
            this.btnSelectNone.UseVisualStyleBackColor = true;
            //
            // pnlRowConfig (Config)
            //
            this.pnlRowConfig.AutoSize = true;
            this.pnlRowConfig.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRowConfig.Location = new System.Drawing.Point(0, 30);
            this.pnlRowConfig.Name = "pnlRowConfig";
            this.pnlRowConfig.Size = new System.Drawing.Size(400, 30);
            this.pnlRowConfig.TabIndex = 1;
            this.pnlRowConfig.Controls.Add(this.lblConfiguration);
            this.pnlRowConfig.Controls.Add(this.cmbConfiguration);
            //
            // lblConfiguration
            //
            this.lblConfiguration.AutoSize = true;
            this.lblConfiguration.Name = "lblConfiguration";
            this.lblConfiguration.Text = "Config:";
            this.lblConfiguration.Padding = new System.Windows.Forms.Padding(4, 6, 0, 0);
            //
            // cmbConfiguration
            //
            this.cmbConfiguration.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbConfiguration.Items.AddRange(new object[] { "Debug", "Release" });
            this.cmbConfiguration.SelectedIndex = 1;
            this.cmbConfiguration.Name = "cmbConfiguration";
            this.cmbConfiguration.Width = 90;
            //
            // pnlRowBuild (Compilar)
            //
            this.pnlRowBuild.AutoSize = true;
            this.pnlRowBuild.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRowBuild.Location = new System.Drawing.Point(0, 60);
            this.pnlRowBuild.Name = "pnlRowBuild";
            this.pnlRowBuild.Size = new System.Drawing.Size(400, 30);
            this.pnlRowBuild.TabIndex = 2;
            this.pnlRowBuild.Controls.Add(this.btnBuild);
            //
            // btnBuild
            //
            this.btnBuild.AutoSize = true;
            this.btnBuild.Name = "btnBuild";
            this.btnBuild.Text = "Compilar seleccionados";
            this.btnBuild.UseVisualStyleBackColor = true;
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatus.Location = new System.Drawing.Point(0, 90);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(4);
            this.lblStatus.Text = "Listo.";
            //
            // txtLog
            //
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(400, 276);
            this.txtLog.TabIndex = 0;
            this.txtLog.WordWrap = false;
            //
            // SelectiveBuildControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer);
            this.Name = "SelectiveBuildControl";
            this.Size = new System.Drawing.Size(400, 500);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlRowSelection.ResumeLayout(false);
            this.pnlRowSelection.PerformLayout();
            this.pnlRowConfig.ResumeLayout(false);
            this.pnlRowConfig.PerformLayout();
            this.pnlRowBuild.ResumeLayout(false);
            this.pnlRowBuild.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
