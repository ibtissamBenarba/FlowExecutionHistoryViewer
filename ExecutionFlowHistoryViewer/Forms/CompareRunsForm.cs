using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class CompareRunsForm : Form
    {
        private readonly FlowRun _run1;
        private readonly FlowRun _run2;
        private readonly FlowRunDetailDto _detail1;
        private readonly FlowRunDetailDto _detail2;
        private readonly FlowActionsResponseDto _actions1;
        private readonly FlowActionsResponseDto _actions2;
        private readonly IFlowClient _flowClient;

        // UI Controls
        private DataGridView _dgvGeneralTrigger;
        private DataGridView _dgvActions;
        private TextBox _tbInputs1;
        private TextBox _tbInputs2;
        private TextBox _tbOutputs1;
        private TextBox _tbOutputs2;

        private List<ActionComparisonItem> _comparisonItems = new List<ActionComparisonItem>();

        // Styling constants
        private static readonly Color HeaderBackColor = Color.FromArgb(45, 55, 72);
        private static readonly Color HeaderForeColor = Color.White;
        private static readonly Color AlternatingRowColor = Color.FromArgb(247, 250, 252);
        private static readonly Color GridLineColor = Color.FromArgb(226, 232, 240);
        private static readonly Color SucceededColor = Color.FromArgb(34, 197, 94); // Modern Green
        private static readonly Color FailedColor = Color.FromArgb(239, 68, 68); // Modern Red
        private static readonly Color SkippedColor = Color.FromArgb(107, 114, 128); // Modern Gray
        private static readonly Color RunningColor = Color.FromArgb(59, 130, 246); // Modern Blue
        private static readonly Color CancelledColor = Color.FromArgb(245, 158, 11); // Modern Orange
        private static readonly Color DiffHighlightColor = Color.FromArgb(254, 243, 199); // Soft Amber
        private static readonly Color DiffHighlightForeColor = Color.FromArgb(146, 64, 14); // Dark Amber

        public CompareRunsForm(
            FlowRun run1, FlowRunDetailDto detail1, FlowActionsResponseDto actions1,
            FlowRun run2, FlowRunDetailDto detail2, FlowActionsResponseDto actions2,
            IFlowClient flowClient)
        {
            _run1 = run1;
            _run2 = run2;
            _detail1 = detail1;
            _detail2 = detail2;
            _actions1 = actions1;
            _actions2 = actions2;
            _flowClient = flowClient;

            InitializeComponent();
            BuildUi();
            LoadData();
        }

        #region UI Construction

        private void BuildUi()
        {
            this.Text = $"Compare Flow Runs — {_run1.FlowName}";
            this.Size = new Size(1250, 820);
            this.MinimumSize = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(243, 244, 246);

            // Main layout panel
            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(243, 244, 246)
            };
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F)); // Summary panel
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Content area

            // 1. Summary Cards Panel
            var pnlSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            pnlSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var card1 = CreateRunSummaryCard("RUN 1 (LEFT)", _run1, _detail1);
            var card2 = CreateRunSummaryCard("RUN 2 (RIGHT)", _run2, _detail2);

            pnlSummary.Controls.Add(card1, 0, 0);
            pnlSummary.Controls.Add(card2, 1, 0);
            pnlMain.Controls.Add(pnlSummary, 0, 0);

            // 2. Tab Control for Content
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Point(12, 6)
            };

            // Tab 1: General & Trigger Info
            var tabGeneralTrigger = new TabPage("  🌐 General & Trigger Comparison  ") { BackColor = Color.White };
            _dgvGeneralTrigger = CreateStyledGrid();
            _dgvGeneralTrigger.CellFormatting += DgvGeneralTrigger_CellFormatting;
            tabGeneralTrigger.Controls.Add(_dgvGeneralTrigger);

            // Tab 2: Actions Comparison
            var tabActions = new TabPage("  ⚙️ Action Steps Comparison  ") { BackColor = Color.White };
            
            var splitActions = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 260
            };

            // Top: Actions list grid
            _dgvActions = CreateStyledGrid();
            _dgvActions.SelectionChanged += DgvActions_SelectionChanged;
            splitActions.Panel1.Controls.Add(_dgvActions);

            // Bottom: Synchronization Detail Diff panel
            var tabInputsOutputs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F)
            };

            // Subtab A: Inputs comparison
            var tabInputs = new TabPage("  📥 Action Inputs Comparison  ") { BackColor = Color.White };
            var layoutInputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
            layoutInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var gbInputs1 = new GroupBox { Text = "Run 1 Action Inputs", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F) };
            _tbInputs1 = CreateCodeTextBox();
            gbInputs1.Controls.Add(_tbInputs1);

            var gbInputs2 = new GroupBox { Text = "Run 2 Action Inputs", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F) };
            _tbInputs2 = CreateCodeTextBox();
            gbInputs2.Controls.Add(_tbInputs2);

            layoutInputs.Controls.Add(gbInputs1, 0, 0);
            layoutInputs.Controls.Add(gbInputs2, 1, 0);
            tabInputs.Controls.Add(layoutInputs);

            // Subtab B: Outputs comparison
            var tabOutputs = new TabPage("  📤 Action Outputs & Errors Comparison  ") { BackColor = Color.White };
            var layoutOutputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
            layoutOutputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutOutputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var gbOutputs1 = new GroupBox { Text = "Run 1 Action Outputs & Errors", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F) };
            _tbOutputs1 = CreateCodeTextBox();
            gbOutputs1.Controls.Add(_tbOutputs1);

            var gbOutputs2 = new GroupBox { Text = "Run 2 Action Outputs & Errors", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F) };
            _tbOutputs2 = CreateCodeTextBox();
            gbOutputs2.Controls.Add(_tbOutputs2);

            layoutOutputs.Controls.Add(gbOutputs1, 0, 0);
            layoutOutputs.Controls.Add(gbOutputs2, 1, 0);
            tabOutputs.Controls.Add(layoutOutputs);

            tabInputsOutputs.TabPages.Add(tabInputs);
            tabInputsOutputs.TabPages.Add(tabOutputs);
            splitActions.Panel2.Controls.Add(tabInputsOutputs);

            tabActions.Controls.Add(splitActions);

            tabControl.TabPages.Add(tabGeneralTrigger);
            tabControl.TabPages.Add(tabActions);
            pnlMain.Controls.Add(tabControl, 0, 1);

            this.Controls.Add(pnlMain);
        }

        private Panel CreateRunSummaryCard(string title, FlowRun run, FlowRunDetailDto detail)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                Margin = new Padding(6)
            };

            // Custom border / shading using raw panel background
            pnl.Paint += (s, e) =>
            {
                var rect = pnl.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using (var pen = new Pen(GridLineColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }

                // Draw status accent line on the left side
                var statusColor = GetStatusColor(detail?.Properties?.Status ?? run.Status);
                using (var brush = new SolidBrush(statusColor))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, 6, pnl.Height);
                }
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 8.5F),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(16, 10),
                AutoSize = true
            };

            string status = detail?.Properties?.Status ?? run.Status;
            var lblStatus = new Label
            {
                Text = status.ToUpper(),
                Font = new Font("Segoe UI Black", 14F),
                ForeColor = GetStatusColor(status),
                Location = new Point(16, 28),
                AutoSize = true
            };

            string details = $"Date: {FormatDateTime(detail?.Properties?.StartTime ?? run.StartDate)}  |  Duration: {run.Duration}";
            var lblDetails = new Label
            {
                Text = details,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(75, 85, 99),
                Location = new Point(16, 62),
                AutoSize = true
            };

            var lblRunId = new Label
            {
                Text = $"ID: {run.Id}",
                Font = new Font("Consolas", 7.5F),
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(16, 82),
                AutoSize = true
            };

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblStatus);
            pnl.Controls.Add(lblDetails);
            pnl.Controls.Add(lblRunId);

            return pnl;
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
                    ForeColor = Color.FromArgb(55, 65, 81),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(30, 58, 138),
                    Padding = new Padding(6, 4, 6, 4),
                    WrapMode = DataGridViewTriState.True
                },

                // Header styling
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = HeaderBackColor,
                    ForeColor = HeaderForeColor,
                    Font = new Font("Segoe UI Semibold", 9.5F),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 6, 8, 6)
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38
            };

            dgv.CellFormatting += Dgv_CellFormatting;
            return dgv;
        }

        private TextBox CreateCodeTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(31, 41, 55),
                BorderStyle = BorderStyle.None
            };
        }

        #endregion

        #region Data Loading

        private void LoadData()
        {
            LoadGeneralTriggerTab();
            LoadActionsComparison();
            PopulateActionsGrid();
        }

        private void LoadGeneralTriggerTab()
        {
            _dgvGeneralTrigger.Columns.Clear();
            _dgvGeneralTrigger.Columns.Add("Property", "Property");
            _dgvGeneralTrigger.Columns.Add("Run1Value", "Run 1 (Left)");
            _dgvGeneralTrigger.Columns.Add("Run2Value", "Run 2 (Right)");

            _dgvGeneralTrigger.Columns["Property"].FillWeight = 24;
            _dgvGeneralTrigger.Columns["Run1Value"].FillWeight = 38;
            _dgvGeneralTrigger.Columns["Run2Value"].FillWeight = 38;

            _dgvGeneralTrigger.Columns["Property"].DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = Color.FromArgb(75, 85, 99)
            };

            // General comparison rows
            AddCompareRow("Run ID", _run1.Id, _run2.Id);
            AddCompareRow("Status", _detail1?.Properties?.Status ?? _run1.Status, _detail2?.Properties?.Status ?? _run2.Status);
            AddCompareRow("Start Time", FormatDateTime(_detail1?.Properties?.StartTime ?? _run1.StartDate), FormatDateTime(_detail2?.Properties?.StartTime ?? _run2.StartDate));
            AddCompareRow("End Time", FormatDateTime(_detail1?.Properties?.EndTime ?? _run1.EndDate), FormatDateTime(_detail2?.Properties?.EndTime ?? _run2.EndDate));
            AddCompareRow("Duration", _run1.Duration, _run2.Duration);
            AddCompareRow("Tracking ID", _detail1?.Properties?.CorrelationClientTrackingId ?? "N/A", _detail2?.Properties?.CorrelationClientTrackingId ?? "N/A");

            // Trigger details comparison
            var trig1 = _detail1?.Properties?.Trigger;
            var trig2 = _detail2?.Properties?.Trigger;

            AddCompareRow("Trigger Name", trig1?.Name ?? "N/A", trig2?.Name ?? "N/A");

            // Parse and compare Trigger Inputs
            string inputs1 = trig1?.InputsLink?.Uri != null ? GetTriggerContent(trig1.InputsLink.Uri) : FormatJson(trig1?.Inputs);
            string inputs2 = trig2?.InputsLink?.Uri != null ? GetTriggerContent(trig2.InputsLink.Uri) : FormatJson(trig2?.Inputs);
            AddJsonComparisonRows("Trigger Input", inputs1, inputs2);

            // Parse and compare Trigger Outputs
            string outputs1 = trig1?.OutputsLink?.Uri != null ? GetTriggerContent(trig1.OutputsLink.Uri) : FormatJson(trig1?.Outputs);
            string outputs2 = trig2?.OutputsLink?.Uri != null ? GetTriggerContent(trig2.OutputsLink.Uri) : FormatJson(trig2?.Outputs);
            AddJsonComparisonRows("Trigger Output", outputs1, outputs2);
        }

        private void LoadActionsComparison()
        {
            _comparisonItems.Clear();

            var actions1Dict = _actions1?.Value?.ToDictionary(a => a.Name ?? "", a => a, StringComparer.OrdinalIgnoreCase) 
                               ?? new Dictionary<string, FlowActionDto>(StringComparer.OrdinalIgnoreCase);
            var actions2Dict = _actions2?.Value?.ToDictionary(a => a.Name ?? "", a => a, StringComparer.OrdinalIgnoreCase) 
                               ?? new Dictionary<string, FlowActionDto>(StringComparer.OrdinalIgnoreCase);

            var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in actions1Dict.Keys) allNames.Add(k);
            foreach (var k in actions2Dict.Keys) allNames.Add(k);

            foreach (var name in allNames.OrderBy(n => n))
            {
                actions1Dict.TryGetValue(name, out var act1);
                actions2Dict.TryGetValue(name, out var act2);

                var item = new ActionComparisonItem
                {
                    Name = name,
                    Type = act1?.Type ?? act2?.Type ?? "Unknown",

                    Status1 = act1?.Properties?.Status ?? "N/A",
                    Status2 = act2?.Properties?.Status ?? "N/A",

                    StartTime1 = act1?.Properties?.StartTime,
                    StartTime2 = act2?.Properties?.StartTime,

                    EndTime1 = act1?.Properties?.EndTime,
                    EndTime2 = act2?.Properties?.EndTime,

                    Duration1 = CalculateDuration(act1),
                    Duration2 = CalculateDuration(act2),

                    Inputs1 = act1?.Properties?.Inputs,
                    Inputs2 = act2?.Properties?.Inputs,

                    Outputs1 = act1?.Properties?.Outputs,
                    Outputs2 = act2?.Properties?.Outputs,

                    Error1 = act1?.Properties?.Error,
                    Error2 = act2?.Properties?.Error
                };
                _comparisonItems.Add(item);
            }
        }

        private void PopulateActionsGrid()
        {
            _dgvActions.Columns.Clear();
            _dgvActions.Columns.Add("ActionName", "Action Name");
            _dgvActions.Columns.Add("Type", "Type");
            _dgvActions.Columns.Add("Status1", "Run 1 Status");
            _dgvActions.Columns.Add("Status2", "Run 2 Status");
            _dgvActions.Columns.Add("Duration1", "Run 1 Duration");
            _dgvActions.Columns.Add("Duration2", "Run 2 Duration");
            _dgvActions.Columns.Add("Comparison", "Comparison Result");

            _dgvActions.Columns["ActionName"].FillWeight = 22;
            _dgvActions.Columns["Type"].FillWeight = 12;
            _dgvActions.Columns["Status1"].FillWeight = 11;
            _dgvActions.Columns["Status2"].FillWeight = 11;
            _dgvActions.Columns["Duration1"].FillWeight = 11;
            _dgvActions.Columns["Duration2"].FillWeight = 11;
            _dgvActions.Columns["Comparison"].FillWeight = 22;

            _dgvActions.Columns["ActionName"].DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 9.5F)
            };

            foreach (var item in _comparisonItems)
            {
                string compResult = "";
                if (item.Status1 == "N/A") compResult = "➕ Only in Run 2";
                else if (item.Status2 == "N/A") compResult = "➖ Only in Run 1";
                else if (item.Status1 == item.Status2)
                {
                    if (item.Status1 == "Succeeded") compResult = "✓ Identical Success";
                    else if (item.Status1 == "Failed") compResult = "❌ Both Failed";
                    else compResult = $"✓ Match ({item.Status1})";
                }
                else
                {
                    compResult = $"⚠️ Mismatch ({item.Status1} ➔ {item.Status2})";
                }

                int idx = _dgvActions.Rows.Add(
                    item.Name,
                    item.Type,
                    item.Status1,
                    item.Status2,
                    item.Duration1,
                    item.Duration2,
                    compResult
                );

                // Color-code the status cells
                var statusCell1 = _dgvActions.Rows[idx].Cells["Status1"];
                statusCell1.Style.ForeColor = GetStatusColor(item.Status1);
                statusCell1.Style.Font = new Font("Segoe UI Semibold", 9.5F);

                var statusCell2 = _dgvActions.Rows[idx].Cells["Status2"];
                statusCell2.Style.ForeColor = GetStatusColor(item.Status2);
                statusCell2.Style.Font = new Font("Segoe UI Semibold", 9.5F);

                // Highlight Mismatch row or failure in comparison cell
                var compCell = _dgvActions.Rows[idx].Cells["Comparison"];
                if (compResult.Contains("Mismatch"))
                {
                    compCell.Style.ForeColor = CancelledColor;
                    compCell.Style.Font = new Font("Segoe UI Semibold", 9.5F);
                    _dgvActions.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(254, 252, 232); // Tint background
                }
                else if (compResult.Contains("Failed") || compResult.Contains("Only in"))
                {
                    compCell.Style.ForeColor = FailedColor;
                    compCell.Style.Font = new Font("Segoe UI Semibold", 9.5F);
                }
                else
                {
                    compCell.Style.ForeColor = SucceededColor;
                }
            }
        }

        #endregion

        #region Helpers

        private void AddCompareRow(string property, string val1, string val2)
        {
            _dgvGeneralTrigger.Rows.Add(property, val1 ?? "N/A", val2 ?? "N/A");
        }

        private void AddJsonComparisonRows(string prefix, string json1, string json2)
        {
            var obj1 = TryParseJson(json1);
            var obj2 = TryParseJson(json2);

            if (obj1 == null && obj2 == null)
            {
                if (!string.IsNullOrWhiteSpace(json1) || !string.IsNullOrWhiteSpace(json2))
                {
                    AddCompareRow(prefix, json1 ?? "N/A", json2 ?? "N/A");
                }
                return;
            }

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (obj1 != null) foreach (var prop in obj1.Properties()) keys.Add(prop.Name);
            if (obj2 != null) foreach (var prop in obj2.Properties()) keys.Add(prop.Name);

            foreach (var key in keys.OrderBy(k => k))
            {
                string val1 = obj1?[key]?.Type == JTokenType.Object || obj1?[key]?.Type == JTokenType.Array
                    ? obj1[key].ToString(Formatting.Indented)
                    : obj1?[key]?.ToString() ?? "N/A";

                string val2 = obj2?[key]?.Type == JTokenType.Object || obj2?[key]?.Type == JTokenType.Array
                    ? obj2[key].ToString(Formatting.Indented)
                    : obj2?[key]?.ToString() ?? "N/A";

                AddCompareRow($"{prefix} › {key}", val1, val2);
            }
        }

        private JObject TryParseJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return null;
            try
            {
                var token = JToken.Parse(jsonString);
                return token as JObject;
            }
            catch
            {
                return null;
            }
        }

        private string CalculateDuration(FlowActionDto action)
        {
            if (action?.Properties?.StartTime == null || action?.Properties?.EndTime == null) return "N/A";
            var span = action.Properties.EndTime.Value - action.Properties.StartTime.Value;
            return span.TotalSeconds < 1
                ? $"{span.TotalMilliseconds:F0} ms"
                : span.ToString(@"hh\:mm\:ss\.fff");
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
            if (string.IsNullOrEmpty(uri)) return null;
            try
            {
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
            return Color.FromArgb(75, 85, 99);
        }

        #endregion

        #region Event Handlers

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null || e.RowIndex < 0) return;

            var cell = dgv[e.ColumnIndex, e.RowIndex];
            if (cell.Value != null && cell.Value.ToString().Length > 100)
            {
                dgv.Rows[e.RowIndex].Height = Math.Min(
                    Math.Max(dgv.Rows[e.RowIndex].Height, 60),
                    250
                );
            }
        }

        private void DgvGeneralTrigger_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            Dgv_CellFormatting(sender, e);

            // Highlight cells where Run 1 value != Run 2 value
            if (e.ColumnIndex == 1 || e.ColumnIndex == 2)
            {
                var val1 = _dgvGeneralTrigger.Rows[e.RowIndex].Cells[1].Value?.ToString();
                var val2 = _dgvGeneralTrigger.Rows[e.RowIndex].Cells[2].Value?.ToString();

                if (val1 != val2)
                {
                    e.CellStyle.BackColor = DiffHighlightColor;
                    e.CellStyle.ForeColor = DiffHighlightForeColor;
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
                }
            }
        }

        private void DgvActions_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvActions.SelectedRows.Count == 0) return;
            int idx = _dgvActions.SelectedRows[0].Index;
            if (idx < 0 || idx >= _comparisonItems.Count) return;

            var item = _comparisonItems[idx];
            DisplayActionDetails(item);
        }

        private void DisplayActionDetails(ActionComparisonItem item)
        {
            string in1 = FormatJson(item.Inputs1);
            string in2 = FormatJson(item.Inputs2);

            string out1 = FormatJson(item.Outputs1);
            string out2 = FormatJson(item.Outputs2);

            if (item.Error1 != null)
            {
                out1 += $"\r\n\r\n--- ERROR DETAILS ---\r\n{FormatJson(item.Error1)}";
            }
            if (item.Error2 != null)
            {
                out2 += $"\r\n\r\n--- ERROR DETAILS ---\r\n{FormatJson(item.Error2)}";
            }

            _tbInputs1.Text = in1;
            _tbInputs2.Text = in2;
            _tbOutputs1.Text = out1;
            _tbOutputs2.Text = out2;

            // Highlight input diffs
            bool inputsDiffer = (in1 != in2);
            _tbInputs1.BackColor = inputsDiffer ? Color.FromArgb(254, 243, 199) : Color.White;
            _tbInputs2.BackColor = inputsDiffer ? Color.FromArgb(254, 243, 199) : Color.White;

            // Highlight output diffs
            bool outputsDiffer = (out1 != out2);
            _tbOutputs1.BackColor = outputsDiffer ? Color.FromArgb(254, 243, 199) : Color.White;
            _tbOutputs2.BackColor = outputsDiffer ? Color.FromArgb(254, 243, 199) : Color.White;
        }

        #endregion
    }

    public class ActionComparisonItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status1 { get; set; }
        public string Status2 { get; set; }
        public DateTime? StartTime1 { get; set; }
        public DateTime? StartTime2 { get; set; }
        public DateTime? EndTime1 { get; set; }
        public DateTime? EndTime2 { get; set; }
        public string Duration1 { get; set; }
        public string Duration2 { get; set; }
        public object Inputs1 { get; set; }
        public object Inputs2 { get; set; }
        public object Outputs1 { get; set; }
        public object Outputs2 { get; set; }
        public object Error1 { get; set; }
        public object Error2 { get; set; }
    }
}
