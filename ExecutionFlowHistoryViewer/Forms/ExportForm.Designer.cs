namespace ExecutionFlowHistoryViewer.Forms
{
    partial class ExportForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportForm));
            this.gbExportFormat = new System.Windows.Forms.GroupBox();
            this.rbExcel = new System.Windows.Forms.RadioButton();
            this.rbCsv = new System.Windows.Forms.RadioButton();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.gbExportRange = new System.Windows.Forms.GroupBox();
            this.rbAllPages = new System.Windows.Forms.RadioButton();
            this.rbCurrentPage = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cmbEncoding = new System.Windows.Forms.ComboBox();
            this.lbEncoding = new System.Windows.Forms.Label();
            this.lbColmDelim = new System.Windows.Forms.Label();
            this.cmbDelimiter = new System.Windows.Forms.ComboBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbIncludeHeaders = new System.Windows.Forms.CheckBox();
            this.gbExportFormat.SuspendLayout();
            this.gbExportRange.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbExportFormat
            // 
            this.gbExportFormat.Controls.Add(this.rbExcel);
            this.gbExportFormat.Controls.Add(this.rbCsv);
            this.gbExportFormat.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbExportFormat.ForeColor = System.Drawing.Color.Navy;
            this.gbExportFormat.Location = new System.Drawing.Point(12, 12);
            this.gbExportFormat.Name = "gbExportFormat";
            this.gbExportFormat.Size = new System.Drawing.Size(558, 75);
            this.gbExportFormat.TabIndex = 0;
            this.gbExportFormat.TabStop = false;
            this.gbExportFormat.Text = "Export Format";
            // 
            // rbExcel
            // 
            this.rbExcel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbExcel.ForeColor = System.Drawing.Color.Black;
            this.rbExcel.Location = new System.Drawing.Point(232, 21);
            this.rbExcel.Name = "rbExcel";
            this.rbExcel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.rbExcel.Size = new System.Drawing.Size(145, 46);
            this.rbExcel.TabIndex = 1;
            this.rbExcel.TabStop = true;
            this.rbExcel.Text = "Excel";
            this.rbExcel.UseCompatibleTextRendering = true;
            this.rbExcel.UseVisualStyleBackColor = true;
            // 
            // rbCsv
            // 
            this.rbCsv.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbCsv.ForeColor = System.Drawing.Color.Black;
            this.rbCsv.Location = new System.Drawing.Point(24, 21);
            this.rbCsv.Name = "rbCsv";
            this.rbCsv.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.rbCsv.Size = new System.Drawing.Size(145, 46);
            this.rbCsv.TabIndex = 0;
            this.rbCsv.TabStop = true;
            this.rbCsv.Text = "CSV";
            this.rbCsv.UseCompatibleTextRendering = true;
            this.rbCsv.UseVisualStyleBackColor = true;
            this.rbCsv.CheckedChanged += new System.EventHandler(this.rbCsv_CheckedChanged);
            // 
            // gbExportRange
            // 
            this.gbExportRange.Controls.Add(this.rbAllPages);
            this.gbExportRange.Controls.Add(this.rbCurrentPage);
            this.gbExportRange.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbExportRange.ForeColor = System.Drawing.Color.Navy;
            this.gbExportRange.Location = new System.Drawing.Point(12, 182);
            this.gbExportRange.Name = "gbExportRange";
            this.gbExportRange.Size = new System.Drawing.Size(558, 82);
            this.gbExportRange.TabIndex = 1;
            this.gbExportRange.TabStop = false;
            this.gbExportRange.Text = "Export Range";
            // 
            // rbAllPages
            // 
            this.rbAllPages.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbAllPages.ForeColor = System.Drawing.Color.Black;
            this.rbAllPages.Location = new System.Drawing.Point(232, 21);
            this.rbAllPages.Name = "rbAllPages";
            this.rbAllPages.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.rbAllPages.Size = new System.Drawing.Size(145, 46);
            this.rbAllPages.TabIndex = 2;
            this.rbAllPages.TabStop = true;
            this.rbAllPages.Text = "All Pages";
            this.rbAllPages.UseCompatibleTextRendering = true;
            this.rbAllPages.UseVisualStyleBackColor = true;
            // 
            // rbCurrentPage
            // 
            this.rbCurrentPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbCurrentPage.ForeColor = System.Drawing.Color.Black;
            this.rbCurrentPage.Location = new System.Drawing.Point(24, 21);
            this.rbCurrentPage.Name = "rbCurrentPage";
            this.rbCurrentPage.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.rbCurrentPage.Size = new System.Drawing.Size(145, 46);
            this.rbCurrentPage.TabIndex = 1;
            this.rbCurrentPage.TabStop = true;
            this.rbCurrentPage.Text = "Current Page";
            this.rbCurrentPage.UseCompatibleTextRendering = true;
            this.rbCurrentPage.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cmbEncoding);
            this.groupBox1.Controls.Add(this.lbEncoding);
            this.groupBox1.Controls.Add(this.lbColmDelim);
            this.groupBox1.Controls.Add(this.cmbDelimiter);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.ForeColor = System.Drawing.Color.Navy;
            this.groupBox1.Location = new System.Drawing.Point(12, 93);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(558, 83);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "CSV Format Settings";
            // 
            // cmbEncoding
            // 
            this.cmbEncoding.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbEncoding.FormattingEnabled = true;
            this.cmbEncoding.Location = new System.Drawing.Point(368, 39);
            this.cmbEncoding.Name = "cmbEncoding";
            this.cmbEncoding.Size = new System.Drawing.Size(85, 28);
            this.cmbEncoding.TabIndex = 8;
            this.cmbEncoding.Text = "UTF-8";
            // 
            // lbEncoding
            // 
            this.lbEncoding.AutoSize = true;
            this.lbEncoding.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbEncoding.ForeColor = System.Drawing.Color.Black;
            this.lbEncoding.Location = new System.Drawing.Point(279, 42);
            this.lbEncoding.Name = "lbEncoding";
            this.lbEncoding.Size = new System.Drawing.Size(83, 20);
            this.lbEncoding.TabIndex = 7;
            this.lbEncoding.Text = "Encoding:";
            // 
            // lbColmDelim
            // 
            this.lbColmDelim.AutoSize = true;
            this.lbColmDelim.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbColmDelim.ForeColor = System.Drawing.Color.Black;
            this.lbColmDelim.Location = new System.Drawing.Point(20, 42);
            this.lbColmDelim.Name = "lbColmDelim";
            this.lbColmDelim.Size = new System.Drawing.Size(144, 20);
            this.lbColmDelim.TabIndex = 6;
            this.lbColmDelim.Text = "Column Delimiter:";
            // 
            // cmbDelimiter
            // 
            this.cmbDelimiter.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbDelimiter.FormattingEnabled = true;
            this.cmbDelimiter.Location = new System.Drawing.Point(170, 39);
            this.cmbDelimiter.Name = "cmbDelimiter";
            this.cmbDelimiter.Size = new System.Drawing.Size(64, 30);
            this.cmbDelimiter.TabIndex = 5;
            this.cmbDelimiter.Text = ";";
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnExport.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExport.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.btnExport.Location = new System.Drawing.Point(295, 308);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(156, 33);
            this.btnExport.TabIndex = 3;
            this.btnExport.Text = "Start Export ";
            this.btnExport.UseVisualStyleBackColor = false;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.btnCancel.Location = new System.Drawing.Point(476, 308);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(94, 33);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cbIncludeHeaders
            // 
            this.cbIncludeHeaders.AutoSize = true;
            this.cbIncludeHeaders.Checked = true;
            this.cbIncludeHeaders.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbIncludeHeaders.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbIncludeHeaders.Location = new System.Drawing.Point(26, 270);
            this.cbIncludeHeaders.Name = "cbIncludeHeaders";
            this.cbIncludeHeaders.Size = new System.Drawing.Size(295, 24);
            this.cbIncludeHeaders.TabIndex = 0;
            this.cbIncludeHeaders.Text = "Include Headers in the exported file";
            this.cbIncludeHeaders.UseVisualStyleBackColor = true;
            // 
            // ExportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(582, 353);
            this.Controls.Add(this.cbIncludeHeaders);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.gbExportRange);
            this.Controls.Add(this.gbExportFormat);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ExportForm";
            this.Text = "Export Flow Run History";
            this.Load += new System.EventHandler(this.ExportForm_Load);
            this.gbExportFormat.ResumeLayout(false);
            this.gbExportRange.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbExportFormat;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.GroupBox gbExportRange;
        private System.Windows.Forms.RadioButton rbCsv;
        private System.Windows.Forms.RadioButton rbExcel;
        private System.Windows.Forms.RadioButton rbAllPages;
        private System.Windows.Forms.RadioButton rbCurrentPage;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbIncludeHeaders;
        private System.Windows.Forms.Label lbColmDelim;
        private System.Windows.Forms.ComboBox cmbDelimiter;
        private System.Windows.Forms.ComboBox cmbEncoding;
        private System.Windows.Forms.Label lbEncoding;
    }
}