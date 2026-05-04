using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Helpers;
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

        // ===== PAGINATION STATE =====
        private List<FlowRun> _allFetchedRuns = new List<FlowRun>();   // Cache of all fetched runs
        private int _currentPage = 1;
        private int _pageSize = 100;
        private bool _hasMoreServerPages = false;      // Does server have more pages?
        private string _nextSkipToken = null;          // Token for next server page
        private Dictionary<string, string> _flowSkipTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // Per-flow tokens
        private bool _isLoadingPage = false;


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

            dataGridView1.CellClick += dataGridView1_CellClick;


            // ---- INIT PAGINATION UI ----
            if (btnPrev != null)
            {
                btnPrev.Enabled = false;
                btnPrev.Click += btnPrev_Click;
            }
            if (btnNext != null)
            {
                btnNext.Enabled = false;
                btnNext.Click += btnNext_Click;
            }
            if (lblPageInfo != null)
                lblPageInfo.Text = "Ready";

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
        // ==================== BUTTON: Fetch First Page ====================
        private void btnFetchHistory_Click_1(object sender, EventArgs e)
        {
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

            string selectedStatus = cmbStatus.SelectedItem?.ToString() ?? "All";

            // Reset pagination
            _allFetchedRuns.Clear();
            _currentPage = 1;
            _nextSkipToken = null;
            _hasMoreServerPages = false;
            _flowSkipTokens.Clear();

            // Fetch first page from server
            FetchPageFromServer(selectedFlows, fromDate, toDate, selectedStatus, isNextPage: false);

        }

        /// <summary>
        /// Fetches ONE page from the server (non-blocking UI)
        /// </summary>
        private void FetchPageFromServer(List<Flow> selectedFlows, DateTime fromDate, DateTime toDate,
            string selectedStatus, bool isNextPage = false)
        {
            if (_isLoadingPage) return;
            _isLoadingPage = true;

            string skipToken = isNextPage ? _nextSkipToken : null;

            WorkAsync(new WorkAsyncInfo
            {
                Message = isNextPage ? "Loading more results..." : $"Fetching history for {selectedFlows.Count} flow(s)...",
                Work = (worker, args) =>
                {
                    var client = CreateFlowClient();
                    var pageResult = new PageFetchResult
                    {
                        Runs = new List<FlowRun>(),
                        HasMore = false,
                        NextSkipToken = null
                    };

                    // For simplicity: fetch one page per flow. 
                    // For multi-flow, you may want to fetch from each flow and merge.
                    foreach (var flow in selectedFlows)
                    {
                        string flowSkipToken = isNextPage && _flowSkipTokens.ContainsKey(flow.Id)
                            ? _flowSkipTokens[flow.Id]
                            : null;

                        var result = client.GetFlowRuns(flow.Id, top: _pageSize, skipToken: flowSkipToken);

                        if (result.Runs == null || result.Runs.Count == 0) continue;

                        // Apply client-side filters (date, status)
                        var filtered = result.Runs
                            .Where(r => r.StartDate >= fromDate && r.StartDate <= toDate)
                            .ToList();

                        if (!string.Equals(selectedStatus, "All", StringComparison.OrdinalIgnoreCase))
                        {
                            filtered = filtered.Where(r => string.Equals(r.Status, selectedStatus, StringComparison.OrdinalIgnoreCase)).ToList();
                        }

                        foreach (var run in filtered)
                        {
                            run.FlowName = flow.DisplayName;
                            pageResult.Runs.Add(run);
                        }

                        // Track per-flow pagination
                        if (result.HasMore)
                            _flowSkipTokens[flow.Id] = result.NextSkipToken;
                        else
                            _flowSkipTokens.Remove(flow.Id);

                        // Global "has more" if ANY flow has more
                        if (result.HasMore) pageResult.HasMore = true;
                    }

                    args.Result = pageResult;
                },
                PostWorkCallBack = (args) =>
                {
                    _isLoadingPage = false;

                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UpdatePaginationButtons();
                        return;
                    }

                    var pageResult = (PageFetchResult)args.Result;

                    // Append to our cache
                    _allFetchedRuns.AddRange(pageResult.Runs);
                    _hasMoreServerPages = pageResult.HasMore;
                    _nextSkipToken = pageResult.NextSkipToken;

                    // Show current page
                    ShowPage(_currentPage);
                    UpdatePaginationButtons();
                }
            });
        }

        /// <summary>
        /// Displays the requested page from cached data
        /// </summary>
        private void ShowPage(int pageNumber)
        {
            int startIndex = (pageNumber - 1) * _pageSize;
            int count = Math.Min(_pageSize, _allFetchedRuns.Count - startIndex);

            List<FlowRun> pageRuns;
            if (startIndex >= _allFetchedRuns.Count)
                pageRuns = new List<FlowRun>();
            else
                pageRuns = _allFetchedRuns.Skip(startIndex).Take(count).ToList();

            BindDataGridView(pageRuns);
        }

        // ==================== FIXED: BindDataGridView ====================
        private void BindDataGridView(List<FlowRun> runs)
        {
            // IMPORTANT: AutoGenerateColumns = false MUST come BEFORE DataSource
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();

            if (runs == null || runs.Count == 0)
            {
                dataGridView1.DataSource = new List<FlowRun>();
                return;
            }

            // Now set DataSource — no auto-generated columns
            dataGridView1.DataSource = runs;

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FlowName",
                HeaderText = "Flow Name",
                Width = 200
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "Run ID",
                Width = 250
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 80
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "StartDate",
                HeaderText = "Start Time",
                Width = 130
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "EndDate",
                HeaderText = "End Time",
                Width = 130
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Duration",
                HeaderText = "Duration",
                Width = 80
            });

            // FIXED: Bind the Url to DataPropertyName so each row gets its own URL
            // UseColumnTextForLinkValue = false now, because we want the URL as the link text
            // OR keep true and handle click manually — see handler below
            var linkColumn = new DataGridViewLinkColumn
            {
                HeaderText = "Action",
                Text = "View Run",
                UseColumnTextForLinkValue = true,  // Shows "View Run" for all rows
                Name = "ViewRun",
                Width = 80,
                // Do NOT set DataPropertyName here — we handle click manually
            };
            dataGridView1.Columns.Add(linkColumn);
        }

        // ==================== FIXED: CellClick (more reliable than CellContentClick) ====================
        // REMOVE your old dataGridView1_CellContentClick handler from the Designer/Load
        // And use this CellClick handler instead — it fires on ANY click in the cell

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= dataGridView1.Columns.Count) return;

            // Check if the clicked column is our link column
            if (dataGridView1.Columns[e.ColumnIndex].Name != "ViewRun") return;

            var row = dataGridView1.Rows[e.RowIndex];
            if (row == null) return;

            var run = row.DataBoundItem as FlowRun;
            if (run == null)
            {
                MessageBox.Show("Could not get run data from row.");
                return;
            }

            if (string.IsNullOrEmpty(run.Url))
            {
                MessageBox.Show($"URL is empty for run: {run.Id}");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = run.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}\n\nURL: {run.Url}");
            }
        }

        private void UpdatePaginationButtons()
        {
            int totalCachedPages = (int)Math.Ceiling((double)_allFetchedRuns.Count / _pageSize);
            if (totalCachedPages == 0) totalCachedPages = 1;

            // Page info label: "Page 1/5 | Showing 100 of 450 | More available"
            string info = $"Page {_currentPage}/{totalCachedPages} | Total cached: {_allFetchedRuns.Count}";
            if (_hasMoreServerPages) info += " | More on server...";

            if (lblPageInfo != null) lblPageInfo.Text = info;

            // Prev: enabled if not on first page
            if (btnPrev != null) btnPrev.Enabled = _currentPage > 1;

            // Next: enabled if we have more cached pages OR server has more pages
            bool canGoNext = (_currentPage < totalCachedPages) || _hasMoreServerPages;
            if (btnNext != null) btnNext.Enabled = canGoNext && !_isLoadingPage;
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

        // ==================== PAGINATION BUTTONS ====================

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ShowPage(_currentPage);
                UpdatePaginationButtons();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            int totalCachedPages = (int)Math.Ceiling((double)_allFetchedRuns.Count / _pageSize);

            // Case 1: Next page is already in cache
            if (_currentPage < totalCachedPages)
            {
                _currentPage++;
                ShowPage(_currentPage);
                UpdatePaginationButtons();
            }
            // Case 2: We're on the last cached page but server has more
            else if (_currentPage >= totalCachedPages && _hasMoreServerPages && !_isLoadingPage)
            {
                _currentPage++;

                DateTime fromDate = dtpDateFrom.Value;
                DateTime toDate = dtpDateTo.Value;
                if (toDate.TimeOfDay == TimeSpan.Zero)
                    toDate = toDate.Date.AddDays(1).AddTicks(-1);
                string selectedStatus = cmbStatus.SelectedItem?.ToString() ?? "All";
                var selectedFlows = _currentFlows.Where(f => _checkedFlowIds.Contains(f.Id)).ToList();

                // Fetch next server page, then show it
                FetchPageFromServer(selectedFlows, fromDate, toDate, selectedStatus, isNextPage: true);
            }
        }
    }
}