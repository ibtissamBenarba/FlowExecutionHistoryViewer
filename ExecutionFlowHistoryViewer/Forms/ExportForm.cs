using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class ExportForm : Form
    {
        public ExportForm()
        {
            InitializeComponent();
        }

        // Propriétés publiques pour l'appelant
        public bool IsCsv => rbCsv.Checked;
        public bool ExportAllPages => rbAllPages.Checked;
        public bool IncludeHeaders => cbIncludeHeaders.Checked;

        public string GetSelectedDelimiter()
        {
            switch (cmbDelimiter.SelectedIndex)
            {
                case 0: return ";";
                case 1: return ",";
                case 2: return "\t";
                case 3: return "|";
                default: return ";";
            }
        }

        public Encoding GetSelectedEncoding()
        {
            switch (cmbEncoding.SelectedIndex)
            {
                case 0: return Encoding.UTF8;
                case 1: return Encoding.Unicode; // UTF-16
                case 2: return Encoding.ASCII;
                default: return Encoding.UTF8;
            }
        }

        private void ExportForm_Load(object sender, EventArgs e)
        {
            rbExcel.Checked = true;
            rbCurrentPage.Checked = true;
            cbIncludeHeaders.Checked = true;

            cmbDelimiter.Items.Clear();
            cmbDelimiter.Items.AddRange(new object[] { ";", ",", "Tab", "|" });
            cmbDelimiter.SelectedIndex = 0;
            cmbDelimiter.DropDownStyle = ComboBoxStyle.DropDownList;

            cmbEncoding.Items.Clear();
            cmbEncoding.Items.AddRange(new object[] { "UTF-8", "UTF-16", "ASCII" });
            cmbEncoding.SelectedIndex = 0;
            cmbEncoding.DropDownStyle = ComboBoxStyle.DropDownList;

            UpdateCsvControlsState();
        }

        private void rbCsv_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCsvControlsState();
        }

        private void UpdateCsvControlsState()
        {
            bool isCsv = rbCsv.Checked;
            cmbDelimiter.Enabled = isCsv;
            cmbEncoding.Enabled = isCsv;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}