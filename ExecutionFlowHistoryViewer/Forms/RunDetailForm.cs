using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly Dictionary<string, string> _triggerContentsCache;

        // Table controls for each tab
        private DataGridView _dgvGeneral;
        private DataGridView _dgvActions;
        private DataGridView _dgvErrors;

        // Redesigned Trigger Tab controls
        private ListBox _lstTriggerCategories;
        private DataGridView _dgvTriggerFields;
        private TextBox _txtTriggerJsonViewer;
        private TextBox _txtTriggerFieldSearch;
        private Button _btnCopyTriggerValue;
        private Button _btnCopyTriggerJson;
        private Label _lblTriggerCategoryTitle;

        private Dictionary<string, Dictionary<string, string>> _parsedTriggerCategories;
        private string _rawInputsJson;
        private string _rawOutputsJson;

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

        public RunDetailForm(FlowRun run, FlowRunDetailDto detail, FlowActionsResponseDto actions, IFlowClient flowClient, Dictionary<string, string> triggerContentsCache)
        {
            _run = run;
            _detail = detail;
            _actions = actions;
            _flowClient = flowClient;
            _triggerContentsCache = triggerContentsCache;
            InitializeComponent();
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
            
            var splitTrigger = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 220
            };

            _lstTriggerCategories = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(247, 250, 252)
            };
            _lstTriggerCategories.SelectedIndexChanged += LstTriggerCategories_SelectedIndexChanged;
            
            splitTrigger.Panel1.Controls.Add(_lstTriggerCategories);

            var pnlRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            pnlRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); 
            pnlRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); 
            pnlRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); 

            _lblTriggerCategoryTitle = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 12F),
                ForeColor = HeaderBackColor,
                Text = "Trigger Details",
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlRight.Controls.Add(_lblTriggerCategoryTitle, 0, 0);

            var pnlControls = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 5)
            };

            var lblSearch = new Label
            {
                Text = "Filter Fields:",
                Font = new Font("Segoe UI Semibold", 9F),
                Location = new Point(0, 8),
                AutoSize = true
            };
            _txtTriggerFieldSearch = new TextBox
            {
                Location = new Point(90, 5),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9F)
            };
            _txtTriggerFieldSearch.TextChanged += TxtTriggerFieldSearch_TextChanged;

            _btnCopyTriggerValue = new Button
            {
                Text = "📋 Copy Value",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(300, 4),
                Size = new Size(110, 27),
                BackColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            _btnCopyTriggerValue.Click += BtnCopyTriggerValue_Click;

            _btnCopyTriggerJson = new Button
            {
                Text = "📋 Copy JSON",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(415, 4),
                Size = new Size(110, 27),
                BackColor = Color.White,
                FlatStyle = FlatStyle.System
            };
            _btnCopyTriggerJson.Click += BtnCopyTriggerJson_Click;

            pnlControls.Controls.Add(lblSearch);
            pnlControls.Controls.Add(_txtTriggerFieldSearch);
            pnlControls.Controls.Add(_btnCopyTriggerValue);
            pnlControls.Controls.Add(_btnCopyTriggerJson);
            pnlRight.Controls.Add(pnlControls, 0, 1);

            var pnlContentOverlay = new Panel { Dock = DockStyle.Fill };

            _dgvTriggerFields = CreateStyledGrid();
            _dgvTriggerFields.Dock = DockStyle.Fill;
            
            _txtTriggerJsonViewer = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9.5F),
                BackColor = Color.FromArgb(248, 249, 250),
                ForeColor = Color.FromArgb(33, 37, 41),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            pnlContentOverlay.Controls.Add(_dgvTriggerFields);
            pnlContentOverlay.Controls.Add(_txtTriggerJsonViewer);
            pnlRight.Controls.Add(pnlContentOverlay, 0, 2);

            splitTrigger.Panel2.Controls.Add(pnlRight);
            tabTrigger.Controls.Add(splitTrigger);

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
            if (_detail?.Properties?.Trigger == null)
            {
                _lstTriggerCategories.Items.Add("Trigger Info");
                _parsedTriggerCategories = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                _parsedTriggerCategories["Trigger Info"] = new Dictionary<string, string> { { "Info", "No trigger data available." } };
                _lstTriggerCategories.SelectedIndex = 0;
                return;
            }

            var trigger = _detail.Properties.Trigger;

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

            ParseTriggerData(inputs, outputs, out _parsedTriggerCategories, out _rawInputsJson, out _rawOutputsJson);

            // Populate categories listbox
            _lstTriggerCategories.Items.Clear();
            foreach (var cat in _parsedTriggerCategories.Keys)
            {
                if (_parsedTriggerCategories[cat].Count > 0)
                {
                    _lstTriggerCategories.Items.Add(cat);
                }
            }

            if (!string.IsNullOrEmpty(_rawInputsJson)) _lstTriggerCategories.Items.Add("Raw Input JSON");
            if (!string.IsNullOrEmpty(_rawOutputsJson)) _lstTriggerCategories.Items.Add("Raw Output JSON");

            if (_lstTriggerCategories.Items.Count > 0)
            {
                _lstTriggerCategories.SelectedIndex = 0;
            }
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
            if (string.IsNullOrEmpty(uri)) return null;

            string content = null;
            if (_triggerContentsCache != null)
            {
                lock (_triggerContentsCache)
                {
                    _triggerContentsCache.TryGetValue(uri, out content);
                }
            }

            if (content == null)
            {
                try
                {
                    content = _flowClient.GetContentFromLink(uri);
                    if (content != null && _triggerContentsCache != null)
                    {
                        lock (_triggerContentsCache)
                        {
                            _triggerContentsCache[uri] = content;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Error loading content: {ex.Message}";
                }
            }
            return content;
        }

        private void LstTriggerCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstTriggerCategories.SelectedItem == null) return;
            string selected = _lstTriggerCategories.SelectedItem.ToString();
            _lblTriggerCategoryTitle.Text = selected;
            _txtTriggerFieldSearch.Text = "";

            if (selected == "Raw Input JSON")
            {
                _dgvTriggerFields.Visible = false;
                _txtTriggerJsonViewer.Visible = true;
                _txtTriggerJsonViewer.Text = _rawInputsJson;
                
                _txtTriggerFieldSearch.Enabled = false;
                _btnCopyTriggerValue.Enabled = false;
                _btnCopyTriggerJson.Enabled = true;
            }
            else if (selected == "Raw Output JSON")
            {
                _dgvTriggerFields.Visible = false;
                _txtTriggerJsonViewer.Visible = true;
                _txtTriggerJsonViewer.Text = _rawOutputsJson;

                _txtTriggerFieldSearch.Enabled = false;
                _btnCopyTriggerValue.Enabled = false;
                _btnCopyTriggerJson.Enabled = true;
            }
            else
            {
                _dgvTriggerFields.Visible = true;
                _txtTriggerJsonViewer.Visible = false;

                _txtTriggerFieldSearch.Enabled = true;
                _btnCopyTriggerValue.Enabled = true;
                _btnCopyTriggerJson.Enabled = true;

                BindTriggerFieldsGrid(selected);
            }
        }

        private void BindTriggerFieldsGrid(string category, string filterText = "")
        {
            _dgvTriggerFields.Columns.Clear();
            _dgvTriggerFields.Columns.Add("Field", "Field");
            _dgvTriggerFields.Columns.Add("Value", "Value");
            _dgvTriggerFields.Columns["Field"].FillWeight = 30;
            _dgvTriggerFields.Columns["Value"].FillWeight = 70;

            _dgvTriggerFields.Columns["Field"].DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 9.5F),
                ForeColor = Color.FromArgb(45, 55, 72)
            };

            if (_parsedTriggerCategories == null || !_parsedTriggerCategories.TryGetValue(category, out var fields)) return;

            foreach (var kvp in fields)
            {
                if (string.IsNullOrEmpty(filterText) ||
                    kvp.Key.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Value.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _dgvTriggerFields.Rows.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private void TxtTriggerFieldSearch_TextChanged(object sender, EventArgs e)
        {
            if (_lstTriggerCategories.SelectedItem == null) return;
            string selected = _lstTriggerCategories.SelectedItem.ToString();
            if (selected != "Raw Input JSON" && selected != "Raw Output JSON")
            {
                BindTriggerFieldsGrid(selected, _txtTriggerFieldSearch.Text.Trim());
            }
        }

        private void BtnCopyTriggerValue_Click(object sender, EventArgs e)
        {
            if (_dgvTriggerFields.SelectedRows.Count == 0) return;
            var row = _dgvTriggerFields.SelectedRows[0];
            string val = row.Cells["Value"].Value?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                Clipboard.SetText(val);
                MessageBox.Show("Value copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCopyTriggerJson_Click(object sender, EventArgs e)
        {
            if (_lstTriggerCategories.SelectedItem == null) return;
            string selected = _lstTriggerCategories.SelectedItem.ToString();

            if (selected == "Raw Input JSON")
            {
                Clipboard.SetText(_rawInputsJson);
                MessageBox.Show("Raw Input JSON copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (selected == "Raw Output JSON")
            {
                Clipboard.SetText(_rawOutputsJson);
                MessageBox.Show("Raw Output JSON copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (_parsedTriggerCategories != null && _parsedTriggerCategories.TryGetValue(selected, out var dict))
                {
                    string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                    Clipboard.SetText(json);
                    MessageBox.Show($"{selected} category copied to clipboard as JSON!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ParseTriggerData(string inputsJson, string outputsJson, out Dictionary<string, Dictionary<string, string>> categories, out string rawInputs, out string rawOutputs)
        {
            categories = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            rawInputs = inputsJson;
            rawOutputs = outputsJson;

            categories["Trigger Info"] = new Dictionary<string, string>();
            categories["Inputs - Parameters"] = new Dictionary<string, string>();
            categories["Inputs - Host"] = new Dictionary<string, string>();
            categories["Outputs - Headers"] = new Dictionary<string, string>();
            categories["Outputs - Body"] = new Dictionary<string, string>();
            categories["Inputs - Other"] = new Dictionary<string, string>();
            categories["Outputs - Other"] = new Dictionary<string, string>();

            if (_detail?.Properties?.Trigger != null)
            {
                var trigger = _detail.Properties.Trigger;
                categories["Trigger Info"]["Trigger Name"] = trigger.Name ?? "N/A";
                categories["Trigger Info"]["Trigger Status"] = trigger.Status ?? "N/A";
                categories["Trigger Info"]["Start Time"] = FormatDateTime(trigger.StartTime);
                categories["Trigger Info"]["End Time"] = FormatDateTime(trigger.EndTime);
            }

            // Parse inputs
            if (!string.IsNullOrWhiteSpace(inputsJson))
            {
                try
                {
                    var token = JToken.Parse(inputsJson);
                    if (token is JObject obj)
                    {
                        bool hasStructuredInput = false;

                        if (obj["parameters"] is JObject paramsObj)
                        {
                            FlattenJObject(paramsObj, categories["Inputs - Parameters"], "");
                            hasStructuredInput = true;
                        }
                        if (obj["host"] is JObject hostObj)
                        {
                            FlattenJObject(hostObj, categories["Inputs - Host"], "");
                            hasStructuredInput = true;
                        }

                        foreach (var prop in obj.Properties())
                        {
                            if (!string.Equals(prop.Name, "parameters", StringComparison.OrdinalIgnoreCase) && 
                                !string.Equals(prop.Name, "host", StringComparison.OrdinalIgnoreCase))
                            {
                                if (prop.Value is JObject subObj) FlattenJObject(subObj, categories["Inputs - Other"], prop.Name + "/");
                                else categories["Inputs - Other"][prop.Name] = prop.Value.ToString();
                            }
                        }

                        if (!hasStructuredInput && categories["Inputs - Other"].Count == 0)
                        {
                            FlattenJObject(obj, categories["Inputs - Other"], "");
                        }
                    }
                }
                catch { }
            }

            // Parse outputs
            if (!string.IsNullOrWhiteSpace(outputsJson))
            {
                try
                {
                    var token = JToken.Parse(outputsJson);
                    if (token is JObject obj)
                    {
                        bool hasStructuredOutput = false;

                        if (obj["headers"] is JObject headersObj)
                        {
                            FlattenJObject(headersObj, categories["Outputs - Headers"], "");
                            hasStructuredOutput = true;
                        }
                        if (obj["body"] is JObject bodyObj)
                        {
                            FlattenJObject(bodyObj, categories["Outputs - Body"], "");
                            hasStructuredOutput = true;
                        }

                        foreach (var prop in obj.Properties())
                        {
                            if (!string.Equals(prop.Name, "headers", StringComparison.OrdinalIgnoreCase) && 
                                !string.Equals(prop.Name, "body", StringComparison.OrdinalIgnoreCase))
                            {
                                if (prop.Value is JObject subObj) FlattenJObject(subObj, categories["Outputs - Other"], prop.Name + "/");
                                else categories["Outputs - Other"][prop.Name] = prop.Value.ToString();
                            }
                        }

                        if (!hasStructuredOutput && categories["Outputs - Other"].Count == 0)
                        {
                            FlattenJObject(obj, categories["Outputs - Other"], "");
                        }
                    }
                }
                catch { }
            }
        }

        private void FlattenJObject(JObject obj, Dictionary<string, string> dict, string prefix)
        {
            foreach (var prop in obj.Properties())
            {
                string key = prefix + prop.Name;
                if (prop.Value is JObject subObj)
                {
                    FlattenJObject(subObj, dict, key + "/");
                }
                else if (prop.Value is JArray jArr)
                {
                    dict[key] = prop.Value.ToString(Formatting.None);
                }
                else
                {
                    dict[key] = prop.Value.ToString();
                }
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