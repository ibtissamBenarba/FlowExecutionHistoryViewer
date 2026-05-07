using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ExecutionFlowHistoryViewer.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class RunDetailForm : Form
    {
        private readonly FlowRun _run;
        private readonly FlowRunDetailDto _detail;
        private readonly FlowActionsResponseDto _actions;
        private readonly IFlowClient _flowClient;

        // Table controls for each tab
        private DataGridView _dgvGeneral;
        private DataGridView _dgvTrigger;
        private DataGridView _dgvActions;
        private DataGridView _dgvErrors;

        // Styling constants
        private static readonly Color HeaderBackColor = Color.FromArgb(45, 55, 72);
        private static readonly Color HeaderForeColor = Color.White;
        private static readonly Color AlternatingRowColor = Color.FromArgb(237, 242, 247);
        private static readonly Color GridLineColor = Color.FromArgb(203, 213, 224);
        private static readonly Color SucceededColor = Color.FromArgb(56, 161, 105);
        private static readonly Color FailedColor = Color.FromArgb(229, 62, 62);
        private static readonly Color SkippedColor = Color.FromArgb(160, 174, 192);
        private static readonly Color RunningColor = Color.FromArgb(49, 130, 206);
        private static readonly Color CancelledColor = Color.FromArgb(214, 158, 46);

        public RunDetailForm(FlowRun run, FlowRunDetailDto detail, FlowActionsResponseDto actions, IFlowClient flowClient)
        {
            _run = run;
            _detail = detail;
            _actions = actions;
            _flowClient = flowClient;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            BuildUi();
            LoadData();
        }

        #region UI Construction

        private void BuildUi()
        {
            this.Text = $"Run Details — {_run.FlowName}";
            this.Size = new Size(1000, 720);
            this.MinimumSize = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(247, 250, 252);

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(12, 6)
            };

            // Tab: General
            var tabGeneral = new TabPage("  General  ") { BackColor = Color.White };
            _dgvGeneral = CreateStyledGrid();
            tabGeneral.Controls.Add(_dgvGeneral);

            // Tab: Trigger
            var tabTrigger = new TabPage("  Trigger  ") { BackColor = Color.White };
            _dgvTrigger = CreateStyledGrid();
            tabTrigger.Controls.Add(_dgvTrigger);

            // Tab: Actions
            var tabActions = new TabPage("  Actions  ") { BackColor = Color.White };
            _dgvActions = CreateStyledGrid();
            tabActions.Controls.Add(_dgvActions);

            // Tab: Errors
            var tabError = new TabPage("  Errors  ") { BackColor = Color.White };
            _dgvErrors = CreateStyledGrid();
            tabError.Controls.Add(_dgvErrors);

            tabControl.TabPages.Add(tabGeneral);
            tabControl.TabPages.Add(tabTrigger);
            tabControl.TabPages.Add(tabActions);
            tabControl.TabPages.Add(tabError);

            this.Controls.Add(tabControl);
        }

        private DataGridView CreateStyledGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = GridLineColor,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9.5F),

                // Row styling
                RowTemplate = { Height = 34 },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AlternatingRowColor
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(45, 55, 72),
                    SelectionBackColor = Color.FromArgb(190, 215, 240),
                    SelectionForeColor = Color.FromArgb(45, 55, 72),
                    Padding = new Padding(6, 4, 6, 4),
                    WrapMode = DataGridViewTriState.True
                },

                // Header styling
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = HeaderBackColor,
                    ForeColor = HeaderForeColor,
                    Font = new Font("Segoe UI Semibold", 10F),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 6, 8, 6)
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40
            };

            dgv.CellFormatting += Dgv_CellFormatting;
            return dgv;
        }

        #endregion

        #region Data Loading

        private void LoadData()
        {
            LoadGeneralTab();
            LoadTriggerTab();
            LoadActionsTab();
            LoadErrorsTab();
        }

        private void LoadGeneralTab()
        {
            _dgvGeneral.Columns.Clear();
            _dgvGeneral.Columns.Add("Property", "Property");
            _dgvGeneral.Columns.Add("Value", "Value");
            _dgvGeneral.Columns["Property"].FillWeight = 30;
            _dgvGeneral.Columns["Value"].FillWeight = 70;

            // Make the Property column bold
            _dgvGeneral.Columns["Property"].DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = Color.FromArgb(45, 55, 72)
            };

            AddPropertyRow(_dgvGeneral, "Run ID", _run.Id);
            AddPropertyRow(_dgvGeneral, "Flow Name", _run.FlowName);
            AddPropertyRow(_dgvGeneral, "Status", _detail?.Properties?.Status ?? _run.Status);
            AddPropertyRow(_dgvGeneral, "Start Time", FormatDateTime(_detail?.Properties?.StartTime ?? _run.StartDate));
            AddPropertyRow(_dgvGeneral, "End Time", FormatDateTime(_detail?.Properties?.EndTime ?? _run.EndDate));
            AddPropertyRow(_dgvGeneral, "Duration", _run.Duration);
            AddPropertyRow(_dgvGeneral, "Tracking ID", _detail?.Properties?.CorrelationClientTrackingId ?? "N/A");
            AddPropertyRow(_dgvGeneral, "Run URL", _run.Url ?? "N/A");
        }

        private void LoadTriggerTab()
        {
            _dgvTrigger.Columns.Clear();
            _dgvTrigger.Columns.Add("Property", "Property");
            _dgvTrigger.Columns.Add("Value", "Value");
            _dgvTrigger.Columns["Property"].FillWeight = 25;
            _dgvTrigger.Columns["Value"].FillWeight = 75;

            _dgvTrigger.Columns["Property"].DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = Color.FromArgb(45, 55, 72)
            };

            if (_detail?.Properties?.Trigger == null)
            {
                AddPropertyRow(_dgvTrigger, "Info", "No trigger data available.");
                return;
            }

            var trigger = _detail.Properties.Trigger;
            AddPropertyRow(_dgvTrigger, "Trigger Name", trigger.Name ?? "N/A");

            // Inputs
            string inputs = null;
            if (trigger.InputsLink?.Uri != null)
            {
                inputs = GetTriggerContent(trigger.InputsLink.Uri);
            }
            else
            {
                inputs = FormatJson(trigger.Inputs);
            }

            // Try to parse inputs as JSON and flatten into rows
            AddJsonPropertyRows(_dgvTrigger, "Input", inputs);

            // Outputs
            string outputs = null;
            if (trigger.OutputsLink?.Uri != null)
            {
                outputs = GetTriggerContent(trigger.OutputsLink.Uri);
            }
            else
            {
                outputs = FormatJson(trigger.Outputs);
            }

            AddJsonPropertyRows(_dgvTrigger, "Output", outputs);
        }

        private void LoadActionsTab()
        {
            _dgvActions.Columns.Clear();
            _dgvActions.Columns.Add("ActionName", "Action Name");
            _dgvActions.Columns.Add("Type", "Type");
            _dgvActions.Columns.Add("Status", "Status");
            _dgvActions.Columns.Add("StartTime", "Start Time");
            _dgvActions.Columns.Add("EndTime", "End Time");
            _dgvActions.Columns.Add("Duration", "Duration");
            _dgvActions.Columns.Add("Error", "Error");

            _dgvActions.Columns["ActionName"].FillWeight = 22;
            _dgvActions.Columns["Type"].FillWeight = 14;
            _dgvActions.Columns["Status"].FillWeight = 10;
            _dgvActions.Columns["StartTime"].FillWeight = 15;
            _dgvActions.Columns["EndTime"].FillWeight = 15;
            _dgvActions.Columns["Duration"].FillWeight = 10;
            _dgvActions.Columns["Error"].FillWeight = 14;

            if (_actions?.Value == null || _actions.Value.Count == 0)
            {
                _dgvActions.Rows.Add("No actions found", "", "", "", "", "", "");
                return;
            }

            foreach (var action in _actions.Value)
            {
                string duration = "";
                if (action.Properties?.StartTime.HasValue == true && action.Properties?.EndTime.HasValue == true)
                {
                    var span = action.Properties.EndTime.Value - action.Properties.StartTime.Value;
                    duration = span.TotalSeconds < 1
                        ? $"{span.TotalMilliseconds:F0} ms"
                        : span.ToString(@"hh\:mm\:ss\.fff");
                }

                string error = action.Properties?.Error != null
                    ? $"{action.Properties.Error.Code}: {action.Properties.Error.Message}"
                    : "";

                int rowIdx = _dgvActions.Rows.Add(
                    action.Name ?? "N/A",
                    action.Type ?? "N/A",
                    action.Properties?.Status ?? "N/A",
                    FormatDateTime(action.Properties?.StartTime),
                    FormatDateTime(action.Properties?.EndTime),
                    duration,
                    error
                );

                // Color-code the status cell
                var statusCell = _dgvActions.Rows[rowIdx].Cells["Status"];
                statusCell.Style.ForeColor = GetStatusColor(action.Properties?.Status);
                statusCell.Style.Font = new Font("Segoe UI Semibold", 9.5F);

                // Highlight error cell if present
                if (!string.IsNullOrEmpty(error))
                {
                    _dgvActions.Rows[rowIdx].Cells["Error"].Style.ForeColor = FailedColor;
                }
            }
        }

        private void LoadErrorsTab()
        {
            _dgvErrors.Columns.Clear();
            _dgvErrors.Columns.Add("ActionName", "Action Name");
            _dgvErrors.Columns.Add("ErrorCode", "Error Code");
            _dgvErrors.Columns.Add("ErrorMessage", "Error Message");

            _dgvErrors.Columns["ActionName"].FillWeight = 25;
            _dgvErrors.Columns["ErrorCode"].FillWeight = 20;
            _dgvErrors.Columns["ErrorMessage"].FillWeight = 55;

            if (_actions?.Value == null)
            {
                _dgvErrors.Rows.Add("No data available", "", "");
                return;
            }

            var failedActions = _actions.Value
                .Where(a => a.Properties?.Error != null)
                .ToList();

            if (failedActions.Count == 0)
            {
                _dgvErrors.Rows.Add("✓ No errors found", "", "");
                _dgvErrors.Rows[0].Cells[0].Style.ForeColor = SucceededColor;
                _dgvErrors.Rows[0].Cells[0].Style.Font = new Font("Segoe UI Semibold", 10F);
                return;
            }

            foreach (var action in failedActions)
            {
                int rowIdx = _dgvErrors.Rows.Add(
                    action.Name,
                    action.Properties.Error.Code,
                    action.Properties.Error.Message
                );

                _dgvErrors.Rows[rowIdx].Cells["ErrorCode"].Style.ForeColor = FailedColor;
                _dgvErrors.Rows[rowIdx].Cells["ErrorMessage"].Style.ForeColor = FailedColor;
            }
        }

        #endregion

        #region Helpers

        private void AddPropertyRow(DataGridView dgv, string property, string value)
        {
            dgv.Rows.Add(property, value ?? "N/A");
        }

        /// <summary>
        /// Attempts to parse a JSON string and add its top-level keys as individual rows.
        /// Falls back to a single row if parsing fails.
        /// </summary>
        private void AddJsonPropertyRows(DataGridView dgv, string prefix, string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "null")
            {
                AddPropertyRow(dgv, prefix, "null");
                return;
            }

            try
            {
                var token = JToken.Parse(jsonString);
                if (token is JObject obj)
                {
                    foreach (var prop in obj.Properties())
                    {
                        string displayValue = prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array
                            ? prop.Value.ToString(Formatting.Indented)
                            : prop.Value.ToString();
                        AddPropertyRow(dgv, $"{prefix} › {prop.Name}", displayValue);
                    }
                }
                else
                {
                    // Array or primitive — show as single row
                    AddPropertyRow(dgv, prefix, token.ToString(Formatting.Indented));
                }
            }
            catch
            {
                // Not valid JSON — just show the raw string
                AddPropertyRow(dgv, prefix, jsonString);
            }
        }

        private string FormatDateTime(DateTime? dt)
        {
            if (!dt.HasValue) return "N/A";
            return dt.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string FormatDateTime(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string GetTriggerContent(string uri)
        {
            try
            {
                // Tu dois passer le IFlowClient ici — voir modification du constructeur ci-dessous
                return _flowClient.GetContentFromLink(uri);
            }
            catch (Exception ex)
            {
                return $"Error loading content: {ex.Message}";
            }
        }

        private string FormatJson(object obj)
        {
            if (obj == null) return "null";
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return obj.ToString();
            }
        }

        private Color GetStatusColor(string status)
        {
            if (string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
                return SucceededColor;
            if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
                return FailedColor;
            if (string.Equals(status, "Skipped", StringComparison.OrdinalIgnoreCase))
                return SkippedColor;
            if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
                return RunningColor;
            if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return CancelledColor;
            return Color.FromArgb(45, 55, 72);
        }

        /// <summary>
        /// Applies cell formatting dynamically — bold property columns, wrap long text, etc.
        /// </summary>
        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Auto-resize row height for cells with long content
            var dgv = sender as DataGridView;
            if (dgv == null || e.RowIndex < 0) return;

            var cell = dgv[e.ColumnIndex, e.RowIndex];
            if (cell.Value != null && cell.Value.ToString().Length > 100)
            {
                dgv.Rows[e.RowIndex].Height = Math.Min(
                    Math.Max(dgv.Rows[e.RowIndex].Height, 60),
                    200
                );
            }
        }

        #endregion
    }
}