using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using McTools.Xrm.Connection;
using Microsoft.Identity.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

                    var allRuns = new List<FlowRun>();

                    foreach (var flow in selectedFlows)
                    {
                        var runs = client.GetFlowRuns(flow.Id);
                        if (runs == null) continue;

                        var filteredRuns = runs
                            .Where(r => r.StartDate >= fromDate && r.StartDate <= toDate)
                            .ToList();

                        if (!string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
                        {
                            filteredRuns = filteredRuns
                                .Where(r => string.Equals(r.Status, selectedStatus, StringComparison.OrdinalIgnoreCase))
                                .ToList();
                        }

                        foreach (var run in filteredRuns)
                        {
                            allRuns.Add(new FlowRun
                            {
                                FlowName = flow.DisplayName,
                                Id = run.Id,
                                Status = run.Status,
                                StartDate = run.StartDate,
                                EndDate = run.EndDate,
                                Url = run.Url   // 👈 IMPORTANT
                            });
                        }
                    }

                    args.Result = allRuns;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var list = (List<FlowRun>)args.Result;

                    dataGridView1.DataSource = list;

                    dataGridView1.AutoGenerateColumns = false;
                    dataGridView1.Columns.Clear();

                    // Add normal columns
                    dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        DataPropertyName = "FlowName",
                        HeaderText = "Flow Name"
                    });

                    dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        DataPropertyName = "Id",
                        HeaderText = "Run ID"
                    });

                    dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        DataPropertyName = "Status",
                        HeaderText = "Status"
                    });

                    dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        DataPropertyName = "StartDate",
                        HeaderText = "Start Time"
                    });

                    dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        DataPropertyName = "EndDate",
                        HeaderText = "End Time"
                    });

                    // ⭐ THIS IS THE LINK COLUMN
                    var linkColumn = new DataGridViewLinkColumn
                    {
                        HeaderText = "Action",
                        Text = "View Run",
                        UseColumnTextForLinkValue = true,
                        Name = "ViewRun"
                    };

                    dataGridView1.Columns.Add(linkColumn);

                    if (list.Count == 0)
                    {
                        MessageBox.Show("No flow runs found.",
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

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridView1.Columns[e.ColumnIndex].Name == "ViewRun")
            {
                var run = (FlowRun)dataGridView1.Rows[e.RowIndex].DataBoundItem;

                if (!string.IsNullOrEmpty(run.Url))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = run.Url,
                        UseShellExecute = true
                    });
                }
            }
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

        private List<FlowRun> GetCurrentHistoryList()
        {
            // Since we assigned a List<FlowRun> to the DataSource, 
            // we can just cast it back.
            if (dataGridView1.DataSource is List<FlowRun> list)
            {
                return list;
            }

            // Fallback if the list is null
            return new List<FlowRun>();
        }
        private void btnExport_Click_1(object sender, EventArgs e)
        {
            // 1. Transformer la DataTable ou la liste en List<FlowRun>
            // Ici, je suppose que vous avez une liste de vos objets
            var history = GetCurrentHistoryList();

            if (history == null || history.Count == 0) return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string ext = Path.GetExtension(sfd.FileName).ToLower();

                    if (ext == ".xlsx")
                        ExcelService.Export(history, sfd.FileName);
                    else
                        CsvService.Export(history, sfd.FileName);

                    MessageBox.Show("Exportation réussie !");
                }
            }
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


    }
}