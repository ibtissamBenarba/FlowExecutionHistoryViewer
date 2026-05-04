namespace ExecutionFlowHistoryViewer
{
    partial class MyPluginControl
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.tsmContainer = new System.Windows.Forms.ToolStrip();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSample = new System.Windows.Forms.ToolStripButton();
            this.tsmConnectToPA = new System.Windows.Forms.ToolStripButton();
            this.btnExport = new System.Windows.Forms.ToolStripButton();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.gbFlows = new System.Windows.Forms.GroupBox();
            this.clbFlows = new System.Windows.Forms.CheckedListBox();
            this.cbSelectAllFlows = new System.Windows.Forms.CheckBox();
            this.gbFlowFilters = new System.Windows.Forms.GroupBox();
            this.tbSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.cbxFlowStatusDraft = new System.Windows.Forms.CheckBox();
            this.cbxFlowStatusActivated = new System.Windows.Forms.CheckBox();
            this.gbSolution = new System.Windows.Forms.GroupBox();
            this.cbSolutions = new System.Windows.Forms.ComboBox();
            this.gbFlowRuns = new System.Windows.Forms.GroupBox();
            this.gbRunFilters = new System.Windows.Forms.GroupBox();
            this.btnFetchHistory = new System.Windows.Forms.Button();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.dtpDateTo = new System.Windows.Forms.DateTimePicker();
            this.lblDateTo = new System.Windows.Forms.Label();
            this.dtpDateFrom = new System.Windows.Forms.DateTimePicker();
            this.lblDateFrom = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.FlowRunStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FlowRunDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FlowRunError = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tsmContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.gbFlows.SuspendLayout();
            this.gbFlowFilters.SuspendLayout();
            this.gbSolution.SuspendLayout();
            this.gbFlowRuns.SuspendLayout();
            this.gbRunFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // tsmContainer
            // 
            this.tsmContainer.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.tsmContainer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbClose,
            this.tssSeparator1,
            this.tsbSample,
            this.tsmConnectToPA,
            this.btnExport});
            this.tsmContainer.Location = new System.Drawing.Point(0, 0);
            this.tsmContainer.Name = "tsmContainer";
            this.tsmContainer.Size = new System.Drawing.Size(1010, 25);
            this.tsmContainer.TabIndex = 4;
            this.tsmContainer.Text = "toolStrip1";
            // 
            // tsbClose
            // 
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(86, 22);
            this.tsbClose.Text = "Close this tool";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            // 
            // tssSeparator1
            // 
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbSample
            // 
            this.tsbSample.Name = "tsbSample";
            this.tsbSample.Size = new System.Drawing.Size(23, 22);
            // 
            // tsmConnectToPA
            // 
            this.tsmConnectToPA.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsmConnectToPA.Name = "tsmConnectToPA";
            this.tsmConnectToPA.Size = new System.Drawing.Size(183, 22);
            this.tsmConnectToPA.Text = "Connect to Power Automate API";
            this.tsmConnectToPA.Click += new System.EventHandler(this.tsmConnectToPA_ItemClicked);
            // 
            // btnExport
            // 
            this.btnExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(105, 22);
            this.btnExport.Text = "Export CSV / Excel";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click_1);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 25);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(2);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.gbFlows);
            this.splitContainerMain.Panel1.Controls.Add(this.gbFlowFilters);
            this.splitContainerMain.Panel1.Controls.Add(this.gbSolution);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.gbFlowRuns);
            this.splitContainerMain.Panel2.Controls.Add(this.gbRunFilters);
            this.splitContainerMain.Size = new System.Drawing.Size(1010, 540);
            this.splitContainerMain.SplitterDistance = 274;
            this.splitContainerMain.SplitterWidth = 3;
            this.splitContainerMain.TabIndex = 5;
            // 
            // gbFlows
            // 
            this.gbFlows.Controls.Add(this.clbFlows);
            this.gbFlows.Controls.Add(this.cbSelectAllFlows);
            this.gbFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbFlows.Location = new System.Drawing.Point(0, 104);
            this.gbFlows.Margin = new System.Windows.Forms.Padding(2);
            this.gbFlows.Name = "gbFlows";
            this.gbFlows.Padding = new System.Windows.Forms.Padding(5);
            this.gbFlows.Size = new System.Drawing.Size(274, 436);
            this.gbFlows.TabIndex = 2;
            this.gbFlows.TabStop = false;
            this.gbFlows.Text = "Flows";
            // 
            // clbFlows
            // 
            this.clbFlows.CheckOnClick = true;
            this.clbFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbFlows.FormattingEnabled = true;
            this.clbFlows.Location = new System.Drawing.Point(5, 39);
            this.clbFlows.Margin = new System.Windows.Forms.Padding(2);
            this.clbFlows.Name = "clbFlows";
            this.clbFlows.Size = new System.Drawing.Size(264, 392);
            this.clbFlows.TabIndex = 1;
            // 
            // cbSelectAllFlows
            // 
            this.cbSelectAllFlows.AutoSize = true;
            this.cbSelectAllFlows.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbSelectAllFlows.Location = new System.Drawing.Point(5, 18);
            this.cbSelectAllFlows.Margin = new System.Windows.Forms.Padding(2);
            this.cbSelectAllFlows.Name = "cbSelectAllFlows";
            this.cbSelectAllFlows.Padding = new System.Windows.Forms.Padding(3, 2, 0, 2);
            this.cbSelectAllFlows.Size = new System.Drawing.Size(264, 21);
            this.cbSelectAllFlows.TabIndex = 0;
            this.cbSelectAllFlows.Text = "Select All";
            this.cbSelectAllFlows.CheckedChanged += new System.EventHandler(this.cbSelectAllFlows_CheckedChanged);
            // 
            // gbFlowFilters
            // 
            this.gbFlowFilters.Controls.Add(this.tbSearch);
            this.gbFlowFilters.Controls.Add(this.lblSearch);
            this.gbFlowFilters.Controls.Add(this.cbxFlowStatusDraft);
            this.gbFlowFilters.Controls.Add(this.cbxFlowStatusActivated);
            this.gbFlowFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbFlowFilters.Location = new System.Drawing.Point(0, 39);
            this.gbFlowFilters.Margin = new System.Windows.Forms.Padding(2);
            this.gbFlowFilters.Name = "gbFlowFilters";
            this.gbFlowFilters.Padding = new System.Windows.Forms.Padding(5);
            this.gbFlowFilters.Size = new System.Drawing.Size(274, 65);
            this.gbFlowFilters.TabIndex = 1;
            this.gbFlowFilters.TabStop = false;
            this.gbFlowFilters.Text = "Flow Filters";
            // 
            // tbSearch
            // 
            this.tbSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSearch.Location = new System.Drawing.Point(48, 36);
            this.tbSearch.Margin = new System.Windows.Forms.Padding(2);
            this.tbSearch.Name = "tbSearch";
            this.tbSearch.Size = new System.Drawing.Size(219, 20);
            this.tbSearch.TabIndex = 3;
            this.tbSearch.TextChanged += new System.EventHandler(this.tbSearch_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(8, 37);
            this.lblSearch.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(44, 13);
            this.lblSearch.TabIndex = 2;
            this.lblSearch.Text = "Search:";
            // 
            // cbxFlowStatusDraft
            // 
            this.cbxFlowStatusDraft.AutoSize = true;
            this.cbxFlowStatusDraft.Location = new System.Drawing.Point(74, 16);
            this.cbxFlowStatusDraft.Margin = new System.Windows.Forms.Padding(2);
            this.cbxFlowStatusDraft.Name = "cbxFlowStatusDraft";
            this.cbxFlowStatusDraft.Size = new System.Drawing.Size(49, 17);
            this.cbxFlowStatusDraft.TabIndex = 1;
            this.cbxFlowStatusDraft.Text = "Draft";
            // 
            // cbxFlowStatusActivated
            // 
            this.cbxFlowStatusActivated.AutoSize = true;
            this.cbxFlowStatusActivated.Checked = true;
            this.cbxFlowStatusActivated.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxFlowStatusActivated.Location = new System.Drawing.Point(8, 16);
            this.cbxFlowStatusActivated.Margin = new System.Windows.Forms.Padding(2);
            this.cbxFlowStatusActivated.Name = "cbxFlowStatusActivated";
            this.cbxFlowStatusActivated.Size = new System.Drawing.Size(71, 17);
            this.cbxFlowStatusActivated.TabIndex = 0;
            this.cbxFlowStatusActivated.Text = "Activated";
            // 
            // gbSolution
            // 
            this.gbSolution.Controls.Add(this.cbSolutions);
            this.gbSolution.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbSolution.Location = new System.Drawing.Point(0, 0);
            this.gbSolution.Margin = new System.Windows.Forms.Padding(2);
            this.gbSolution.Name = "gbSolution";
            this.gbSolution.Padding = new System.Windows.Forms.Padding(5);
            this.gbSolution.Size = new System.Drawing.Size(274, 39);
            this.gbSolution.TabIndex = 0;
            this.gbSolution.TabStop = false;
            this.gbSolution.Text = "Solution";
            // 
            // cbSolutions
            // 
            this.cbSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbSolutions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSolutions.FormattingEnabled = true;
            this.cbSolutions.Location = new System.Drawing.Point(5, 18);
            this.cbSolutions.Margin = new System.Windows.Forms.Padding(2);
            this.cbSolutions.Name = "cbSolutions";
            this.cbSolutions.Size = new System.Drawing.Size(264, 21);
            this.cbSolutions.TabIndex = 0;
            this.cbSolutions.SelectedIndexChanged += new System.EventHandler(this.cbSolutions_SelectedIndexChanged);
            // 
            // gbFlowRuns
            // 
            this.gbFlowRuns.Controls.Add(this.dataGridView1);
            this.gbFlowRuns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbFlowRuns.Location = new System.Drawing.Point(0, 46);
            this.gbFlowRuns.Margin = new System.Windows.Forms.Padding(2);
            this.gbFlowRuns.Name = "gbFlowRuns";
            this.gbFlowRuns.Padding = new System.Windows.Forms.Padding(5);
            this.gbFlowRuns.Size = new System.Drawing.Size(733, 494);
            this.gbFlowRuns.TabIndex = 1;
            this.gbFlowRuns.TabStop = false;
            this.gbFlowRuns.Text = "Flow Runs";
            // 
            // gbRunFilters
            // 
            this.gbRunFilters.Controls.Add(this.btnFetchHistory);
            this.gbRunFilters.Controls.Add(this.cmbStatus);
            this.gbRunFilters.Controls.Add(this.lblStatus);
            this.gbRunFilters.Controls.Add(this.dtpDateTo);
            this.gbRunFilters.Controls.Add(this.lblDateTo);
            this.gbRunFilters.Controls.Add(this.dtpDateFrom);
            this.gbRunFilters.Controls.Add(this.lblDateFrom);
            this.gbRunFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbRunFilters.Location = new System.Drawing.Point(0, 0);
            this.gbRunFilters.Margin = new System.Windows.Forms.Padding(2);
            this.gbRunFilters.Name = "gbRunFilters";
            this.gbRunFilters.Padding = new System.Windows.Forms.Padding(5);
            this.gbRunFilters.Size = new System.Drawing.Size(733, 46);
            this.gbRunFilters.TabIndex = 0;
            this.gbRunFilters.TabStop = false;
            this.gbRunFilters.Text = "Run Filters";
            // 
            // btnFetchHistory
            // 
            this.btnFetchHistory.Location = new System.Drawing.Point(453, 15);
            this.btnFetchHistory.Margin = new System.Windows.Forms.Padding(2);
            this.btnFetchHistory.Name = "btnFetchHistory";
            this.btnFetchHistory.Size = new System.Drawing.Size(67, 21);
            this.btnFetchHistory.TabIndex = 6;
            this.btnFetchHistory.Text = "Get Runs";
            this.btnFetchHistory.UseVisualStyleBackColor = true;
            this.btnFetchHistory.Click += new System.EventHandler(this.btnFetchHistory_Click_1);
            // 
            // cmbStatus
            // 
            this.cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStatus.FormattingEnabled = true;
            this.cmbStatus.Items.AddRange(new object[] {
            "All",
            "Succeeded",
            "Failed",
            "Cancelled",
            "Running"});
            this.cmbStatus.Location = new System.Drawing.Point(362, 18);
            this.cmbStatus.Margin = new System.Windows.Forms.Padding(2);
            this.cmbStatus.Name = "cmbStatus";
            this.cmbStatus.Size = new System.Drawing.Size(81, 21);
            this.cmbStatus.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(323, 20);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(40, 13);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Status:";
            // 
            // dtpDateTo
            // 
            this.dtpDateTo.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDateTo.Location = new System.Drawing.Point(192, 18);
            this.dtpDateTo.Margin = new System.Windows.Forms.Padding(2);
            this.dtpDateTo.Name = "dtpDateTo";
            this.dtpDateTo.Size = new System.Drawing.Size(121, 20);
            this.dtpDateTo.TabIndex = 3;
            // 
            // lblDateTo
            // 
            this.lblDateTo.AutoSize = true;
            this.lblDateTo.Location = new System.Drawing.Point(170, 20);
            this.lblDateTo.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblDateTo.Name = "lblDateTo";
            this.lblDateTo.Size = new System.Drawing.Size(23, 13);
            this.lblDateTo.TabIndex = 2;
            this.lblDateTo.Text = "To:";
            // 
            // dtpDateFrom
            // 
            this.dtpDateFrom.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDateFrom.Location = new System.Drawing.Point(41, 18);
            this.dtpDateFrom.Margin = new System.Windows.Forms.Padding(2);
            this.dtpDateFrom.Name = "dtpDateFrom";
            this.dtpDateFrom.Size = new System.Drawing.Size(121, 20);
            this.dtpDateFrom.TabIndex = 1;
            // 
            // lblDateFrom
            // 
            this.lblDateFrom.AutoSize = true;
            this.lblDateFrom.Location = new System.Drawing.Point(8, 20);
            this.lblDateFrom.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblDateFrom.Name = "lblDateFrom";
            this.lblDateFrom.Size = new System.Drawing.Size(33, 13);
            this.lblDateFrom.TabIndex = 0;
            this.lblDateFrom.Text = "From:";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FlowRunStatus,
            this.FlowRunDuration,
            this.FlowRunError});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(5, 18);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(2);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(723, 471);
            this.dataGridView1.TabIndex = 0;
            // 
            // FlowRunStatus
            // 
            this.FlowRunStatus.DataPropertyName = "Status";
            this.FlowRunStatus.FillWeight = 60F;
            this.FlowRunStatus.HeaderText = "Status";
            this.FlowRunStatus.MinimumWidth = 6;
            this.FlowRunStatus.Name = "FlowRunStatus";
            this.FlowRunStatus.ReadOnly = true;
            // 
            // FlowRunDuration
            // 
            this.FlowRunDuration.DataPropertyName = "FormattedDuration";
            this.FlowRunDuration.FillWeight = 70F;
            this.FlowRunDuration.HeaderText = "Duration";
            this.FlowRunDuration.MinimumWidth = 6;
            this.FlowRunDuration.Name = "FlowRunDuration";
            this.FlowRunDuration.ReadOnly = true;
            // 
            // FlowRunError
            // 
            this.FlowRunError.DataPropertyName = "ErrorDetails";
            this.FlowRunError.HeaderText = "Error";
            this.FlowRunError.MinimumWidth = 6;
            this.FlowRunError.Name = "FlowRunError";
            this.FlowRunError.ReadOnly = true;
            // 
            // MyPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.tsmContainer);
            this.Name = "MyPluginControl";
            this.Size = new System.Drawing.Size(1010, 565);
            this.Load += new System.EventHandler(this.MyPluginControl_Load);
            this.tsmContainer.ResumeLayout(false);
            this.tsmContainer.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.gbFlows.ResumeLayout(false);
            this.gbFlows.PerformLayout();
            this.gbFlowFilters.ResumeLayout(false);
            this.gbFlowFilters.PerformLayout();
            this.gbSolution.ResumeLayout(false);
            this.gbFlowRuns.ResumeLayout(false);
            this.gbRunFilters.ResumeLayout(false);
            this.gbRunFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip tsmContainer;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripButton tsbSample;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.GroupBox gbFlows;
        private System.Windows.Forms.CheckedListBox clbFlows;
        private System.Windows.Forms.CheckBox cbSelectAllFlows;
        private System.Windows.Forms.GroupBox gbFlowFilters;
        private System.Windows.Forms.TextBox tbSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.CheckBox cbxFlowStatusDraft;
        private System.Windows.Forms.CheckBox cbxFlowStatusActivated;
        private System.Windows.Forms.GroupBox gbSolution;
        private System.Windows.Forms.ComboBox cbSolutions;
        private System.Windows.Forms.GroupBox gbFlowRuns;
        private System.Windows.Forms.GroupBox gbRunFilters;
        private System.Windows.Forms.Button btnFetchHistory;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DateTimePicker dtpDateTo;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.DateTimePicker dtpDateFrom;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.ToolStripButton tsmConnectToPA;
        private System.Windows.Forms.ToolStripButton btnExport;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunError;
    }
}
