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
        private System.Windows.Forms.ComboBox cmbDetail;
        private System.Windows.Forms.Label lblDetail;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.FlowLayoutPanel pnlRowSelection;
        private System.Windows.Forms.FlowLayoutPanel pnlRowConfig;
        private System.Windows.Forms.FlowLayoutPanel pnlRowBuild;
        private System.Windows.Forms.TabControl tabsBottom;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TabPage tabSummary;
        private System.Windows.Forms.SplitContainer splitSummary;
        private System.Windows.Forms.ListView lvResults;
        private System.Windows.Forms.TextBox txtDetail;
        private System.Windows.Forms.FlowLayoutPanel pnlSummaryButtons;
        private System.Windows.Forms.Button btnCopyErrors;
        private System.Windows.Forms.Button btnCopyLog;

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
            this.lblDetail = new System.Windows.Forms.Label();
            this.cmbDetail = new System.Windows.Forms.ComboBox();
            this.pnlRowBuild = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBuild = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.tabsBottom = new System.Windows.Forms.TabControl();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.tabSummary = new System.Windows.Forms.TabPage();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.splitSummary = new System.Windows.Forms.SplitContainer();
            this.lvResults = new System.Windows.Forms.ListView();
            this.txtDetail = new System.Windows.Forms.TextBox();
            this.pnlSummaryButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCopyErrors = new System.Windows.Forms.Button();
            this.btnCopyLog = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitSummary)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.splitSummary.Panel1.SuspendLayout();
            this.splitSummary.Panel2.SuspendLayout();
            this.splitSummary.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.pnlRowSelection.SuspendLayout();
            this.pnlRowConfig.SuspendLayout();
            this.pnlRowBuild.SuspendLayout();
            this.tabsBottom.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.tabSummary.SuspendLayout();
            this.pnlSummaryButtons.SuspendLayout();
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
            this.splitContainer.Panel2.Controls.Add(this.tabsBottom);
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
            // pnlRowConfig (Config / Detalle)
            //
            this.pnlRowConfig.AutoSize = true;
            this.pnlRowConfig.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRowConfig.Location = new System.Drawing.Point(0, 30);
            this.pnlRowConfig.Name = "pnlRowConfig";
            this.pnlRowConfig.Size = new System.Drawing.Size(400, 30);
            this.pnlRowConfig.TabIndex = 1;
            this.pnlRowConfig.Controls.Add(this.lblConfiguration);
            this.pnlRowConfig.Controls.Add(this.cmbConfiguration);
            this.pnlRowConfig.Controls.Add(this.lblDetail);
            this.pnlRowConfig.Controls.Add(this.cmbDetail);
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
            // lblDetail
            //
            this.lblDetail.AutoSize = true;
            this.lblDetail.Name = "lblDetail";
            this.lblDetail.Text = "Detalle:";
            this.lblDetail.Padding = new System.Windows.Forms.Padding(8, 6, 0, 0);
            //
            // cmbDetail
            //
            this.cmbDetail.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDetail.Items.AddRange(new object[] { "Mínimo", "Solo errores", "Todo" });
            this.cmbDetail.SelectedIndex = 2;
            this.cmbDetail.Name = "cmbDetail";
            this.cmbDetail.Width = 100;
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
            // tabsBottom
            //
            this.tabsBottom.Controls.Add(this.tabLog);
            this.tabsBottom.Controls.Add(this.tabSummary);
            this.tabsBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsBottom.Location = new System.Drawing.Point(0, 0);
            this.tabsBottom.Name = "tabsBottom";
            this.tabsBottom.SelectedIndex = 0;
            this.tabsBottom.Size = new System.Drawing.Size(400, 276);
            this.tabsBottom.TabIndex = 0;
            //
            // tabLog
            //
            this.tabLog.Controls.Add(this.txtLog);
            this.tabLog.Location = new System.Drawing.Point(4, 22);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(392, 250);
            this.tabLog.TabIndex = 0;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            //
            // txtLog
            //
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(3, 3);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(386, 244);
            this.txtLog.TabIndex = 0;
            this.txtLog.WordWrap = false;
            //
            // tabSummary
            //
            this.tabSummary.Controls.Add(this.splitSummary);
            this.tabSummary.Controls.Add(this.pnlSummaryButtons);
            this.tabSummary.Location = new System.Drawing.Point(4, 22);
            this.tabSummary.Name = "tabSummary";
            this.tabSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabSummary.Size = new System.Drawing.Size(392, 250);
            this.tabSummary.TabIndex = 1;
            this.tabSummary.Text = "Resumen";
            this.tabSummary.UseVisualStyleBackColor = true;
            //
            // splitSummary
            //
            this.splitSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitSummary.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitSummary.Location = new System.Drawing.Point(3, 3);
            this.splitSummary.Name = "splitSummary";
            this.splitSummary.Panel1.Controls.Add(this.lvResults);
            this.splitSummary.Panel2.Controls.Add(this.txtDetail);
            this.splitSummary.Size = new System.Drawing.Size(386, 214);
            this.splitSummary.SplitterDistance = 110;
            this.splitSummary.TabIndex = 0;
            //
            // lvResults
            //
            this.lvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvResults.FullRowSelect = true;
            this.lvResults.GridLines = true;
            this.lvResults.HideSelection = false;
            this.lvResults.MultiSelect = false;
            this.lvResults.Name = "lvResults";
            this.lvResults.TabIndex = 0;
            this.lvResults.UseCompatibleStateImageBehavior = false;
            this.lvResults.View = System.Windows.Forms.View.Details;
            this.lvResults.Columns.Add("App", 120);
            this.lvResults.Columns.Add("Estado", 70);
            this.lvResults.Columns.Add("Errores", 55, System.Windows.Forms.HorizontalAlignment.Right);
            this.lvResults.Columns.Add("Advert.", 55, System.Windows.Forms.HorizontalAlignment.Right);
            this.lvResults.Columns.Add("Duración", 70, System.Windows.Forms.HorizontalAlignment.Right);
            //
            // txtDetail
            //
            this.txtDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDetail.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtDetail.Multiline = true;
            this.txtDetail.Name = "txtDetail";
            this.txtDetail.ReadOnly = true;
            this.txtDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDetail.TabIndex = 1;
            this.txtDetail.WordWrap = false;
            //
            // pnlSummaryButtons
            //
            this.pnlSummaryButtons.AutoSize = true;
            this.pnlSummaryButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSummaryButtons.Name = "pnlSummaryButtons";
            this.pnlSummaryButtons.Size = new System.Drawing.Size(386, 30);
            this.pnlSummaryButtons.TabIndex = 1;
            this.pnlSummaryButtons.Controls.Add(this.btnCopyErrors);
            this.pnlSummaryButtons.Controls.Add(this.btnCopyLog);
            //
            // btnCopyErrors
            //
            this.btnCopyErrors.AutoSize = true;
            this.btnCopyErrors.Name = "btnCopyErrors";
            this.btnCopyErrors.Text = "Copiar errores";
            this.btnCopyErrors.UseVisualStyleBackColor = true;
            //
            // btnCopyLog
            //
            this.btnCopyLog.AutoSize = true;
            this.btnCopyLog.Name = "btnCopyLog";
            this.btnCopyLog.Text = "Copiar log completo";
            this.btnCopyLog.UseVisualStyleBackColor = true;
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
            this.splitSummary.Panel1.ResumeLayout(false);
            this.splitSummary.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitSummary)).EndInit();
            this.splitSummary.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlRowSelection.ResumeLayout(false);
            this.pnlRowSelection.PerformLayout();
            this.pnlRowConfig.ResumeLayout(false);
            this.pnlRowConfig.PerformLayout();
            this.pnlRowBuild.ResumeLayout(false);
            this.pnlRowBuild.PerformLayout();
            this.tabsBottom.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.tabSummary.ResumeLayout(false);
            this.tabSummary.PerformLayout();
            this.pnlSummaryButtons.ResumeLayout(false);
            this.pnlSummaryButtons.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
