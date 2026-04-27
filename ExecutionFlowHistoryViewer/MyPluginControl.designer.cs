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
            this.toolStripMenu = new System.Windows.Forms.ToolStrip();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSample = new System.Windows.Forms.ToolStripButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnFetchHistory = new System.Windows.Forms.Button();
            this.cmbFlows = new System.Windows.Forms.ComboBox();
            this.btnLoadFlows = new System.Windows.Forms.Button();
            this.btnConnectPA = new System.Windows.Forms.Button();
            this.toolStripMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStripMenu
            // 
            this.toolStripMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbClose,
            this.tssSeparator1,
            this.tsbSample});
            this.toolStripMenu.Location = new System.Drawing.Point(0, 0);
            this.toolStripMenu.Name = "toolStripMenu";
            this.toolStripMenu.Size = new System.Drawing.Size(745, 27);
            this.toolStripMenu.TabIndex = 4;
            this.toolStripMenu.Text = "toolStrip1";
            // 
            // tsbClose
            // 
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(107, 24);
            this.tsbClose.Text = "Close this tool";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            // 
            // tssSeparator1
            // 
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // tsbSample
            // 
            this.tsbSample.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSample.Name = "tsbSample";
            this.tsbSample.Size = new System.Drawing.Size(57, 24);
            this.tsbSample.Text = "Try me";
            this.tsbSample.Click += new System.EventHandler(this.tsbSample_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(112, 34);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(633, 314);
            this.dataGridView1.TabIndex = 5;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            // 
            // btnFetchHistory
            // 
            this.btnFetchHistory.Location = new System.Drawing.Point(4, 214);
            this.btnFetchHistory.Margin = new System.Windows.Forms.Padding(4);
            this.btnFetchHistory.Name = "btnFetchHistory";
            this.btnFetchHistory.Size = new System.Drawing.Size(100, 28);
            this.btnFetchHistory.TabIndex = 6;
            this.btnFetchHistory.Text = "FetchHistory";
            this.btnFetchHistory.UseVisualStyleBackColor = true;
            this.btnFetchHistory.Click += new System.EventHandler(this.btnFetchHistory_Click);
            // 
            // cmbFlows
            // 
            this.cmbFlows.FormattingEnabled = true;
            this.cmbFlows.Location = new System.Drawing.Point(4, 51);
            this.cmbFlows.Margin = new System.Windows.Forms.Padding(4);
            this.cmbFlows.Name = "cmbFlows";
            this.cmbFlows.Size = new System.Drawing.Size(99, 24);
            this.cmbFlows.TabIndex = 7;
            // 
            // btnLoadFlows
            // 
            this.btnLoadFlows.Location = new System.Drawing.Point(4, 262);
            this.btnLoadFlows.Margin = new System.Windows.Forms.Padding(4);
            this.btnLoadFlows.Name = "btnLoadFlows";
            this.btnLoadFlows.Size = new System.Drawing.Size(100, 28);
            this.btnLoadFlows.TabIndex = 8;
            this.btnLoadFlows.Text = "FetchFlow";
            this.btnLoadFlows.UseVisualStyleBackColor = true;
            this.btnLoadFlows.Click += new System.EventHandler(this.btnLoadFlows_Click);
            // 
            // btnConnectPA
            // 
            this.btnConnectPA.Location = new System.Drawing.Point(0, 142);
            this.btnConnectPA.Name = "btnConnectPA";
            this.btnConnectPA.Size = new System.Drawing.Size(111, 29);
            this.btnConnectPA.TabIndex = 9;
            this.btnConnectPA.Text = "ConnectPA";
            this.btnConnectPA.UseVisualStyleBackColor = true;
            this.btnConnectPA.Click += new System.EventHandler(this.btnConnectPA_Click);
            // 
            // MyPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnConnectPA);
            this.Controls.Add(this.btnLoadFlows);
            this.Controls.Add(this.cmbFlows);
            this.Controls.Add(this.btnFetchHistory);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStripMenu);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MyPluginControl";
            this.Size = new System.Drawing.Size(745, 369);
            this.Load += new System.EventHandler(this.MyPluginControl_Load);
            this.toolStripMenu.ResumeLayout(false);
            this.toolStripMenu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStripMenu;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripButton tsbSample;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnFetchHistory;
        private System.Windows.Forms.ComboBox cmbFlows;
        private System.Windows.Forms.Button btnLoadFlows;
        private System.Windows.Forms.Button btnConnectPA;
    }
}
