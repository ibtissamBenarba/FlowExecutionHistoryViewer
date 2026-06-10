using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public static class DataGridBinder
    {
        public static void BindFlowRuns(DataGridView grid, List<FlowRun> runs, HashSet<string> checkedRunIds = null)
        {
            // 1. Clear existing columns and set auto-generation
            grid.Columns.Clear();
            grid.AutoGenerateColumns = true;

            // 2. Bind an empty list first to generate columns from the type
            grid.DataSource = new List<FlowRun>();

            // 3. Now customize the generated columns
            //    - Hide properties you don't want to display
            //    - Set headers, widths, etc.
            foreach (DataGridViewColumn col in grid.Columns)
            {
                switch (col.Name)
                {
                    case nameof(FlowRun.FlowName):
                        col.HeaderText = "Flow Name";
                        col.Width = 200;
                        break;
                    case nameof(FlowRun.Id):
                        col.HeaderText = "Run ID";
                        col.Width = 250;
                        break;
                    case nameof(FlowRun.Status):
                        col.HeaderText = "Status";
                        col.Width = 80;
                        break;
                    case nameof(FlowRun.StartDate):
                        col.HeaderText = "Start Time";
                        col.Width = 130;
                        break;
                    case nameof(FlowRun.EndDate):
                        col.HeaderText = "End Time";
                        col.Width = 130;
                        break;
                    case nameof(FlowRun.Duration):
                        col.HeaderText = "Duration";
                        col.Width = 80;
                        break;
                    // Hide any property you don't want (e.g., FlowId, TriggerOutputs, Url)
                    case nameof(FlowRun.FlowId):
                    case nameof(FlowRun.TriggerOutputs):
                    case nameof(FlowRun.Url):
                        col.Visible = false;
                        break;
                }
                // Make all data columns read-only
                col.ReadOnly = true;
            }

            // 4. Insert custom checkbox column at index 0
            var selectColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "☐",
                Width = 24,
                MinimumWidth = 24,
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
            grid.Columns.Insert(0, selectColumn);

            // 5. Add link columns at the end
            var viewRunColumn = new DataGridViewLinkColumn
            {
                HeaderText = "Action",
                Text = "View Run",
                UseColumnTextForLinkValue = true,
                Name = "ViewRun",
                Width = 80,
                ReadOnly = true
            };
            grid.Columns.Add(viewRunColumn);

            var detailsColumn = new DataGridViewLinkColumn
            {
                HeaderText = "Details",
                Text = "View Details",
                UseColumnTextForLinkValue = true,
                Name = "ViewDetails",
                Width = 80,
                ReadOnly = true
            };
            grid.Columns.Add(detailsColumn);

            // 6. Turn off auto-generation for future binds (we now have our static set)
            grid.AutoGenerateColumns = false;

            // 7. Bind the actual data
            if (runs == null || runs.Count == 0)
            {
                grid.DataSource = new List<FlowRun>();
            }
            else
            {
                grid.DataSource = runs;
                if (checkedRunIds != null)
                    SyncCheckboxStates(grid, checkedRunIds);
            }
        }

        /// <summary>
        /// Syncs checkbox column values with the checkedRunIds set.
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