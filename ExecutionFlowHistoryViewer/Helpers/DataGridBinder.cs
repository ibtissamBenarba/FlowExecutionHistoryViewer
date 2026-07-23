using ExecutionFlowHistoryViewer.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public static class DataGridBinder
    {
        public static void BindFlowRuns(DataGridView grid, List<FlowRun> runs, HashSet<string> checkedRunIds = null)
        {
            grid.Columns.Clear();
            grid.AutoGenerateColumns = false;

            // Checkbox
            var selectColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "☐",
                Width = 30,
                MinimumWidth = 30,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                FalseValue = false,
                TrueValue = true,
                FlatStyle = FlatStyle.Standard,
                Resizable = DataGridViewTriState.False,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            selectColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            selectColumn.HeaderCell.Style.Font = new Font(grid.Font.FontFamily, 10, FontStyle.Regular);

            grid.Columns.Add(selectColumn);

            // Flow Name
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.FlowName),
                HeaderText = "Flow Name",
                Name = nameof(FlowRun.FlowName),
                Width = 200,
                ReadOnly = true
            });

            // Run ID
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Id),
                HeaderText = "Run ID",
                Name = nameof(FlowRun.Id),
                Width = 250,
                ReadOnly = true
            });

            // Status
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Status),
                HeaderText = "Status",
                Name = nameof(FlowRun.Status),
                Width = 90,
                ReadOnly = true
            });

            // Start Time
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.StartDate),
                HeaderText = "Start Time",
                Name = nameof(FlowRun.StartDate),
                Width = 140,
                ReadOnly = true
            });

            // End Time
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.EndDate),
                HeaderText = "End Time",
                Name = nameof(FlowRun.EndDate),
                Width = 140,
                ReadOnly = true
            });

            // Duration
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Duration),
                HeaderText = "Duration",
                Name = nameof(FlowRun.Duration),
                Width = 90,
                ReadOnly = true
            });

            // Trigger
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.TriggerName),
                HeaderText = "Trigger",
                Name = nameof(FlowRun.TriggerName),
                Width = 150,
                ReadOnly = true
            });

            // Trigger Status
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.TriggerStatus),
                HeaderText = "Trigger Status",
                Name = nameof(FlowRun.TriggerStatus),
                Width = 120,
                ReadOnly = true
            });

            // View Run
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                HeaderText = "Action",
                Text = "View Run",
                UseColumnTextForLinkValue = true,
                Name = "ViewRun",
                Width = 90,
                ReadOnly = true
            });

            // View Details
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                HeaderText = "Details",
                Text = "View Details",
                UseColumnTextForLinkValue = true,
                Name = "ViewDetails",
                Width = 100,
                ReadOnly = true
            });

            if (runs == null)
                runs = new List<FlowRun>();

            grid.DataSource = runs;

            if (checkedRunIds != null)
                SyncCheckboxStates(grid, checkedRunIds);
        }

        /// <summary>
        /// Synchronize checkbox values with selected Run IDs.
        /// </summary>
        public static void SyncCheckboxStates(DataGridView grid, HashSet<string> checkedRunIds)
        {
            if (grid.Columns["Select"] == null) return;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var run = row.DataBoundItem as FlowRun;
                if (run == null) continue;

                // Set cell value directly instead of modifying the bound property
                row.Cells["Select"].Value = checkedRunIds.Contains(run.Id);
            }
        }
    }
}