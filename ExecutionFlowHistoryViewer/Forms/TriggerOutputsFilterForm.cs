using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class TriggerOutputsFilterForm : Form
    {
        private MyPluginControl _plugin;
        private List<string> _attributes;
        private List<TriggerOutputOperator> _operators;

        // CRITICAL: Explicit list so we never rely on TableLayoutPanel.Controls order
        private readonly List<FilterConditionControl> _conditionControls = new List<FilterConditionControl>();

        public ConditionGroup ConditionGroup { get; set; }

        private ComboBox cbGroupOperator;
        private ComboBox cmbScope;
        private Label lblLoading;
        private Label lblPreview;   // Shows captured conditions for verification
        private TableLayoutPanel tableLayoutPanel2;
        private Button btnAdd;
        private Button btnFilter;
        private Button btnCancel;
        private bool _attributesLoaded = false;

        public TriggerOutputsFilterForm(MyPluginControl plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _operators = Enum.GetValues(typeof(TriggerOutputOperator)).Cast<TriggerOutputOperator>().ToList();
            InitializeForm();

            // Start attribute discovery in background
            Task.Run(() => InitializeAttributes());
        }

        private void InitializeForm()
        {
            this.Text = "Trigger Outputs Filter";
            this.Size = new Size(780, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(10);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Group op + scope
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Loading
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Conditions
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Preview label
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Buttons

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
                Text = "Loading trigger attributes...",
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

            // --- Preview label (shows what will be sent) ---
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

            // Start with one empty row
            AddFilterConditionControl();
        }

        private void InitializeAttributes()
        {
            var selectedFlows = _plugin.GetSelectedFlows();
            var allRuns = _plugin.GetAllRuns();
            var allAttributes = new List<string>();
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

                    try
                    {
                        var outputs = client.GetTriggerOutputs(flow.Id, firstRun.Id);
                        if (outputs == null) continue;

                        var body = outputs["body"] as Newtonsoft.Json.Linq.JObject;
                        if (body != null)
                            allAttributes.AddRange(body.Properties().Select(p => p.Name));
                        else
                            allAttributes.AddRange(outputs.Properties().Select(p => p.Name));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Attribute discovery failed for flow {flow.DisplayName}: {ex.Message}");
                    }

                    flowCount++;
                    if (flowCount % 3 == 0)
                        System.Threading.Thread.Sleep(200);
                }
            }

            _attributes = allAttributes.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList();

            this.Invoke((MethodInvoker)(() =>
            {
                foreach (var control in _conditionControls)
                    control.RefreshAttributes(_attributes);

                if (_attributes.Count == 0)
                {
                    lblLoading.Text = "No trigger attributes found. Ensure runs were fetched.";
                    lblLoading.ForeColor = Color.Red;
                }
                else
                {
                    lblLoading.Text = $"{_attributes.Count} attribute(s) discovered.";
                    lblLoading.ForeColor = Color.Green;
                    _attributesLoaded = true;
                    btnFilter.Enabled = true;
                }
            }));
        }

        private void AddFilterConditionControl()
        {
            var control = new FilterConditionControl(_attributes ?? new List<string>(), _operators);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            control.RemoveButtonClicked += OnRemoveButtonClicked;
            control.ValueChanged += OnConditionChanged;  // NEW: update preview on every keystroke

            _conditionControls.Add(control);  // EXPLICIT TRACKING

            tableLayoutPanel2.Controls.Add(control, 0, tableLayoutPanel2.RowCount);
            tableLayoutPanel2.RowCount++;

            UpdatePreview();
        }

        private void OnRemoveButtonClicked(object sender, FilterConditionControl control)
        {
            if (_conditionControls.Count <= 1)
            {
                MessageBox.Show("At least one condition is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _conditionControls.Remove(control);  // EXPLICIT TRACKING
            tableLayoutPanel2.Controls.Remove(control);
            tableLayoutPanel2.RowCount--;

            // Renumber rows
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
                sb.AppendLine($"  [{i + 1}] {c.Attribute}  {c.Operator}  '{c.Value}'");
            }
            lblPreview.Text = sb.ToString();
            lblPreview.ForeColor = Color.Black;
        }

        private List<FilterCondition> GetAllFilterConditions()
        {
            // CRITICAL: Read from our explicit list, NOT from TableLayoutPanel.Controls
            return _conditionControls.Select(c => c.FilterCondition).ToList();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            if (!_attributesLoaded)
            {
                MessageBox.Show("Please wait for attributes to finish loading.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.None;
                return;
            }

            var conditions = GetAllFilterConditions();

            // Validation
            foreach (var c in conditions)
            {
                if (string.IsNullOrWhiteSpace(c.Attribute))
                {
                    MessageBox.Show("Please select an attribute for all conditions.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            // Resolve scan limit
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

            _plugin.ApplyTriggerOutputsFilter(ConditionGroup, maxRuns);
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