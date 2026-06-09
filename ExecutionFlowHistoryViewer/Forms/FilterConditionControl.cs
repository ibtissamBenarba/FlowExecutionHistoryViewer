using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public class ActionAttributeRequestEventArgs : EventArgs
    {
        public string ActionName { get; }
        public ActionAttributeRequestEventArgs(string actionName) { ActionName = actionName; }
    }

    public partial class FilterConditionControl : UserControl
    {
        private ComboBox cmbTarget;
        private ComboBox cmbAction;
        private ComboBox cmbAttribute;
        private ComboBox cmbOperator;
        private TextBox txtValue;
        private Button btnRemove;
        private Label lblLoading;

        private List<string> _triggerAttributes = new List<string>();
        private List<string> _actionNames = new List<string>();
        private List<FilterOperator> _operators;
        private Dictionary<string, List<string>> _actionAttributesCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public int RowIndex { get; set; }

        public event EventHandler<FilterConditionControl> RemoveButtonClicked;
        public event EventHandler ValueChanged;
        public event EventHandler<ActionAttributeRequestEventArgs> ActionAttributeRequested;

        // CORRECT constructor: (triggerAttributes, actionNames, operators)
        public FilterConditionControl(List<string> triggerAttributes, List<string> actionNames, List<FilterOperator> operators)
        {
            _triggerAttributes = triggerAttributes ?? new List<string>();
            _actionNames = actionNames ?? new List<string>();
            _operators = operators ?? new List<FilterOperator>();
            InitializeControl();
        }

        private void InitializeControl()
        {
            this.Height = 36;
            this.Dock = DockStyle.Top;
            this.Margin = new Padding(0, 2, 0, 2);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // Target
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));   // Action
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));    // Attribute
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));   // Operator
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));    // Value
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));    // Remove
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));    // Loading

            // Target
            cmbTarget = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbTarget.Items.AddRange(new object[] { FilterTarget.Trigger, FilterTarget.Action });
            cmbTarget.SelectedIndex = 0;
            cmbTarget.SelectedIndexChanged += CmbTarget_SelectedIndexChanged;
            layout.Controls.Add(cmbTarget, 0, 0);

            // Action Name
            cmbAction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Enabled = false };
            cmbAction.Visible = false;
            foreach (var name in _actionNames)
                cmbAction.Items.Add(name);
            if (cmbAction.Items.Count > 0)
                cmbAction.SelectedIndex = 0;
            cmbAction.SelectedIndexChanged += CmbAction_SelectedIndexChanged;
            layout.Controls.Add(cmbAction, 1, 0);

            // Attribute
            cmbAttribute = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Fill };
            RefreshTriggerAttributes();
            layout.Controls.Add(cmbAttribute, 2, 0);

            // Operator - FIX: use Items.Add NOT DataSource
            cmbOperator = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            foreach (var op in _operators)
                cmbOperator.Items.Add(op);
            if (cmbOperator.Items.Count > 0)
                cmbOperator.SelectedIndex = 0;
            layout.Controls.Add(cmbOperator, 3, 0);

            // Value
            txtValue = new TextBox { Dock = DockStyle.Fill };
            txtValue.TextChanged += (s, e) => ValueChanged?.Invoke(this, e);
            layout.Controls.Add(txtValue, 4, 0);

            // Remove
            btnRemove = new Button { Text = "✕", Width = 30, Height = 24, Anchor = AnchorStyles.None };
            btnRemove.Click += (s, e) => RemoveButtonClicked?.Invoke(this, this);
            layout.Controls.Add(btnRemove, 5, 0);

            // Loading label
            lblLoading = new Label { Text = "...", Dock = DockStyle.Fill, ForeColor = Color.DodgerBlue, Visible = false, TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(lblLoading, 6, 0);

            this.Controls.Add(layout);
        }

        private void CmbTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            var target = (FilterTarget)cmbTarget.SelectedItem;

            if (target == FilterTarget.Trigger)
            {
                cmbAction.Visible = false;
                cmbAction.Enabled = false;
                lblLoading.Visible = false;
                RefreshTriggerAttributes();
            }
            else
            {
                cmbAction.Visible = true;
                cmbAction.Enabled = true;
                cmbAttribute.DataSource = null;
                cmbAttribute.Items.Clear();
                cmbAttribute.Text = "";

                if (cmbAction.SelectedItem != null)
                {
                    LoadActionAttributes(cmbAction.SelectedItem.ToString());
                }
            }

            ValueChanged?.Invoke(this, e);
        }

        private void CmbAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTarget.SelectedItem is FilterTarget target && target == FilterTarget.Action)
            {
                var actionName = cmbAction.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(actionName))
                {
                    LoadActionAttributes(actionName);
                }
            }
            ValueChanged?.Invoke(this, e);
        }

        private void LoadActionAttributes(string actionName)
        {
            if (_actionAttributesCache.TryGetValue(actionName, out var cached))
            {
                cmbAttribute.DataSource = null;
                cmbAttribute.Items.Clear();
                foreach (var attr in cached)
                    cmbAttribute.Items.Add(attr);
                lblLoading.Visible = false;
                return;
            }

            cmbAttribute.DataSource = null;
            cmbAttribute.Items.Clear();
            cmbAttribute.Text = "";
            lblLoading.Text = "...";
            lblLoading.Visible = true;

            ActionAttributeRequested?.Invoke(this, new ActionAttributeRequestEventArgs(actionName));
        }

        public void SetActionAttributes(string actionName, List<string> attributes)
        {
            if (cmbAction.SelectedItem?.ToString() != actionName) return;

            _actionAttributesCache[actionName] = attributes ?? new List<string>();
            cmbAttribute.DataSource = null;
            cmbAttribute.Items.Clear();
            if (attributes != null)
            {
                foreach (var attr in attributes)
                    cmbAttribute.Items.Add(attr);
            }
            lblLoading.Visible = false;
        }

        public void RefreshTriggerAttributes(List<string> attributes = null)
        {
            if (attributes != null) _triggerAttributes = attributes;
            cmbAttribute.DataSource = null;
            cmbAttribute.Items.Clear();
            foreach (var attr in _triggerAttributes)
                cmbAttribute.Items.Add(attr);
        }

        public void RefreshActionNames(List<string> actionNames)
        {
            _actionNames = actionNames ?? new List<string>();
            var current = cmbAction.SelectedItem?.ToString();
            cmbAction.Items.Clear();
            foreach (var name in _actionNames)
                cmbAction.Items.Add(name);

            if (current != null)
            {
                for (int i = 0; i < cmbAction.Items.Count; i++)
                {
                    if (cmbAction.Items[i].ToString() == current)
                    {
                        cmbAction.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (cmbAction.Items.Count > 0)
            {
                cmbAction.SelectedIndex = 0;
            }
        }

        public FilterCondition FilterCondition
        {
            get
            {
                var target = cmbTarget.SelectedItem is FilterTarget ft ? ft : FilterTarget.Trigger;
                var op = cmbOperator.SelectedItem is FilterOperator fo ? fo : FilterOperator.Equals;

                return new FilterCondition
                {
                    Target = target,
                    ActionName = target == FilterTarget.Action ? cmbAction.SelectedItem?.ToString() : null,
                    Attribute = cmbAttribute.Text?.Trim() ?? string.Empty,
                    Operator = op,
                    Value = txtValue.Text ?? string.Empty
                };
            }
        }
    }
}