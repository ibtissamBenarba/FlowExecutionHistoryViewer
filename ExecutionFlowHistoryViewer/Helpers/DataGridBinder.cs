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
        public static void BindFlowRuns(DataGridView grid, List<FlowRun> runs)
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
                Width = 200
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Id),
                HeaderText = "Run ID",
                Width = 250
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Status),
                HeaderText = "Status",
                Width = 80
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.StartDate),
                HeaderText = "Start Time",
                Width = 130
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.EndDate),
                HeaderText = "End Time",
                Width = 130
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FlowRun.Duration),
                HeaderText = "Duration",
                Width = 80
            });

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
