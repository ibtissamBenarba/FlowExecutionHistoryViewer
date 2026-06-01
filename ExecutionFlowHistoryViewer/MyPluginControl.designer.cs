using System.Drawing;
using System.Windows.Forms;

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
            System.Windows.Forms.ToolStripButton tsmConnectToPA;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MyPluginControl));
            this.tsmContainer = new System.Windows.Forms.ToolStrip();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExport = new System.Windows.Forms.ToolStripButton();
            this.tsbAiAssistant = new System.Windows.Forms.ToolStripButton();
            this.tsbDarkMode = new System.Windows.Forms.ToolStripButton();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.gbFlows = new System.Windows.Forms.GroupBox();
            this.clbFlows = new System.Windows.Forms.CheckedListBox();
            this.cbSelectAllFlows = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.gbFlowFilters = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbSolutions = new System.Windows.Forms.ComboBox();
            this.tbSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.cbxFlowStatusDraft = new System.Windows.Forms.CheckBox();
            this.cbxFlowStatusActivated = new System.Windows.Forms.CheckBox();
            this.gbFlowRuns = new System.Windows.Forms.GroupBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.FlowName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FlowRunId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FlowRunStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StartDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EndDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Duration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Action = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Details = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tsPagination = new System.Windows.Forms.ToolStrip();
            this.tslItems = new System.Windows.Forms.ToolStripLabel();
            this.tscNumberOfRuns = new System.Windows.Forms.ToolStripComboBox();
            this.tslTotalItems = new System.Windows.Forms.ToolStripLabel();
            this.tsbSkipNext = new System.Windows.Forms.ToolStripButton();
            this.tsbNext = new System.Windows.Forms.ToolStripButton();
            this.tslPageNumber = new System.Windows.Forms.ToolStripLabel();
            this.tstbPageNumber = new System.Windows.Forms.ToolStripTextBox();
            this.tsbPrevious = new System.Windows.Forms.ToolStripButton();
            this.tsbSkipPrevious = new System.Windows.Forms.ToolStripButton();
            this.gbDeepSearch = new System.Windows.Forms.GroupBox();
            this.btnCheckSafety = new System.Windows.Forms.Button();
            this.btnResubmit = new System.Windows.Forms.Button();
            this.lblDeepSearchStatus = new System.Windows.Forms.Label();
            this.progressBarDeepSearch = new System.Windows.Forms.ProgressBar();
            this.btnClearDeepSearch = new System.Windows.Forms.Button();
            this.btnDeepSearch = new System.Windows.Forms.Button();
            this.tbDeepSearch = new System.Windows.Forms.TextBox();
            this.lblDeepSearch = new System.Windows.Forms.Label();
            this.gbRunFilters = new System.Windows.Forms.GroupBox();
            this.btnFetchHistory = new System.Windows.Forms.Button();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.dtpDateTo = new System.Windows.Forms.DateTimePicker();
            this.lblDateTo = new System.Windows.Forms.Label();
            this.dtpDateFrom = new System.Windows.Forms.DateTimePicker();
            this.lblDateFrom = new System.Windows.Forms.Label();
            this.vS2015BlueTheme1 = new WeifenLuo.WinFormsUI.Docking.VS2015BlueTheme();
            tsmConnectToPA = new System.Windows.Forms.ToolStripButton();
            this.tsmContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.gbFlows.SuspendLayout();
            this.gbFlowFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.gbFlowRuns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tsPagination.SuspendLayout();
            this.gbDeepSearch.SuspendLayout();
            this.gbRunFilters.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsmConnectToPA
            // 
            tsmConnectToPA.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))));
            tsmConnectToPA.DoubleClickEnabled = true;
            tsmConnectToPA.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            tsmConnectToPA.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            tsmConnectToPA.Image = ((System.Drawing.Image)(resources.GetObject("tsmConnectToPA.Image")));
            tsmConnectToPA.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            tsmConnectToPA.Name = "tsmConnectToPA";
            tsmConnectToPA.Size = new System.Drawing.Size(255, 28);
            tsmConnectToPA.Text = "Connect to Power Automate API";
            tsmConnectToPA.Click += new System.EventHandler(this.tsmConnectToPA_ItemClicked);
            // 
            // tsmContainer
            // 
            this.tsmContainer.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.tsmContainer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbClose,
            this.tssSeparator1,
            tsmConnectToPA,
            this.btnExport,
            this.tsbDarkMode,
            this.tsbAiAssistant});
            this.tsmContainer.Location = new System.Drawing.Point(0, 0);
            this.tsmContainer.Name = "tsmContainer";
            this.tsmContainer.Size = new System.Drawing.Size(1347, 31);
            this.tsmContainer.TabIndex = 4;
            this.tsmContainer.Text = "toolStrip1";
            // 
            // tsbClose
            // 
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(107, 28);
            this.tsbClose.Text = "Close this tool";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            // 
            // tssSeparator1
            // 
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(242)))), ((int)(((byte)(234)))));
            this.btnExport.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExport.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(115)))), ((int)(((byte)(70)))));
            this.btnExport.Image = ((System.Drawing.Image)(resources.GetObject("btnExport.Image")));
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(162, 28);
            this.btnExport.Text = "Export CSV / Excel";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click_1);
            // 
            // tsbAiAssistant
            // 
            this.tsbAiAssistant.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAiAssistant.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbAiAssistant.Name = "tsbAiAssistant";
            this.tsbAiAssistant.Size = new System.Drawing.Size(120, 24);
            this.tsbAiAssistant.Text = "✨ Global AI";
            this.tsbAiAssistant.Click += new System.EventHandler(this.tsbAiAssistant_Click);
            // 
            // tsbDarkMode
            // 
            this.tsbDarkMode.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbDarkMode.AutoSize = false;
            this.tsbDarkMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.tsbDarkMode.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.tsbDarkMode.ForeColor = System.Drawing.Color.White;
            this.tsbDarkMode.Image = global::ExecutionFlowHistoryViewer.Properties.Resources.moon_stars_16dp_1F1F1F_FILL0_wght400_GRAD0_opsz20;
            this.tsbDarkMode.Margin = new System.Windows.Forms.Padding(0, 2, 8, 2);
            this.tsbDarkMode.Name = "tsbDarkMode";
            this.tsbDarkMode.Size = new System.Drawing.Size(120, 23);
            this.tsbDarkMode.Text = "Dark";
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 31);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.gbFlows);
            this.splitContainerMain.Panel1.Controls.Add(this.gbFlowFilters);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.gbFlowRuns);
            this.splitContainerMain.Panel2.Controls.Add(this.gbDeepSearch);
            this.splitContainerMain.Panel2.Controls.Add(this.gbRunFilters);
            this.splitContainerMain.Size = new System.Drawing.Size(1347, 664);
            this.splitContainerMain.SplitterDistance = 365;
            this.splitContainerMain.TabIndex = 5;
            // 
            // gbFlows
            // 
            this.gbFlows.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.gbFlows.Controls.Add(this.clbFlows);
            this.gbFlows.Controls.Add(this.cbSelectAllFlows);
            this.gbFlows.Controls.Add(this.label4);
            this.gbFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbFlows.Location = new System.Drawing.Point(0, 254);
            this.gbFlows.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbFlows.Name = "gbFlows";
            this.gbFlows.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.gbFlows.Size = new System.Drawing.Size(365, 410);
            this.gbFlows.TabIndex = 2;
            this.gbFlows.TabStop = false;
            // 
            // clbFlows
            // 
            this.clbFlows.CheckOnClick = true;
            this.clbFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbFlows.FormattingEnabled = true;
            this.clbFlows.Location = new System.Drawing.Point(7, 67);
            this.clbFlows.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.clbFlows.Name = "clbFlows";
            this.clbFlows.Size = new System.Drawing.Size(351, 337);
            this.clbFlows.TabIndex = 1;
            // 
            // cbSelectAllFlows
            // 
            this.cbSelectAllFlows.AutoSize = true;
            this.cbSelectAllFlows.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbSelectAllFlows.Location = new System.Drawing.Point(7, 43);
            this.cbSelectAllFlows.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbSelectAllFlows.Name = "cbSelectAllFlows";
            this.cbSelectAllFlows.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.cbSelectAllFlows.Size = new System.Drawing.Size(351, 24);
            this.cbSelectAllFlows.TabIndex = 0;
            this.cbSelectAllFlows.Text = "Select All";
            this.cbSelectAllFlows.CheckedChanged += new System.EventHandler(this.cbSelectAllFlows_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Top;
            this.label4.Font = new System.Drawing.Font("Microsoft Tai Le", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.label4.Location = new System.Drawing.Point(7, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 22);
            this.label4.TabIndex = 7;
            this.label4.Text = "FLOWS";
            // 
            // gbFlowFilters
            // 
            this.gbFlowFilters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.gbFlowFilters.Controls.Add(this.pictureBox1);
            this.gbFlowFilters.Controls.Add(this.label3);
            this.gbFlowFilters.Controls.Add(this.label2);
            this.gbFlowFilters.Controls.Add(this.label1);
            this.gbFlowFilters.Controls.Add(this.cbSolutions);
            this.gbFlowFilters.Controls.Add(this.tbSearch);
            this.gbFlowFilters.Controls.Add(this.lblSearch);
            this.gbFlowFilters.Controls.Add(this.cbxFlowStatusDraft);
            this.gbFlowFilters.Controls.Add(this.cbxFlowStatusActivated);
            this.gbFlowFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbFlowFilters.Location = new System.Drawing.Point(0, 0);
            this.gbFlowFilters.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbFlowFilters.Name = "gbFlowFilters";
            this.gbFlowFilters.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.gbFlowFilters.Size = new System.Drawing.Size(365, 254);
            this.gbFlowFilters.TabIndex = 1;
            this.gbFlowFilters.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(11, 17);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Tai Le", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.label3.Location = new System.Drawing.Point(41, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 22);
            this.label3.TabIndex = 6;
            this.label3.Text = "FILTERS";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label2.Location = new System.Drawing.Point(17, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 19);
            this.label2.TabIndex = 5;
            this.label2.Text = "Solution";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 129);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 19);
            this.label1.TabIndex = 4;
            this.label1.Text = "Flow Status";
            // 
            // cbSolutions
            // 
            this.cbSolutions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSolutions.FormattingEnabled = true;
            this.cbSolutions.Location = new System.Drawing.Point(20, 84);
            this.cbSolutions.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbSolutions.Name = "cbSolutions";
            this.cbSolutions.Size = new System.Drawing.Size(325, 24);
            this.cbSolutions.TabIndex = 0;
            this.cbSolutions.SelectedIndexChanged += new System.EventHandler(this.cbSolutions_SelectedIndexChanged);
            // 
            // tbSearch
            // 
            this.tbSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSearch.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbSearch.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.tbSearch.Location = new System.Drawing.Point(17, 216);
            this.tbSearch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbSearch.Name = "tbSearch";
            this.tbSearch.Size = new System.Drawing.Size(325, 27);
            this.tbSearch.TabIndex = 3;
            this.tbSearch.TextChanged += new System.EventHandler(this.tbSearch_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSearch.Location = new System.Drawing.Point(17, 193);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(97, 19);
            this.lblSearch.TabIndex = 2;
            this.lblSearch.Text = "Search flows";
            // 
            // cbxFlowStatusDraft
            // 
            this.cbxFlowStatusDraft.AutoSize = true;
            this.cbxFlowStatusDraft.Location = new System.Drawing.Point(140, 150);
            this.cbxFlowStatusDraft.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxFlowStatusDraft.Name = "cbxFlowStatusDraft";
            this.cbxFlowStatusDraft.Size = new System.Drawing.Size(57, 20);
            this.cbxFlowStatusDraft.TabIndex = 1;
            this.cbxFlowStatusDraft.Text = "Draft";
            // 
            // cbxFlowStatusActivated
            // 
            this.cbxFlowStatusActivated.AutoSize = true;
            this.cbxFlowStatusActivated.Checked = true;
            this.cbxFlowStatusActivated.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxFlowStatusActivated.Location = new System.Drawing.Point(21, 150);
            this.cbxFlowStatusActivated.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxFlowStatusActivated.Name = "cbxFlowStatusActivated";
            this.cbxFlowStatusActivated.Size = new System.Drawing.Size(85, 20);
            this.cbxFlowStatusActivated.TabIndex = 0;
            this.cbxFlowStatusActivated.Text = "Activated";
            // 
            // gbFlowRuns
            // 
            this.gbFlowRuns.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.gbFlowRuns.Controls.Add(this.dataGridView1);
            this.gbFlowRuns.Controls.Add(this.tsPagination);
            this.gbFlowRuns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbFlowRuns.Location = new System.Drawing.Point(0, 119);
            this.gbFlowRuns.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbFlowRuns.Name = "gbFlowRuns";
            this.gbFlowRuns.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.gbFlowRuns.Size = new System.Drawing.Size(978, 545);
            this.gbFlowRuns.TabIndex = 1;
            this.gbFlowRuns.TabStop = false;
            this.gbFlowRuns.Text = "Flow Runs";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FlowName,
            this.FlowRunId,
            this.FlowRunStatus,
            this.StartDate,
            this.EndDate,
            this.Duration,
            this.Action,
            this.Details});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(7, 21);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(964, 478);
            this.dataGridView1.TabIndex = 0;
            // 
            // FlowName
            // 
            this.FlowName.HeaderText = "Flow Name";
            this.FlowName.MinimumWidth = 6;
            this.FlowName.Name = "FlowName";
            // 
            // FlowRunId
            // 
            this.FlowRunId.HeaderText = "Run ID";
            this.FlowRunId.MinimumWidth = 6;
            this.FlowRunId.Name = "FlowRunId";
            // 
            // FlowRunStatus
            // 
            this.FlowRunStatus.HeaderText = "Status";
            this.FlowRunStatus.MinimumWidth = 6;
            this.FlowRunStatus.Name = "FlowRunStatus";
            // 
            // StartDate
            // 
            this.StartDate.HeaderText = "Start Time";
            this.StartDate.MinimumWidth = 6;
            this.StartDate.Name = "StartDate";
            // 
            // EndDate
            // 
            this.EndDate.HeaderText = "End Time";
            this.EndDate.MinimumWidth = 6;
            this.EndDate.Name = "EndDate";
            // 
            // Duration
            // 
            this.Duration.HeaderText = "Duration";
            this.Duration.MinimumWidth = 6;
            this.Duration.Name = "Duration";
            // 
            // Action
            // 
            this.Action.HeaderText = "Action";
            this.Action.MinimumWidth = 6;
            this.Action.Name = "Action";
            // 
            // Details
            // 
            this.Details.HeaderText = "Details";
            this.Details.MinimumWidth = 6;
            this.Details.Name = "Details";
            // 
            // tsPagination
            // 
            this.tsPagination.AutoSize = false;
            this.tsPagination.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tsPagination.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tsPagination.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslItems,
            this.tscNumberOfRuns,
            this.tslTotalItems,
            this.tsbSkipNext,
            this.tsbNext,
            this.tslPageNumber,
            this.tstbPageNumber,
            this.tsbPrevious,
            this.tsbSkipPrevious});
            this.tsPagination.Location = new System.Drawing.Point(7, 499);
            this.tsPagination.Name = "tsPagination";
            this.tsPagination.Size = new System.Drawing.Size(964, 40);
            this.tsPagination.TabIndex = 1;
            this.tsPagination.Text = "toolStrip1";
            // 
            // tslItems
            // 
            this.tslItems.Margin = new System.Windows.Forms.Padding(10, 1, 5, 2);
            this.tslItems.Name = "tslItems";
            this.tslItems.Size = new System.Drawing.Size(109, 37);
            this.tslItems.Text = "Items per page";
            // 
            // tscNumberOfRuns
            // 
            this.tscNumberOfRuns.AutoCompleteCustomSource.AddRange(new string[] {
            "25",
            "50",
            "100"});
            this.tscNumberOfRuns.AutoSize = false;
            this.tscNumberOfRuns.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.tscNumberOfRuns.Name = "tscNumberOfRuns";
            this.tscNumberOfRuns.Size = new System.Drawing.Size(55, 28);
            this.tscNumberOfRuns.Text = "25";
            // 
            // tslTotalItems
            // 
            this.tslTotalItems.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.tslTotalItems.Margin = new System.Windows.Forms.Padding(5, 1, 0, 2);
            this.tslTotalItems.Name = "tslTotalItems";
            this.tslTotalItems.Size = new System.Drawing.Size(133, 37);
            this.tslTotalItems.Text = "1 - 25 of 500 items";
            // 
            // tsbSkipNext
            // 
            this.tsbSkipNext.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbSkipNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSkipNext.Image = ((System.Drawing.Image)(resources.GetObject("tsbSkipNext.Image")));
            this.tsbSkipNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSkipNext.Margin = new System.Windows.Forms.Padding(10, 5, 20, 2);
            this.tsbSkipNext.Name = "tsbSkipNext";
            this.tsbSkipNext.RightToLeftAutoMirrorImage = true;
            this.tsbSkipNext.Size = new System.Drawing.Size(29, 33);
            this.tsbSkipNext.Text = "tsbSkipNext";
            this.tsbSkipNext.Click += new System.EventHandler(this.tsbSkipNext_Click);
            // 
            // tsbNext
            // 
            this.tsbNext.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbNext.Image = ((System.Drawing.Image)(resources.GetObject("tsbNext.Image")));
            this.tsbNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbNext.Margin = new System.Windows.Forms.Padding(10, 5, 10, 2);
            this.tsbNext.Name = "tsbNext";
            this.tsbNext.Size = new System.Drawing.Size(64, 33);
            this.tsbNext.Text = "Next";
            this.tsbNext.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.tsbNext.Click += new System.EventHandler(this.tsbNext_Click);
            // 
            // tslPageNumber
            // 
            this.tslPageNumber.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tslPageNumber.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.tslPageNumber.Margin = new System.Windows.Forms.Padding(4, 5, 10, 2);
            this.tslPageNumber.Name = "tslPageNumber";
            this.tslPageNumber.Size = new System.Drawing.Size(43, 33);
            this.tslPageNumber.Text = "of 20";
            // 
            // tstbPageNumber
            // 
            this.tstbPageNumber.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tstbPageNumber.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.tstbPageNumber.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tstbPageNumber.ForeColor = System.Drawing.SystemColors.Window;
            this.tstbPageNumber.Margin = new System.Windows.Forms.Padding(10, 5, 4, 2);
            this.tstbPageNumber.Name = "tstbPageNumber";
            this.tstbPageNumber.ShortcutsEnabled = false;
            this.tstbPageNumber.Size = new System.Drawing.Size(40, 33);
            this.tstbPageNumber.Text = "10";
            this.tstbPageNumber.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // tsbPrevious
            // 
            this.tsbPrevious.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbPrevious.Image = ((System.Drawing.Image)(resources.GetObject("tsbPrevious.Image")));
            this.tsbPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbPrevious.Margin = new System.Windows.Forms.Padding(10, 5, 10, 2);
            this.tsbPrevious.Name = "tsbPrevious";
            this.tsbPrevious.Size = new System.Drawing.Size(88, 33);
            this.tsbPrevious.Text = "Previous";
            this.tsbPrevious.Click += new System.EventHandler(this.tsbPrevious_Click);
            // 
            // tsbSkipPrevious
            // 
            this.tsbSkipPrevious.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbSkipPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSkipPrevious.Image = ((System.Drawing.Image)(resources.GetObject("tsbSkipPrevious.Image")));
            this.tsbSkipPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSkipPrevious.Margin = new System.Windows.Forms.Padding(10, 5, 10, 2);
            this.tsbSkipPrevious.Name = "tsbSkipPrevious";
            this.tsbSkipPrevious.Size = new System.Drawing.Size(29, 33);
            this.tsbSkipPrevious.Text = "tsbSkipPrevious";
            this.tsbSkipPrevious.Click += new System.EventHandler(this.tsbSkipPrevious_Click);
            // 
            // gbDeepSearch
            // 
            this.gbDeepSearch.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.gbDeepSearch.Controls.Add(this.btnCheckSafety);
            this.gbDeepSearch.Controls.Add(this.btnResubmit);
            this.gbDeepSearch.Controls.Add(this.lblDeepSearchStatus);
            this.gbDeepSearch.Controls.Add(this.progressBarDeepSearch);
            this.gbDeepSearch.Controls.Add(this.btnClearDeepSearch);
            this.gbDeepSearch.Controls.Add(this.btnDeepSearch);
            this.gbDeepSearch.Controls.Add(this.tbDeepSearch);
            this.gbDeepSearch.Controls.Add(this.lblDeepSearch);
            this.gbDeepSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbDeepSearch.Location = new System.Drawing.Point(0, 57);
            this.gbDeepSearch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbDeepSearch.Name = "gbDeepSearch";
            this.gbDeepSearch.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.gbDeepSearch.Size = new System.Drawing.Size(978, 62);
            this.gbDeepSearch.TabIndex = 2;
            this.gbDeepSearch.TabStop = false;
            this.gbDeepSearch.Text = "🔍 Search in Run Details";
            // 
            // btnCheckSafety
            // 
            this.btnCheckSafety.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.btnCheckSafety.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCheckSafety.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCheckSafety.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCheckSafety.ForeColor = System.Drawing.Color.Green;
            this.btnCheckSafety.Location = new System.Drawing.Point(735, 21);
            this.btnCheckSafety.Name = "btnCheckSafety";
            this.btnCheckSafety.Size = new System.Drawing.Size(144, 35);
            this.btnCheckSafety.TabIndex = 7;
            this.btnCheckSafety.Text = "Check Safety";
            this.btnCheckSafety.UseVisualStyleBackColor = false;
            this.btnCheckSafety.Click += new System.EventHandler(this.btnCheckSafety_Click);
            // 
            // btnResubmit
            // 
            this.btnResubmit.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnResubmit.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnResubmit.Location = new System.Drawing.Point(879, 21);
            this.btnResubmit.Name = "btnResubmit";
            this.btnResubmit.Size = new System.Drawing.Size(92, 35);
            this.btnResubmit.TabIndex = 6;
            this.btnResubmit.Text = "Resubmit";
            this.btnResubmit.UseVisualStyleBackColor = true;
            this.btnResubmit.Click += new System.EventHandler(this.btnResubmit_Click);
            // 
            // lblDeepSearchStatus
            // 
            this.lblDeepSearchStatus.AutoSize = true;
            this.lblDeepSearchStatus.Location = new System.Drawing.Point(752, 28);
            this.lblDeepSearchStatus.Name = "lblDeepSearchStatus";
            this.lblDeepSearchStatus.Size = new System.Drawing.Size(0, 16);
            this.lblDeepSearchStatus.TabIndex = 5;
            // 
            // progressBarDeepSearch
            // 
            this.progressBarDeepSearch.Location = new System.Drawing.Point(707, 33);
            this.progressBarDeepSearch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.progressBarDeepSearch.Name = "progressBarDeepSearch";
            this.progressBarDeepSearch.Size = new System.Drawing.Size(150, 18);
            this.progressBarDeepSearch.TabIndex = 4;
            this.progressBarDeepSearch.Visible = false;
            // 
            // btnClearDeepSearch
            // 
            this.btnClearDeepSearch.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.btnClearDeepSearch.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnClearDeepSearch.Image = ((System.Drawing.Image)(resources.GetObject("btnClearDeepSearch.Image")));
            this.btnClearDeepSearch.Location = new System.Drawing.Point(595, 21);
            this.btnClearDeepSearch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnClearDeepSearch.Name = "btnClearDeepSearch";
            this.btnClearDeepSearch.Size = new System.Drawing.Size(92, 35);
            this.btnClearDeepSearch.TabIndex = 3;
            this.btnClearDeepSearch.Text = "Clear";
            this.btnClearDeepSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClearDeepSearch.UseVisualStyleBackColor = false;
            this.btnClearDeepSearch.Click += new System.EventHandler(this.btnClearDeepSearch_Click);
            // 
            // btnDeepSearch
            // 
            this.btnDeepSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnDeepSearch.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnDeepSearch.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnDeepSearch.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDeepSearch.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.btnDeepSearch.Image = ((System.Drawing.Image)(resources.GetObject("btnDeepSearch.Image")));
            this.btnDeepSearch.Location = new System.Drawing.Point(469, 21);
            this.btnDeepSearch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnDeepSearch.Name = "btnDeepSearch";
            this.btnDeepSearch.Size = new System.Drawing.Size(110, 35);
            this.btnDeepSearch.TabIndex = 2;
            this.btnDeepSearch.Text = "Search";
            this.btnDeepSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDeepSearch.UseVisualStyleBackColor = false;
            this.btnDeepSearch.Click += new System.EventHandler(this.btnDeepSearch_Click);
            // 
            // tbDeepSearch
            // 
            this.tbDeepSearch.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbDeepSearch.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.tbDeepSearch.Location = new System.Drawing.Point(108, 25);
            this.tbDeepSearch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbDeepSearch.Name = "tbDeepSearch";
            this.tbDeepSearch.Size = new System.Drawing.Size(333, 27);
            this.tbDeepSearch.TabIndex = 1;
            // 
            // lblDeepSearch
            // 
            this.lblDeepSearch.AutoSize = true;
            this.lblDeepSearch.Location = new System.Drawing.Point(11, 28);
            this.lblDeepSearch.Name = "lblDeepSearch";
            this.lblDeepSearch.Size = new System.Drawing.Size(89, 16);
            this.lblDeepSearch.TabIndex = 0;
            this.lblDeepSearch.Text = "Search value:";
            // 
            // gbRunFilters
            // 
            this.gbRunFilters.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.gbRunFilters.Controls.Add(this.btnFetchHistory);
            this.gbRunFilters.Controls.Add(this.cmbStatus);
            this.gbRunFilters.Controls.Add(this.lblStatus);
            this.gbRunFilters.Controls.Add(this.dtpDateTo);
            this.gbRunFilters.Controls.Add(this.lblDateTo);
            this.gbRunFilters.Controls.Add(this.dtpDateFrom);
            this.gbRunFilters.Controls.Add(this.lblDateFrom);
            this.gbRunFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbRunFilters.Location = new System.Drawing.Point(0, 0);
            this.gbRunFilters.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbRunFilters.Name = "gbRunFilters";
            this.gbRunFilters.Padding = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.gbRunFilters.Size = new System.Drawing.Size(978, 57);
            this.gbRunFilters.TabIndex = 0;
            this.gbRunFilters.TabStop = false;
            this.gbRunFilters.Text = "Run Filters";
            // 
            // btnFetchHistory
            // 
            this.btnFetchHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnFetchHistory.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnFetchHistory.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnFetchHistory.Font = new System.Drawing.Font("Microsoft Tai Le", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFetchHistory.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.btnFetchHistory.Location = new System.Drawing.Point(822, 21);
            this.btnFetchHistory.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnFetchHistory.Name = "btnFetchHistory";
            this.btnFetchHistory.Size = new System.Drawing.Size(149, 30);
            this.btnFetchHistory.TabIndex = 6;
            this.btnFetchHistory.Text = "Get Runs";
            this.btnFetchHistory.UseVisualStyleBackColor = false;
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
            this.cmbStatus.Location = new System.Drawing.Point(571, 22);
            this.cmbStatus.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmbStatus.Name = "cmbStatus";
            this.cmbStatus.Size = new System.Drawing.Size(150, 24);
            this.cmbStatus.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(511, 26);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(54, 18);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Status:";
            // 
            // dtpDateTo
            // 
            this.dtpDateTo.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpDateTo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDateTo.Location = new System.Drawing.Point(295, 19);
            this.dtpDateTo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dtpDateTo.Name = "dtpDateTo";
            this.dtpDateTo.Size = new System.Drawing.Size(185, 27);
            this.dtpDateTo.TabIndex = 3;
            // 
            // lblDateTo
            // 
            this.lblDateTo.AutoSize = true;
            this.lblDateTo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateTo.Location = new System.Drawing.Point(259, 25);
            this.lblDateTo.Name = "lblDateTo";
            this.lblDateTo.Size = new System.Drawing.Size(30, 18);
            this.lblDateTo.TabIndex = 2;
            this.lblDateTo.Text = "To:";
            // 
            // dtpDateFrom
            // 
            this.dtpDateFrom.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpDateFrom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDateFrom.Location = new System.Drawing.Point(63, 20);
            this.dtpDateFrom.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dtpDateFrom.Name = "dtpDateFrom";
            this.dtpDateFrom.Size = new System.Drawing.Size(177, 27);
            this.dtpDateFrom.TabIndex = 1;
            // 
            // lblDateFrom
            // 
            this.lblDateFrom.AutoSize = true;
            this.lblDateFrom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateFrom.Location = new System.Drawing.Point(10, 24);
            this.lblDateFrom.Name = "lblDateFrom";
            this.lblDateFrom.Size = new System.Drawing.Size(46, 20);
            this.lblDateFrom.TabIndex = 0;
            this.lblDateFrom.Text = "From:";
            // 
            // MyPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.tsmContainer);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MyPluginControl";
            this.Size = new System.Drawing.Size(1347, 695);
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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.gbFlowRuns.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tsPagination.ResumeLayout(false);
            this.tsPagination.PerformLayout();
            this.gbDeepSearch.ResumeLayout(false);
            this.gbDeepSearch.PerformLayout();
            this.gbRunFilters.ResumeLayout(false);
            this.gbRunFilters.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip tsmContainer;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbDarkMode;
        private System.Windows.Forms.ToolStripButton tsbAiAssistant;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.GroupBox gbFlows;
        private System.Windows.Forms.CheckedListBox clbFlows;
        private System.Windows.Forms.CheckBox cbSelectAllFlows;
        private System.Windows.Forms.GroupBox gbFlowFilters;
        private System.Windows.Forms.TextBox tbSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.CheckBox cbxFlowStatusDraft;
        private System.Windows.Forms.CheckBox cbxFlowStatusActivated;
        private System.Windows.Forms.GroupBox gbFlowRuns;
        private System.Windows.Forms.GroupBox gbRunFilters;
        private System.Windows.Forms.Button btnFetchHistory;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DateTimePicker dtpDateTo;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.DateTimePicker dtpDateFrom;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.ToolStripButton btnExport;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ToolStrip tsPagination;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunId;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn EndDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn Duration;
        private System.Windows.Forms.DataGridViewTextBoxColumn Action;
        private System.Windows.Forms.DataGridViewTextBoxColumn Details;
        private System.Windows.Forms.ToolStripButton tsbSkipPrevious;
        private System.Windows.Forms.ToolStripButton tsbPrevious;
        private System.Windows.Forms.ToolStripButton tsbNext;
        private System.Windows.Forms.ToolStripButton tsbSkipNext;
        private System.Windows.Forms.ToolStripLabel tslPageNumber;
        private System.Windows.Forms.ToolStripTextBox tstbPageNumber;
        private System.Windows.Forms.ToolStripComboBox tscNumberOfRuns;
        private System.Windows.Forms.ToolStripLabel tslItems;
        private System.Windows.Forms.ToolStripLabel tslTotalItems;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn FlowRunError;
        private System.Windows.Forms.GroupBox gbDeepSearch;
        private System.Windows.Forms.Label lblDeepSearch;
        private System.Windows.Forms.TextBox tbDeepSearch;
        private System.Windows.Forms.Button btnDeepSearch;
        private System.Windows.Forms.Button btnClearDeepSearch;
        private System.Windows.Forms.ProgressBar progressBarDeepSearch;
        private System.Windows.Forms.Label lblDeepSearchStatus;
        private System.Windows.Forms.ComboBox cbSolutions;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnResubmit;
        private System.Windows.Forms.Button btnCheckSafety;
        private WeifenLuo.WinFormsUI.Docking.VS2015BlueTheme vS2015BlueTheme1;
    }
}
