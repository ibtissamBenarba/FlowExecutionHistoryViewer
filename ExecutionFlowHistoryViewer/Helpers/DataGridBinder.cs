using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public static class DataGridBinder
    {
        public static void BindFlowRuns(DataGridView grid, List<FlowRun> runs, List<CustomTriggerColumnSetting> customCols = null)
        {
            grid.AutoGenerateColumns = false;
            grid.DataSource = null;
            grid.Columns.Clear();

            if (runs == null || runs.Count == 0)
            {
                grid.DataSource = new List<FlowRun>();
                return;
            }

            grid.DataSource = runs;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.FlowName),
                HeaderText = "Flow Name",
                Name = "FlowName",
                Width = 200
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Id),
                HeaderText = "Run ID",
                Name = "Id",
                Width = 250
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Status),
                HeaderText = "Status",
                Name = "Status",
                Width = 80
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.StartDate),
                HeaderText = "Start Time",
                Name = "StartDate",
                Width = 130
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.EndDate),
                HeaderText = "End Time",
                Name = "EndDate",
                Width = 130
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Duration),
                HeaderText = "Duration",
                Name = "Duration",
                Width = 80
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.TriggerName),
                HeaderText = "Trigger",
                Name = "TriggerName",
                Width = 120
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.TriggerStatus),
                HeaderText = "Trigger Status",
                Name = "TriggerStatus",
                Width = 100
            });

            if (customCols != null)
            {
                foreach (var cc in customCols)
                {
                    grid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        HeaderText = cc.HeaderText,
                        Name = "col_custom_trigger_" + cc.JsonPath,
                        Width = 120,
                        ReadOnly = true
                    });
                }
            }

            grid.Columns.Add(new DataGridViewLinkColumn
            {
                HeaderText = "Action",
                Text = "View Run",
                UseColumnTextForLinkValue = true,
                Name = "ViewRun",
                Width = 80
            });

            var detailsColumn = new DataGridViewLinkColumn
            {
                HeaderText = "Details",
                Text = "View Details",
                UseColumnTextForLinkValue = true,
                Name = "ViewDetails",
                Width = 80
            };
            grid.Columns.Add(detailsColumn);
        }
    }
}
