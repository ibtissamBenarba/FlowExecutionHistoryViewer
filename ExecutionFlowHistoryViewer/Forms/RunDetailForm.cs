using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using Newtonsoft.Json;
using System;
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

        private TextBox _txtGeneral;
        private TextBox _txtTrigger;
        private TreeView _treeActions;
        private TextBox _txtError;

        public RunDetailForm(FlowRun run, FlowRunDetailDto detail, FlowActionsResponseDto actions, IFlowClient flowClient)
        {
            _run = run;
            _detail = detail;
            _actions = actions;
            _flowClient = flowClient;
            InitializeComponent();
            BuildUi();
            LoadData();
        }

        private void BuildUi()
        {
            this.Text = $"Run Details - {_run.FlowName}";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // Tab General
            var tabGeneral = new TabPage("General");
            _txtGeneral = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10)
            };
            tabGeneral.Controls.Add(_txtGeneral);

            // Tab Trigger
            var tabTrigger = new TabPage("Trigger");
            _txtTrigger = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10)
            };
            tabTrigger.Controls.Add(_txtTrigger);

            // Tab Actions
            var tabActions = new TabPage("Actions");
            _treeActions = new TreeView { Dock = DockStyle.Fill };
            tabActions.Controls.Add(_treeActions);

            // Tab Error
            var tabError = new TabPage("Error");
            _txtError = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                ForeColor = Color.Red,
                Font = new Font("Consolas", 10)
            };
            tabError.Controls.Add(_txtError);

            tabControl.TabPages.Add(tabGeneral);
            tabControl.TabPages.Add(tabTrigger);
            tabControl.TabPages.Add(tabActions);
            tabControl.TabPages.Add(tabError);

            this.Controls.Add(tabControl);
        }

        private void LoadData()
        {
            if (_detail?.Properties != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Run ID: {_run.Id}");
                sb.AppendLine($"Flow: {_run.FlowName}");
                sb.AppendLine($"Status: {_detail.Properties.Status}");
                sb.AppendLine($"Start: {_detail.Properties.StartTime}");
                sb.AppendLine($"End: {_detail.Properties.EndTime}");
                sb.AppendLine($"Duration: {_run.Duration}");
                sb.AppendLine($"Tracking ID: {_detail.Properties.CorrelationClientTrackingId}");
                _txtGeneral.Text = sb.ToString();
            }

            // --- Trigger ---
            if (_detail?.Properties?.Trigger != null)
            {
                var trigger = _detail.Properties.Trigger;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Trigger Name: {trigger.Name}");
                sb.AppendLine();

                // ← CORRECTION : Récupérer les données depuis les liens
                string inputs = null;
                string outputs = null;

                if (trigger.InputsLink?.Uri != null)
                {
                    sb.AppendLine("--- Inputs (loading from link) ---");
                    inputs = GetTriggerContent(trigger.InputsLink.Uri);
                }
                else
                {
                    sb.AppendLine("--- Inputs ---");
                    inputs = FormatJson(trigger.Inputs);
                }

                if (trigger.OutputsLink?.Uri != null)
                {
                    sb.AppendLine("--- Outputs (loading from link) ---");
                    outputs = GetTriggerContent(trigger.OutputsLink.Uri);
                }
                else
                {
                    sb.AppendLine("--- Outputs ---");
                    outputs = FormatJson(trigger.Outputs);
                }

                sb.AppendLine(inputs ?? "null");
                sb.AppendLine();
                sb.AppendLine(outputs ?? "null");

                _txtTrigger.Text = sb.ToString();
            }
            else
            {
                _txtTrigger.Text = "No trigger data available.";
            }

            // --- Actions ---
            if (_actions?.Value != null)
            {
                foreach (var action in _actions.Value)
                {
                    var node = new TreeNode($"{action.Name} ({action.Type}) - {action.Properties?.Status}")
                    {
                        ForeColor = GetStatusColor(action.Properties?.Status)
                    };

                    if (action.Properties?.Inputs != null)
                    {
                        var inputsNode = new TreeNode("Inputs");
                        inputsNode.Nodes.Add(new TreeNode(FormatJson(action.Properties.Inputs)));
                        node.Nodes.Add(inputsNode);
                    }

                    if (action.Properties?.Outputs != null)
                    {
                        var outputsNode = new TreeNode("Outputs");
                        outputsNode.Nodes.Add(new TreeNode(FormatJson(action.Properties.Outputs)));
                        node.Nodes.Add(outputsNode);
                    }

                    if (action.Properties?.Error != null)
                    {
                        var errorNode = new TreeNode($"Error: {action.Properties.Error.Message}")
                        {
                            ForeColor = Color.Red
                        };
                        node.Nodes.Add(errorNode);
                    }

                    _treeActions.Nodes.Add(node);
                }
                _treeActions.ExpandAll();
            }

            // --- Error ---
            if (_actions?.Value != null)
            {
                var failedActions = _actions.Value
                    .Where(a => a.Properties?.Error != null)
                    .ToList();

                if (failedActions.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var action in failedActions)
                    {
                        sb.AppendLine($"Action: {action.Name}");
                        sb.AppendLine($"Code: {action.Properties.Error.Code}");
                        sb.AppendLine($"Message: {action.Properties.Error.Message}");
                        sb.AppendLine(new string('-', 50));
                    }
                    _txtError.Text = sb.ToString();
                }
                else
                {
                    _txtError.Text = "No errors found.";
                }
            }
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
                return Color.Green;
            if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
                return Color.Red;
            if (string.Equals(status, "Skipped", StringComparison.OrdinalIgnoreCase))
                return Color.Gray;
            return Color.Black;
        }
    }
}