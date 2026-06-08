using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class FilterConditionControl : UserControl
    {
        private List<string> _attributes;
        private List<TriggerOutputOperator> _operators;

        public int RowIndex { get; set; }

        public event EventHandler<FilterConditionControl> RemoveButtonClicked;
        public event EventHandler ValueChanged;  // NEW

        public FilterCondition FilterCondition
        {
            get
            {
                // Safe attribute: null if nothing selected
                string attribute = cbAttribute?.SelectedItem?.ToString();

                // Safe operator: default to Equals if nothing selected
                TriggerOutputOperator op = TriggerOutputOperator.Equals;
                if (cbOperator?.SelectedItem != null && cbOperator.SelectedItem is TriggerOutputOperator selectedOp)
                {
                    op = selectedOp;
                }

                // Safe value: empty string if null
                string value = txtValue?.Text ?? string.Empty;

                return new FilterCondition
                {
                    Attribute = attribute,
                    Operator = op,
                    Value = value
                };
            }
        }

        private ComboBox cbAttribute;
        private ComboBox cbOperator;
        private TextBox txtValue;
        private Button btnRemove;

        public FilterConditionControl(List<string> attributes, List<TriggerOutputOperator> operators, Image deleteImage = null)
        {
            _attributes = attributes ?? new List<string>();
            _operators = operators ?? Enum.GetValues(typeof(TriggerOutputOperator)).Cast < TriggerOutputOperator > ().ToList();
            InitializeControl(deleteImage);
        }

        public void RefreshAttributes(List<string> attributes)
        {
            _attributes = attributes ?? new List<string>();
            string previous = cbAttribute?.SelectedItem?.ToString();

            cbAttribute.DataSource = null;
            cbAttribute.Items.Clear();
            cbAttribute.Items.AddRange(_attributes.ToArray());

            if (!string.IsNullOrEmpty(previous) && _attributes.Contains(previous))
                cbAttribute.SelectedItem = previous;
            else if (_attributes.Count > 0)
                cbAttribute.SelectedIndex = 0;
            else
                cbAttribute.SelectedIndex = -1;  // No items available
        }

        private void InitializeControl(Image deleteImage)
        {
            this.Height = 34;
            this.Margin = new Padding(0, 2, 0, 2);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));

            // --- Attribute ComboBox ---
            cbAttribute = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3)
            };
            cbAttribute.Items.AddRange(_attributes.ToArray());
            if (_attributes.Count > 0) cbAttribute.SelectedIndex = 0;
            cbAttribute.SelectedIndexChanged += (s, e) => ValueChanged?.Invoke(this, EventArgs.Empty);

            // --- Operator ComboBox ---
            cbOperator = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3)
            };

            // Use Items.AddRange instead of DataSource to avoid binding issues
            cbOperator.Items.AddRange(_operators.Cast<object>().ToArray());
            if (_operators.Count > 0) cbOperator.SelectedIndex = 0;  // CRITICAL: ensure selection

            cbOperator.SelectedIndexChanged += (s, e) => ValueChanged?.Invoke(this, EventArgs.Empty);

            // --- Value TextBox ---
            txtValue = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Text = string.Empty  // Ensure not null
            };
            txtValue.TextChanged += (s, e) => ValueChanged?.Invoke(this, EventArgs.Empty);

            // --- Remove Button ---
            btnRemove = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Text = "X",
                ForeColor = Color.Red,
                FlatStyle = FlatStyle.Flat
            };
            if (deleteImage != null)
            {
                btnRemove.Text = "";
                btnRemove.Image = deleteImage;
                btnRemove.ImageAlign = ContentAlignment.MiddleCenter;
            }
            btnRemove.Click += (s, e) => RemoveButtonClicked?.Invoke(this, this);

            layout.Controls.Add(cbAttribute, 0, 0);
            layout.Controls.Add(cbOperator, 1, 0);
            layout.Controls.Add(txtValue, 2, 0);
            layout.Controls.Add(btnRemove, 3, 0);

            this.Controls.Add(layout);
        }
    }
}