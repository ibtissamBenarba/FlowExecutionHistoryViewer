using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using McTools.Xrm.Connection;
using Microsoft.Identity.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace ExecutionFlowHistoryViewer
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;
        private IPublicClientApplication _pca;
        private bool _isPowerAutomateConnected = false;

        // In-memory list of flows for the current solution
        private List<Flow> _currentFlows = new List<Flow>();
        // Tracks checked flow IDs independently of the visible list
        private readonly HashSet<string> _checkedFlowIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private class SolutionItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public MyPluginControl()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new System.Reflection.AssemblyName(args.Name);
            if (name.Name == "System.Diagnostics.DiagnosticSource")
            {
                string pluginPath = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                string assemblyPath = System.IO.Path.Combine(pluginPath, "System.Diagnostics.DiagnosticSource.dll");
                if (System.IO.File.Exists(assemblyPath))
                {
                    return System.Reflection.Assembly.LoadFrom(assemblyPath);
                }
            }
            return null;
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            clbFlows.CheckOnClick = true;
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository",
                new Uri("https://github.com/MscrmTools/XrmToolBox"));

            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();
                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }

            // ---- INIT STATUS FILTER ----
            cmbStatus.Items.Clear();
            cmbStatus.Items.AddRange(new object[] { "All", "Succeeded", "Failed", "Cancelled", "Running" });
            cmbStatus.SelectedIndex = 0;

            btnFetchHistory.Enabled = false;

            if (Service != null)
            {
                LoadSolutions();
            }
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsmConnectToPA_ItemClicked(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to Dataverse first!", "Not Connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExecuteMethod(ConnectToPowerAutomate);
        }

        private void ConnectToPowerAutomate()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Connecting to Power Automate...",
                Work = (worker, args) =>
                {
                    var client = CreateFlowClient();
                    args.Result = client;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Failed to connect to Power Automate:\n\n{args.Error.Message}",
                            "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        _isPowerAutomateConnected = false;
                        btnFetchHistory.Enabled = false;
                        return;
                    }

                    _isPowerAutomateConnected = true;
                    btnFetchHistory.Enabled = true;

                    MessageBox.Show("Successfully connected to Power Automate!\n\nYou can now fetch history without logging in again.",
                        "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        // ==================== BUTTON: Get Runs ====================
        private void btnFetchHistory_Click_1(object sender, EventArgs e)
        {
            // Use _checkedFlowIds so we include flows hidden by the search filter
            var selectedFlows = _currentFlows.Where(f => _checkedFlowIds.Contains(f.Id)).ToList();

            if (selectedFlows.Count == 0)
            {
                MessageBox.Show("Please check at least one flow from the list first!",
                    "No Flows Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_isPowerAutomateConnected)
            {
                MessageBox.Show("Please click 'Connect to Power Automate' first!",
                    "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ---- DATE FILTERING ----
            DateTime fromDate = dtpDateFrom.Value;
            DateTime toDate = dtpDateTo.Value;

            if (toDate.TimeOfDay == TimeSpan.Zero)
                toDate = toDate.Date.AddDays(1).AddTicks(-1);

            if (fromDate > toDate)
            {
                MessageBox.Show("The 'From' date must be earlier than or equal to the 'To' date.",
                    "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ---- STATUS FILTERING ----
            string selectedStatus = cmbStatus.SelectedItem?.ToString() ?? "All";

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Fetching history for {selectedFlows.Count} flow(s)...",
                Work = (worker, args) =>
                {
                    var client = CreateFlowClient();

                    var combinedResults = new DataTable();
                    combinedResults.Columns.Add("Flow Name", typeof(string));
                    combinedResults.Columns.Add("Run ID", typeof(string));
                    combinedResults.Columns.Add("Status", typeof(string));
                    combinedResults.Columns.Add("Start Time", typeof(string));
                    combinedResults.Columns.Add("End Time", typeof(string));

                    foreach (var flow in selectedFlows)
                    {
                        var runs = client.GetFlowRuns(flow.Id);
                        if (runs == null) continue;

                        var filteredRuns = runs.Where(r => r.StartDate >= fromDate && r.StartDate <= toDate).ToList();

                        if (!string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
                        {
                            filteredRuns = filteredRuns
                                .Where(r => string.Equals(r.Status, selectedStatus, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                        }

                        if (filteredRuns.Count == 0) continue;

                        foreach (var run in filteredRuns)
                        {
                            var row = combinedResults.NewRow();
                            row["Flow Name"] = flow.DisplayName;
                            row["Run ID"] = run.Id?.ToString() ?? "N/A";
                            row["Status"] = run.Status?.ToString() ?? "N/A";
                            row["Start Time"] = run.StartDate != default ? run.StartDate.ToString() : "N/A";
                            row["End Time"] = run.EndDate != default ? run.EndDate.ToString() : "N/A";
                            combinedResults.Rows.Add(row);
                        }
                    }

                    args.Result = combinedResults;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        if (args.Error.Message.Contains("401") || args.Error.Message.Contains("Unauthorized"))
                        {
                            _isPowerAutomateConnected = false;
                            btnFetchHistory.Enabled = false;
                            MessageBox.Show("Your Power Automate session expired. Please connect again.",
                                "Session Expired", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        return;
                    }

                    var dt = (DataTable)args.Result;
                    dataGridView1.DataSource = dt;
                    dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("No flow runs found matching the selected filters.",
                            "No Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            });
        }

        private void EnsurePcaInitialized()
        {
            if (_pca == null)
            {
                _pca = PublicClientApplicationBuilder.Create("51f81489-12ee-4a9e-aaae-a2591f45987d")
                    .WithAuthority($"https://login.microsoftonline.com/{ConnectionDetail.TenantId}")
                    .WithRedirectUri("app://58145B91-0C36-4500-8554-080854F2AC97")
                    .Build();
            }
        }

        private FlowClient CreateFlowClient()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var envId = ConnectionDetail.EnvironmentId.ToString();
            var scopes = new[] { "https://service.flow.microsoft.com/.default" };

            EnsurePcaInitialized();
            string token = GetAccessToken(_pca, scopes);

            string url = "https://api.flow.microsoft.com";
            return new FlowClient(envId, token, url);
        }

        private string GetAccessToken(IPublicClientApplication pca, string[] scopes)
        {
            try
            {
                var accounts = pca.GetAccountsAsync().GetAwaiter().GetResult();
                var account = accounts.FirstOrDefault(a => a.Username.Equals(ConnectionDetail.UserName, StringComparison.OrdinalIgnoreCase));

                if (account != null)
                {
                    return pca.AcquireTokenSilent(scopes, account)
                        .ExecuteAsync().GetAwaiter().GetResult().AccessToken;
                }
            }
            catch (MsalUiRequiredException) { }

            string interactiveToken = null;
            var task = Task.Run(async () =>
            {
                var result = await pca.AcquireTokenInteractive(scopes)
                    .WithLoginHint(ConnectionDetail.UserName)
                    .ExecuteAsync();
                interactiveToken = result.AccessToken;
            });

            task.Wait();
            return interactiveToken;
        }

        private void LoadSolutions()
        {
            if (Service == null) return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Solutions...",
                Work = (worker, args) =>
                {
                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true)
                            }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };

                    args.Result = Service.RetrieveMultiple(query);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var results = (EntityCollection)args.Result;
                    cbSolutions.Items.Clear();
                    cbSolutions.Items.Add(new SolutionItem { Id = Guid.Empty, Name = "-- All Solutions --" });

                    foreach (var entity in results.Entities)
                    {
                        cbSolutions.Items.Add(new SolutionItem
                        {
                            Id = entity.Id,
                            Name = entity.GetAttributeValue<string>("friendlyname")
                                    ?? entity.GetAttributeValue<string>("uniquename")
                                    ?? entity.Id.ToString()
                        });
                    }

                    if (cbSolutions.Items.Count > 0)
                        cbSolutions.SelectedIndex = 0;
                }
            });
        }

        private void LoadFlows(Guid? solutionId = null)
        {
            if (Service == null) return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = solutionId.HasValue && solutionId.Value != Guid.Empty
                    ? "Loading Flows for selected Solution..."
                    : "Loading Flows from Dataverse...",
                Work = (worker, args) =>
                {
                    var query = new QueryExpression("workflow")
                    {
                        ColumnSet = new ColumnSet("workflowid", "name"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("category", ConditionOperator.Equal, 5),
                                new ConditionExpression("type", ConditionOperator.Equal, 1)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };

                    if (solutionId.HasValue && solutionId.Value != Guid.Empty)
                    {
                        query.LinkEntities.Add(
                            new LinkEntity("workflow", "solutioncomponent", "workflowid", "objectid", JoinOperator.Inner)
                            {
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId.Value),
                                        new ConditionExpression("componenttype", ConditionOperator.Equal, 29)
                                    }
                                }
                            });
                    }

                    args.Result = Service.RetrieveMultiple(query);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var results = (EntityCollection)args.Result;

                    // Reset in-memory lists
                    _currentFlows = results.Entities
                        .Select(entity => new Flow
                        {
                            Id = entity.Id.ToString(),
                            DisplayName = entity.GetAttributeValue<string>("name")
                        })
                        .OrderBy(f => f.DisplayName)
                        .ToList();

                    _checkedFlowIds.Clear();

                    // Reset Select All checkbox without triggering its event
                    cbSelectAllFlows.CheckedChanged -= cbSelectAllFlows_CheckedChanged;
                    cbSelectAllFlows.Checked = false;
                    cbSelectAllFlows.CheckedChanged += cbSelectAllFlows_CheckedChanged;

                    // Apply search filter (if any) and populate the list
                    ApplyFlowFilter();
                }
            });
        }

        // ==================== SEARCH FILTER ====================
        private void ApplyFlowFilter()
        {
            string search = tbSearch.Text?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(search)
                ? _currentFlows
                : _currentFlows.Where(f =>
                    f.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Detach ItemCheck while repopulating to avoid double-tracking
            clbFlows.ItemCheck -= clbFlows_ItemCheck;
            clbFlows.Items.Clear();

            foreach (var flow in filtered)
            {
                clbFlows.Items.Add(flow, _checkedFlowIds.Contains(flow.Id));
            }

            clbFlows.ItemCheck += clbFlows_ItemCheck;
        }

        // ==================== SELECT ALL CHECKBOX ====================
        private void cbSelectAllFlows_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSelectAllFlows.Checked)
            {
                foreach (var flow in _currentFlows)
                    _checkedFlowIds.Add(flow.Id);
            }
            else
            {
                _checkedFlowIds.Clear();
            }

            ApplyFlowFilter();
        }

        // ==================== EVENTS ====================
        private void cbSolutions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSolutions.SelectedItem == null) return;

            var selectedSolution = (SolutionItem)cbSolutions.SelectedItem;

            if (selectedSolution.Id == Guid.Empty)
                LoadFlows();
            else
                LoadFlows(selectedSolution.Id);
        }

        private void clbFlows_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void clbFlows_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= clbFlows.Items.Count) return;

            var flow = clbFlows.Items[e.Index] as Flow;
            if (flow == null) return;

            if (e.NewValue == CheckState.Checked)
                _checkedFlowIds.Add(flow.Id);
            else
                _checkedFlowIds.Remove(flow.Id);
        }

        private void tbSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFlowFilter();
        }

        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            _pca = null;

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }

            LoadSolutions();
        }

        private void dtpDateFrom_ValueChanged(object sender, EventArgs e)
        {
        }

        private void dtpDateTo_ValueChanged(object sender, EventArgs e)
        {
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void datagridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }
    }
}