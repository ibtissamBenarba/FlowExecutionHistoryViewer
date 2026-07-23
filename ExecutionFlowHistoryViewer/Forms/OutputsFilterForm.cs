using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class OutputsFilterForm : Form
    {
        private MyPluginControl _plugin;
        private List<string> _triggerAttributes;
        private List<string> _actionNames;
        private List<FilterOperator> _operators;

        private readonly List<FilterConditionControl> _conditionControls = new List<FilterConditionControl>();

        public ConditionGroup ConditionGroup { get; set; }

        private ComboBox cbGroupOperator;
        private ComboBox cmbScope;
        private Label lblLoading;
        private Label lblPreview;
        private TableLayoutPanel tableLayoutPanel2;
        private Button btnAdd;
        private Button btnFilter;
        private Button btnCancel;
        private bool _discoveryComplete = false;

        public OutputsFilterForm(MyPluginControl plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _operators = Enum.GetValues(typeof(FilterOperator)).Cast<FilterOperator>().ToList();
            InitializeForm();

            Task.Run(() => InitializeDiscovery());
        }

        private void InitializeForm()
        {
            this.Text = "Outputs Filter (Trigger & Actions)";
            this.Size = new Size(950, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(10);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // Group op + scope
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // Loading
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Conditions
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // Preview
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // Buttons

            // --- Top: Group Operator + Scope ---
            var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            topPanel.Controls.Add(new Label { Text = "Group Operator:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });

            cbGroupOperator = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            cbGroupOperator.DataSource = Enum.GetValues(typeof(GroupOperator)).Cast<GroupOperator>().ToList();
            cbGroupOperator.SelectedItem = GroupOperator.And;
            topPanel.Controls.Add(cbGroupOperator);

            topPanel.Controls.Add(new Label { Text = "  Scan limit:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });

            cmbScope = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
            cmbScope.Items.AddRange(new object[] { "All loaded runs", "Current page only", "Max 50 runs", "Max 100 runs", "Max 200 runs" });
            cmbScope.SelectedIndex = 2;
            topPanel.Controls.Add(cmbScope);

            mainLayout.Controls.Add(topPanel, 0, 0);

            // --- Loading label ---
            lblLoading = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Discovering trigger attributes and actions...",
                ForeColor = Color.DodgerBlue,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblLoading, 0, 1);

            // --- Conditions Panel ---
            tableLayoutPanel2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 0,
                AutoScroll = true
            };
            tableLayoutPanel2.RowStyles.Clear();
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var scrollHost = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            scrollHost.Controls.Add(tableLayoutPanel2);
            mainLayout.Controls.Add(scrollHost, 0, 2);

            // --- Preview label ---
            lblPreview = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Conditions preview will appear here...",
                ForeColor = Color.Gray,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblPreview, 0, 3);

            // --- Buttons ---
            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 100, Height = 30 };
            btnCancel.Click += (s, e) => Close();

            btnFilter = new Button { Text = "Apply Filter", DialogResult = DialogResult.OK, Width = 120, Height = 30, Margin = new Padding(0, 0, 10, 0), Enabled = false };
            btnFilter.Click += btnFilter_Click;

            btnAdd = new Button { Text = "+ Add Condition", Width = 120, Height = 30, Margin = new Padding(0, 0, 10, 0) };
            btnAdd.Click += (s, e) => AddFilterConditionControl();

            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnFilter);
            bottomPanel.Controls.Add(btnAdd);
            mainLayout.Controls.Add(bottomPanel, 0, 4);

            this.Controls.Add(mainLayout);
            tableLayoutPanel2.Layout += tableLayoutPanel2_Layout;

            AddFilterConditionControl();
        }

        private void InitializeDiscovery()
        {
            var selectedFlows = _plugin.GetSelectedFlows();
            var allRuns = _plugin.GetAllRuns();
            var allTriggerAttributes = new List<string>();
            var allActionNames = new List<string>();
            var client = _plugin.CreateFlowClient();

            if (client != null && selectedFlows.Count > 0 && allRuns.Count > 0)
            {
                int flowCount = 0;
                foreach (var flow in selectedFlows)
                {
                    var firstRun = allRuns.FirstOrDefault(r =>
                        !string.IsNullOrEmpty(r.FlowId) && r.FlowId.Equals(flow.Id, StringComparison.OrdinalIgnoreCase))
                        ?? allRuns.FirstOrDefault(r => !string.IsNullOrEmpty(r.FlowName) && r.FlowName.Equals(flow.DisplayName, StringComparison.OrdinalIgnoreCase));

                    if (firstRun == null) continue;

                    // Discover trigger attributes
                    try
                    {
                        var outputs = client.GetTriggerOutputs(flow.Id, firstRun.Id);
                        if (outputs != null)
                        {
                            var body = outputs["body"] as JObject;
                            if (body != null)
                                allTriggerAttributes.AddRange(body.Properties().Select(p => p.Name));
                            else
                                allTriggerAttributes.AddRange(outputs.Properties().Select(p => p.Name));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Trigger attribute discovery failed for flow {flow.DisplayName}: {ex.Message}");
                    }

                    // Discover action names
                    try
                    {
                        var actions = client.GetRunActions(flow.Id, firstRun.Id);
                        if (actions?.Value != null)
                        {
                            foreach (var action in actions.Value)
                            {
                                if (!string.IsNullOrEmpty(action.Name))
                                    allActionNames.Add(action.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Action discovery failed for flow {flow.DisplayName}: {ex.Message}");
                    }

                    flowCount++;
                    if (flowCount % 3 == 0)
                        System.Threading.Thread.Sleep(200);
                }
            }

            _triggerAttributes = allTriggerAttributes.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();
            _actionNames = allActionNames.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();

            this.Invoke((MethodInvoker)(() =>
            {
                foreach (var control in _conditionControls)
                {
                    control.RefreshTriggerAttributes(_triggerAttributes);
                    control.RefreshActionNames(_actionNames);
                }

                if (_triggerAttributes.Count == 0 && _actionNames.Count == 0)
                {
                    lblLoading.Text = "No trigger attributes or actions found. Ensure runs were fetched.";
                    lblLoading.ForeColor = Color.Red;
                }
                else
                {
                    lblLoading.Text = $"{_triggerAttributes.Count} trigger attribute(s), {_actionNames.Count} action(s) discovered.";
                    lblLoading.ForeColor = Color.Green;
                    _discoveryComplete = true;
                    btnFilter.Enabled = true;
                }
            }));
        }

        private void AddFilterConditionControl()
        {
            var control = new FilterConditionControl(_triggerAttributes ?? new List<string>(), _actionNames ?? new List<string>(), _operators);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            control.RemoveButtonClicked += OnRemoveButtonClicked;
            control.ValueChanged += OnConditionChanged;
            control.ActionAttributeRequested += OnActionAttributeRequested;

            _conditionControls.Add(control);

            tableLayoutPanel2.Controls.Add(control, 0, tableLayoutPanel2.RowCount);
            tableLayoutPanel2.RowCount++;

            UpdatePreview();
        }

        private void OnActionAttributeRequested(object sender, ActionAttributeRequestEventArgs e)
        {
            Task.Run(() =>
            {
                var selectedFlows = _plugin.GetSelectedFlows();
                var allRuns = _plugin.GetAllRuns();
                var client = _plugin.CreateFlowClient();
                var attributes = new List<string>();

                if (client != null && selectedFlows.Count > 0 && allRuns.Count > 0)
                {
                    foreach (var flow in selectedFlows)
                    {
                        var firstRun = allRuns.FirstOrDefault(r =>
                            !string.IsNullOrEmpty(r.FlowId) && r.FlowId.Equals(flow.Id, StringComparison.OrdinalIgnoreCase))
                            ?? allRuns.FirstOrDefault(r => !string.IsNullOrEmpty(r.FlowName) && r.FlowName.Equals(flow.DisplayName, StringComparison.OrdinalIgnoreCase));

                        if (firstRun == null) continue;

                        try
                        {
                            var rawActions = client.GetRunActionsRaw(flow.Id, firstRun.Id);
                            var actionsArray = rawActions?["value"] as JArray;
                            if (actionsArray == null) continue;

                            // Try exact match first
                            var actionObj = actionsArray.FirstOrDefault(a =>
                                a["name"]?.ToString().Equals(e.ActionName, StringComparison.OrdinalIgnoreCase) == true);

                            // If not found, try ignoring spaces/parentheses/underscores
                            if (actionObj == null)
                            {
                                string normalizedSearch = e.ActionName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("_", "").ToLower();
                                actionObj = actionsArray.FirstOrDefault(a =>
                                {
                                    string normalizedApi = a["name"]?.ToString().Replace(" ", "").Replace("(", "").Replace(")", "").Replace("_", "").ToLower();
                                    return normalizedApi == normalizedSearch;
                                });
                            }

                            if (actionObj == null) continue;

                            var props = actionObj["properties"];
                            if (props == null) continue;

                            // Try outputs
                            JObject actionOutputs = null;
                            var outputs = props["outputs"];
                            if (outputs != null && outputs.Type != JTokenType.Null)
                            {
                                if (outputs is JObject jOut) actionOutputs = jOut;
                            }

                            // Try outputsLink
                            if (actionOutputs == null)
                            {
                                var outputsLink = props["outputsLink"]?["uri"]?.ToString();
                                if (!string.IsNullOrEmpty(outputsLink))
                                {
                                    try { actionOutputs = JObject.Parse(client.GetContentFromLink(outputsLink)); }
                                    catch { }
                                }
                            }

                            // Try inputs
                            if (actionOutputs == null)
                            {
                                var inputs = props["inputs"];
                                if (inputs != null && inputs.Type != JTokenType.Null)
                                {
                                    if (inputs is JObject jIn) actionOutputs = jIn;
                                }
                            }

                            // Try inputsLink
                            if (actionOutputs == null)
                            {
                                var inputsLink = props["inputsLink"]?["uri"]?.ToString();
                                if (!string.IsNullOrEmpty(inputsLink))
                                {
                                    try { actionOutputs = JObject.Parse(client.GetContentFromLink(inputsLink)); }
                                    catch { }
                                }
                            }

                            var allPaths = new List<string>();

                            // Add from outputs (if any)
                            if (actionOutputs != null)
                            {
                                ExtractJsonPaths(actionOutputs, "", allPaths);
                            }

                            // Also add from inputs (parameters)
                            JObject actionInputs = null;
                            var inputsToken = props["inputs"];
                            if (inputsToken != null && inputsToken.Type != JTokenType.Null)
                            {
                                actionInputs = inputsToken as JObject;
                            }
                            else
                            {
                                var inputsLink = props["inputsLink"]?["uri"]?.ToString();
                                if (!string.IsNullOrEmpty(inputsLink))
                                {
                                    try { actionInputs = JObject.Parse(client.GetContentFromLink(inputsLink)); }
                                    catch { }
                                }
                            }

                            if (actionInputs != null)
                            {
                                ExtractJsonPaths(actionInputs, "", allPaths);
                            }

                            if (allPaths.Count > 0)
                            {
                                attributes.AddRange(allPaths);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Action attribute discovery failed for {e.ActionName}: {ex.Message}");
                        }
                    }
                }

                var distinct = attributes.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();

                this.Invoke((MethodInvoker)(() =>
                {
                    if (sender is FilterConditionControl ctrl)
                        ctrl.SetActionAttributes(e.ActionName, distinct);
                }));
            });
        }

        private void ExtractJsonPaths(JToken token, string currentPath, List<string> paths)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        string newPath = string.IsNullOrEmpty(currentPath) ? prop.Name : currentPath + "." + prop.Name;
                        ExtractJsonPaths(prop.Value, newPath, paths);
                    }
                    break;
                case JTokenType.Array:
                    for (int i = 0; i < ((JArray)token).Count; i++)
                    {
                        ExtractJsonPaths(((JArray)token)[i], currentPath + $"[{i}]", paths);
                    }
                    break;
                default:
                    if (!string.IsNullOrEmpty(currentPath))
                        paths.Add(currentPath);
                    break;
            }
        }

        private void OnRemoveButtonClicked(object sender, FilterConditionControl control)
        {
            if (_conditionControls.Count <= 1)
            {
                MessageBox.Show("At least one condition is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _conditionControls.Remove(control);
            tableLayoutPanel2.Controls.Remove(control);
            tableLayoutPanel2.RowCount--;

            for (int i = 0; i < _conditionControls.Count; i++)
                _conditionControls[i].RowIndex = i;

            tableLayoutPanel2.PerformLayout();
            UpdatePreview();
        }

        private void OnConditionChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var conditions = GetAllFilterConditions();
            if (conditions.Count == 0)
            {
                lblPreview.Text = "No conditions defined.";
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Group: {cbGroupOperator.SelectedItem} | {conditions.Count} condition(s):");
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                string target = c.Target == FilterTarget.Trigger ? "Trigger" : $"Action[{c.ActionName}]";
                sb.AppendLine($"  [{i + 1}] [{target}] {c.Attribute}  {c.Operator}  '{c.Value}'");
            }
            lblPreview.Text = sb.ToString();
            lblPreview.ForeColor = Color.Black;
        }

        private List<FilterCondition> GetAllFilterConditions()
        {
            return _conditionControls.Select(c => c.FilterCondition).ToList();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            if (!_discoveryComplete)
            {
                MessageBox.Show("Please wait for discovery to finish loading.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.None;
                return;
            }

            var conditions = GetAllFilterConditions();

            foreach (var c in conditions)
            {
                if (string.IsNullOrWhiteSpace(c.Attribute))
                {
                    MessageBox.Show("Please select an attribute for all conditions.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (c.Target == FilterTarget.Action && string.IsNullOrWhiteSpace(c.ActionName))
                {
                    MessageBox.Show("Please select an action for all action-targeted conditions.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            int maxRuns = 0;
            string scope = cmbScope.SelectedItem?.ToString();
            if (scope == "Current page only")
                maxRuns = _plugin.GetCurrentPageSize();
            else if (scope?.StartsWith("Max ") == true)
            {
                var numPart = scope.Replace("Max ", "").Replace(" runs", "").Trim();
                int.TryParse(numPart, out maxRuns);
            }

            ConditionGroup = new ConditionGroup
            {
                GroupOperator = (GroupOperator)cbGroupOperator.SelectedItem,
                FilterConditions = conditions
            };

            _plugin.ApplyOutputsFilter(ConditionGroup, maxRuns);
            Close();
        }

        private void tableLayoutPanel2_Layout(object sender, LayoutEventArgs e)
        {
            if (tableLayoutPanel2.HorizontalScroll.Visible)
                tableLayoutPanel2.Padding = new Padding(0, 0, SystemInformation.VerticalScrollBarWidth, 0);
            else
                tableLayoutPanel2.Padding = new Padding(0);
        }
    }
}