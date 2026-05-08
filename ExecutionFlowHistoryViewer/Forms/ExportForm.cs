using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class ExportForm : Form
    {
        private readonly List<FlowRun> _currentPageRuns;
        private readonly List<FlowRun> _allRuns;

        public ExportForm(List<FlowRun> currentPageRuns, List<FlowRun> allRuns)
        {
            InitializeComponent();
            _currentPageRuns = currentPageRuns ?? new List<FlowRun>();
            _allRuns = allRuns ?? new List<FlowRun>();
        }

        private void ExportForm_Load(object sender, EventArgs e)
        {
            // Format par défaut
            rbExcel.Checked = true;

            // Plage par défaut
            rbCurrentPage.Checked = true;

            // Headers coché par défaut
            cbIncludeHeaders.Checked = true;

            // --- Delimiter ---
            cmbDelimiter.Items.Clear();
            cmbDelimiter.Items.AddRange(new object[] { ";", ",", "Tab", "|" });
            cmbDelimiter.SelectedIndex = 0;
            cmbDelimiter.DropDownStyle = ComboBoxStyle.DropDownList;

            // --- Encoding ---
            cmbEncoding.Items.Clear();
            cmbEncoding.Items.AddRange(new object[] { "UTF-8", "UTF-16", "ASCII" });
            cmbEncoding.SelectedIndex = 0;
            cmbEncoding.DropDownStyle = ComboBoxStyle.DropDownList;

            // Activer/désactiver les options CSV selon la sélection
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
            List<FlowRun> runsToExport = rbCurrentPage.Checked ? _currentPageRuns : _allRuns;

            if (runsToExport.Count == 0)
            {
                MessageBox.Show("There is no data to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string filter = rbCsv.Checked
                ? "CSV files (*.csv)|*.csv"
                : "Excel files (*.xlsx)|*.xlsx";

            using (var sfd = new SaveFileDialog { Filter = filter })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    if (rbCsv.Checked)
                    {
                        string delimiter = GetSelectedDelimiter();
                        Encoding encoding = GetSelectedEncoding();
                        CsvService.Export(runsToExport, sfd.FileName, delimiter, encoding, cbIncludeHeaders.Checked);
                    }
                    else
                    {
                        ExcelService.Export(runsToExport, sfd.FileName, cbIncludeHeaders.Checked);
                    }

                    MessageBox.Show("Export completed successfully!", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export failed:\n\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private string GetSelectedDelimiter()
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

        private Encoding GetSelectedEncoding()
        {
            switch (cmbEncoding.SelectedIndex)
            {
                case 0: return Encoding.UTF8;
                case 1: return Encoding.Unicode; // UTF-16
                case 2: return Encoding.ASCII;
                default: return Encoding.UTF8;
            }
        }
    }
}
