// MyPluginControl.cs
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Forms;
using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using XrmToolBox.Extensibility;

namespace ExecutionFlowHistoryViewer
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings _settings;
        private IAuthenticationService _authService;
        private IDataverseService _dataverseService;
        private IFlowClientFactory _flowClientFactory;
        private IFlowHistoryService _historyService;
        private IPaginationService _pagination;

        // Flow selection state
        private List<Flow> _currentFlows = new List<Flow>();
        private readonly HashSet<string> _checkedFlowIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _flowSkipTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Deep search state
        private bool _isDeepSearchActive;
        private BackgroundWorker _deepSearchWorker;

        public MyPluginControl()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        #region Initialization

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            clbFlows.CheckOnClick = true;
            InitializeFilters();
            InitializePagination();
            InitializeSettings();
            WireEvents();

            if (Service != null) InitializeServices();
        }

        private void InitializeFilters()
        {
            cmbStatus.Items.Clear();
            cmbStatus.Items.AddRange(new object[] { "All", "Succeeded", "Failed", "Cancelled", "Running" });
            cmbStatus.SelectedIndex = 0;
        }

        private void InitializePagination()
        {
            // -= avant += pour éviter les doublons
            if (btnPrev != null)
            {
                btnPrev.Enabled = false;
                btnPrev.Click -= btnPrev_Click;
                btnPrev.Click += btnPrev_Click;
            }
            if (btnNext != null)
            {
                btnNext.Enabled = false;
                btnNext.Click -= btnNext_Click;
                btnNext.Click += btnNext_Click;
            }
            if (lblPageInfo != null) lblPageInfo.Text = "Ready";
        }

        private void InitializeSettings()
        {
            if (!SettingsManager.Instance.TryLoad(GetType(), out _settings))
            {
                _settings = new Settings();
                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        private void WireEvents()
        {
            // -= avant += pour éviter les doublons
            dataGridView1.CellClick -= dataGridView1_CellClick;
            dataGridView1.CellClick += dataGridView1_CellClick;

            cbSolutions.SelectedIndexChanged -= cbSolutions_SelectedIndexChanged;
            cbSolutions.SelectedIndexChanged += cbSolutions_SelectedIndexChanged;

            clbFlows.ItemCheck -= clbFlows_ItemCheck;
            clbFlows.ItemCheck += clbFlows_ItemCheck;

            clbFlows.MouseDown -= clbFlows_MouseDown;
            clbFlows.MouseDown += clbFlows_MouseDown;

            tbSearch.TextChanged -= tbSearch_TextChanged;
            tbSearch.TextChanged += tbSearch_TextChanged;

            cbSelectAllFlows.CheckedChanged -= cbSelectAllFlows_CheckedChanged;
            cbSelectAllFlows.CheckedChanged += cbSelectAllFlows_CheckedChanged;

            btnDeepSearch.Click -= btnDeepSearch_Click;
            btnDeepSearch.Click += btnDeepSearch_Click;

            btnClearDeepSearch.Click -= btnClearDeepSearch_Click;
            btnClearDeepSearch.Click += btnClearDeepSearch_Click;

            tbDeepSearch.KeyDown -= tbDeepSearch_KeyDown;
            tbDeepSearch.KeyDown += tbDeepSearch_KeyDown;
        }

        private void InitializeServices()
        {
            _authService = new AuthenticationService(ConnectionDetail);
            _dataverseService = new DataverseService(Service);
            _flowClientFactory = new FlowClientFactory(_authService, ConnectionDetail.EnvironmentId.ToString());
            _historyService = new FlowHistoryService(_flowClientFactory);
            _pagination = new PaginationService();
        }

        #endregion

        #region Connection & Services

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            _authService?.Reset();
            InitializeServices();
            LoadSolutions();
        }

        private void tsmConnectToPA_ItemClicked(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to Dataverse first!", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ExecuteMethod(ConnectToPowerAutomate);
        }

        private void ConnectToPowerAutomate()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Connecting to Power Automate...",
                Work = (worker, args) => args.Result = _flowClientFactory.Create(),
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Failed to connect:\n\n{args.Error.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnFetchHistory.Enabled = false;
                        return;
                    }
                    btnFetchHistory.Enabled = true;
                    MessageBox.Show("Successfully connected to Power Automate!", "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        #endregion

        #region Data Loading

        private void LoadSolutions()
        {
            if (_dataverseService == null) return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Solutions...",
                Work = (worker, args) => args.Result = _dataverseService.GetSolutions(),
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null) { ShowError(args.Error); return; }

                    cbSolutions.Items.Clear();
                    cbSolutions.Items.Add(new SolutionItem { Id = Guid.Empty, Name = "-- All Solutions --" });

                    foreach (var sol in (List<SolutionItem>)args.Result)
                        cbSolutions.Items.Add(sol);

                    cbSolutions.SelectedIndex = 0;
                }
            });
        }

        private void LoadFlows(Guid? solutionId = null)
        {
            if (_dataverseService == null) return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = solutionId.HasValue && solutionId.Value != Guid.Empty
                    ? "Loading Flows for selected Solution..."
                    : "Loading Flows from Dataverse...",
                Work = (worker, args) => args.Result = _dataverseService.GetFlows(solutionId),
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null) { ShowError(args.Error); return; }

                    _currentFlows = (List<Flow>)args.Result;
                    _checkedFlowIds.Clear();
                    cbSelectAllFlows.Checked = false;
                    ApplyFlowFilter();
                }
            });
        }

        #endregion

        #region Fetch History

        private void btnFetchHistory_Click_1(object sender, EventArgs e)
        {
            var selectedFlows = GetSelectedFlows();
            if (!ValidateFetch(selectedFlows)) return;

            var (fromDate, toDate, status) = GetFilterValues();
            ResetPagination();
            FetchPage(selectedFlows, fromDate, toDate, status, isNextPage: false);
        }

        private void FetchPage(List<Flow> flows, DateTime fromDate, DateTime toDate, string status, bool isNextPage)
        {
            if (_pagination.IsLoading) return;
            _pagination.IsLoading = true;

            WorkAsync(new WorkAsyncInfo
            {
                Message = isNextPage ? "Loading more results..." : $"Fetching history for {flows.Count} flow(s)...",
                Work = (worker, args) => args.Result = _historyService.FetchRuns(
                    flows, fromDate, toDate, status, isNextPage, _flowSkipTokens, _pagination.PageSize),
                PostWorkCallBack = (args) =>
                {
                    _pagination.IsLoading = false;
                    if (args.Error != null) { ShowError(args.Error); UpdatePaginationUI(); return; }

                    var result = (FlowRunPageResult)args.Result;
                    _pagination.AppendRuns(result.Runs);
                    _pagination.HasMoreServerPages = result.HasMore;

                    // CORRECTION : Si c'est un fetch "Next", on avance la page APRÈS avoir reçu les données
                    if (isNextPage)
                    {
                        _pagination.CurrentPage++;
                    }

                    ShowCurrentPage();
                    UpdatePaginationUI();
                }
            });
        }

        #endregion

        #region Pagination UI

        private void ShowCurrentPage()
        {
            DataGridBinder.BindFlowRuns(dataGridView1, _pagination.GetCurrentPage());
        }

        private void UpdatePaginationUI()
        {
            lblPageInfo.Text = _pagination.GetPageInfoText();
            btnPrev.Enabled = _pagination.CanGoPrevious() && !_pagination.IsLoading;
            btnNext.Enabled = _pagination.CanGoNext() && !_pagination.IsLoading;
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            // CORRECTION : Protection contre double-clic et chargement
            if (!_pagination.CanGoPrevious() || _pagination.IsLoading) return;

            _pagination.CurrentPage--;
            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            // CORRECTION : Protection contre chargement
            if (_pagination.IsLoading) return;

            // Case 1: Next page is already in cache
            if (_pagination.CurrentPage < _pagination.TotalCachedPages)
            {
                _pagination.CurrentPage++;
                ShowCurrentPage();
                UpdatePaginationUI();
            }
            // Case 2: We're on the last cached page but server has more
            else if (_pagination.HasMoreServerPages)
            {
                // CORRECTION : On NE TOUCHE PAS à CurrentPage ici !
                // Elle sera incrémentée dans le callback de FetchPage
                var (fromDate, toDate, status) = GetFilterValues();
                FetchPage(GetSelectedFlows(), fromDate, toDate, status, isNextPage: true);
            }
        }

        #endregion

        #region Flow Selection & Filtering

        private List<Flow> GetSelectedFlows() =>
            _currentFlows.Where(f => _checkedFlowIds.Contains(f.Id)).ToList();

        private void ApplyFlowFilter()
        {
            string search = tbSearch.Text?.Trim() ?? string.Empty;
            var filtered = string.IsNullOrEmpty(search)
                ? _currentFlows
                : _currentFlows.Where(f => f.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            clbFlows.ItemCheck -= clbFlows_ItemCheck;
            clbFlows.Items.Clear();
            foreach (var flow in filtered)
                clbFlows.Items.Add(flow, _checkedFlowIds.Contains(flow.Id));
            clbFlows.ItemCheck += clbFlows_ItemCheck;
        }

        private void cbSelectAllFlows_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSelectAllFlows.Checked)
                foreach (var flow in _currentFlows) _checkedFlowIds.Add(flow.Id);
            else
                _checkedFlowIds.Clear();
            ApplyFlowFilter();
        }

        private void clbFlows_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= clbFlows.Items.Count) return;
            var flow = clbFlows.Items[e.Index] as Flow;
            if (flow == null) return;

            if (e.NewValue == CheckState.Checked) _checkedFlowIds.Add(flow.Id);
            else _checkedFlowIds.Remove(flow.Id);
        }

        private void cbSolutions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(cbSolutions.SelectedItem is SolutionItem selected)) return;
            LoadFlows(selected.Id == Guid.Empty ? (Guid?)null : selected.Id);
        }

        private void tbSearch_TextChanged(object sender, EventArgs e) => ApplyFlowFilter();

        private void clbFlows_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = clbFlows.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    clbFlows.SelectedIndex = index;
                    var flow = clbFlows.Items[index] as Flow;
                    if (flow != null)
                    {
                        var ctx = new ContextMenuStrip();
                        
                        var enableItem = new ToolStripMenuItem("Enable Flow", null, (s, ev) => ToggleFlowState(flow, true));
                        var disableItem = new ToolStripMenuItem("Disable Flow", null, (s, ev) => ToggleFlowState(flow, false));
                        var openBrowserItem = new ToolStripMenuItem("Open Flow in Browser", null, (s, ev) => OpenFlowInBrowser(flow));

                        ctx.Items.Add(enableItem);
                        ctx.Items.Add(disableItem);
                        ctx.Items.Add(openBrowserItem);

                        ctx.Show(clbFlows, e.Location);
                    }
                }
            }
        }

        private void ToggleFlowState(Flow flow, bool enable)
        {
            if (_dataverseService == null) return;
            WorkAsync(new WorkAsyncInfo
            {
                Message = $"{(enable ? "Enabling" : "Disabling")} Flow...",
                Work = (worker, args) =>
                {
                    _dataverseService.UpdateFlowState(flow.Id, enable);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error);
                        return;
                    }
                    MessageBox.Show($"Flow successfully {(enable ? "enabled" : "disabled")}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        private void OpenFlowInBrowser(Flow flow)
        {
            if (ConnectionDetail == null) return;
            string url = $"https://make.powerautomate.com/environments/{ConnectionDetail.EnvironmentId}/flows/{flow.Id}/details";
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}");
            }
        }

        #endregion

        #region Grid Interaction

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= dataGridView1.Columns.Count) return;

            var run = dataGridView1.Rows[e.RowIndex].DataBoundItem as FlowRun;
            if (run == null) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            if (colName == "ViewRun")
            {
                if (string.IsNullOrEmpty(run.Url))
                {
                    MessageBox.Show("URL is empty for this run.");
                    return;
                }
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = run.Url, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open URL: {ex.Message}");
                }
            }
            else if (colName == "ViewDetails")
            {
                ShowRunDetails(run);
            }
        }

        private void ShowRunDetails(FlowRun run)
        {
            var flow = _currentFlows.FirstOrDefault(f => f.DisplayName == run.FlowName);
            if (flow == null)
            {
                MessageBox.Show("Could not determine the flow for this run.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading run details...",
                Work = (worker, args) =>
                {
                    var client = _flowClientFactory.Create();
                    var detail = client.GetRunDetails(flow.Id, run.Id);
                    var actions = client.GetRunActions(flow.Id, run.Id);

                    // ← Passer le client aussi
                    args.Result = new Tuple<FlowRunDetailDto, FlowActionsResponseDto, IFlowClient>(detail, actions, client);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error);
                        return;
                    }

                    var tuple = (Tuple<FlowRunDetailDto, FlowActionsResponseDto, IFlowClient>)args.Result;
                    var detail = tuple.Item1;
                    var actions = tuple.Item2;
                    var client = tuple.Item3;

                    using (var form = new RunDetailForm(run, detail, actions, client))  // ← Passer client
                    {
                        form.ShowDialog(this);
                    }
                }
            });
        }

        private void tsbCompareRuns_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count != 2)
            {
                MessageBox.Show("Please select exactly two flow runs in the table to compare them side-by-side.",
                    "Select Two Runs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRows = dataGridView1.SelectedRows;
            var run1 = selectedRows[0].DataBoundItem as FlowRun;
            var run2 = selectedRows[1].DataBoundItem as FlowRun;

            if (run1 == null || run2 == null) return;

            var flow1 = _currentFlows.FirstOrDefault(f => f.DisplayName == run1.FlowName);
            var flow2 = _currentFlows.FirstOrDefault(f => f.DisplayName == run2.FlowName);

            if (flow1 == null || flow2 == null)
            {
                MessageBox.Show("Could not determine the flows for the selected runs.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Fetching details and preparing side-by-side comparison...",
                Work = (worker, args) =>
                {
                    var client = _flowClientFactory.Create();

                    // Fetch details and actions for Run 1
                    var detail1 = client.GetRunDetails(flow1.Id, run1.Id);
                    var actions1 = client.GetRunActions(flow1.Id, run1.Id);

                    // Fetch details and actions for Run 2
                    var detail2 = client.GetRunDetails(flow2.Id, run2.Id);
                    var actions2 = client.GetRunActions(flow2.Id, run2.Id);

                    args.Result = new Tuple<FlowRunDetailDto, FlowActionsResponseDto, FlowRunDetailDto, FlowActionsResponseDto, IFlowClient>(
                        detail1, actions1, detail2, actions2, client);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error);
                        return;
                    }

                    var tuple = (Tuple<FlowRunDetailDto, FlowActionsResponseDto, FlowRunDetailDto, FlowActionsResponseDto, IFlowClient>)args.Result;
                    var detail1 = tuple.Item1;
                    var actions1 = tuple.Item2;
                    var detail2 = tuple.Item3;
                    var actions2 = tuple.Item4;
                    var client = tuple.Item5;

                    using (var form = new CompareRunsForm(run1, detail1, actions1, run2, detail2, actions2, client))
                    {
                        form.ShowDialog(this);
                    }
                }
            });
        }

        #endregion

        #region Deep Search

        private void tbDeepSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnDeepSearch_Click(sender, e);
            }
        }

        private void btnDeepSearch_Click(object sender, EventArgs e)
        {
            string searchValue = tbDeepSearch.Text?.Trim();
            if (string.IsNullOrEmpty(searchValue))
            {
                MessageBox.Show("Please enter a value to search for in the run details.",
                    "Search Value Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbDeepSearch.Focus();
                return;
            }

            var allRuns = _pagination.AllRuns;
            if (allRuns == null || allRuns.Count == 0)
            {
                MessageBox.Show("No run history loaded. Please fetch run history first.",
                    "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Cancel any existing search
            if (_deepSearchWorker != null && _deepSearchWorker.IsBusy)
            {
                _deepSearchWorker.CancelAsync();
                return;
            }

            PerformDeepSearch(allRuns.ToList(), searchValue);
        }

        private void btnClearDeepSearch_Click(object sender, EventArgs e)
        {
            // Cancel any running search
            if (_deepSearchWorker != null && _deepSearchWorker.IsBusy)
            {
                _deepSearchWorker.CancelAsync();
            }

            _isDeepSearchActive = false;
            tbDeepSearch.Text = "";
            lblDeepSearchStatus.Text = "";
            progressBarDeepSearch.Visible = false;
            gbFlowRuns.Text = "Flow Runs";

            // Restore original paginated view
            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void PerformDeepSearch(List<FlowRun> runsToSearch, string searchValue)
        {
            // Setup UI for search in progress
            progressBarDeepSearch.Visible = true;
            progressBarDeepSearch.Minimum = 0;
            progressBarDeepSearch.Maximum = runsToSearch.Count;
            progressBarDeepSearch.Value = 0;
            lblDeepSearchStatus.Text = $"Scanning 0/{runsToSearch.Count}...";
            btnDeepSearch.Text = "❌ Cancel";
            btnDeepSearch.Enabled = true;
            btnClearDeepSearch.Enabled = false;
            btnFetchHistory.Enabled = false;

            _deepSearchWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _deepSearchWorker.DoWork += (s, args) =>
            {
                var matchingRuns = new List<FlowRun>();
                var client = _flowClientFactory.Create();
                int processed = 0;

                foreach (var run in runsToSearch)
                {
                    if (_deepSearchWorker.CancellationPending)
                    {
                        args.Cancel = true;
                        return;
                    }

                    try
                    {
                        // Find the flow for this run
                        var flow = _currentFlows.FirstOrDefault(f => f.DisplayName == run.FlowName);
                        if (flow == null)
                        {
                            processed++;
                            _deepSearchWorker.ReportProgress(processed);
                            continue;
                        }

                        bool matched = false;

                        // 1) Check run details (trigger inputs/outputs)
                        var detail = client.GetRunDetails(flow.Id, run.Id);
                        if (detail?.Properties?.Trigger != null)
                        {
                            var trigger = detail.Properties.Trigger;

                            // Check inputs
                            string inputs = null;
                            if (trigger.InputsLink?.Uri != null)
                            {
                                try { inputs = client.GetContentFromLink(trigger.InputsLink.Uri); }
                                catch { /* skip */ }
                            }
                            else if (trigger.Inputs != null)
                            {
                                inputs = JsonConvert.SerializeObject(trigger.Inputs);
                            }

                            if (inputs != null && inputs.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                matched = true;

                            // Check outputs
                            if (!matched)
                            {
                                string outputs = null;
                                if (trigger.OutputsLink?.Uri != null)
                                {
                                    try { outputs = client.GetContentFromLink(trigger.OutputsLink.Uri); }
                                    catch { /* skip */ }
                                }
                                else if (trigger.Outputs != null)
                                {
                                    outputs = JsonConvert.SerializeObject(trigger.Outputs);
                                }

                                if (outputs != null && outputs.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                    matched = true;
                            }
                        }

                        // 2) Check actions (inputs/outputs/errors)
                        if (!matched)
                        {
                            var actions = client.GetRunActions(flow.Id, run.Id);
                            if (actions?.Value != null)
                            {
                                foreach (var action in actions.Value)
                                {
                                    if (action.Properties?.Inputs != null)
                                    {
                                        string actionInputs = JsonConvert.SerializeObject(action.Properties.Inputs);
                                        if (actionInputs.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            matched = true;
                                            break;
                                        }
                                    }
                                    if (action.Properties?.Outputs != null)
                                    {
                                        string actionOutputs = JsonConvert.SerializeObject(action.Properties.Outputs);
                                        if (actionOutputs.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            matched = true;
                                            break;
                                        }
                                    }
                                    if (action.Properties?.Error != null)
                                    {
                                        string errorStr = $"{action.Properties.Error.Code} {action.Properties.Error.Message}";
                                        if (errorStr.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            matched = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (matched)
                            matchingRuns.Add(run);
                    }
                    catch
                    {
                        // Skip runs that fail to load — don't crash the search
                    }

                    processed++;
                    _deepSearchWorker.ReportProgress(processed);
                }

                args.Result = matchingRuns;
            };

            _deepSearchWorker.ProgressChanged += (s, args) =>
            {
                if (args.ProgressPercentage <= progressBarDeepSearch.Maximum)
                    progressBarDeepSearch.Value = args.ProgressPercentage;
                lblDeepSearchStatus.Text = $"Scanning {args.ProgressPercentage}/{runsToSearch.Count}...";
            };

            _deepSearchWorker.RunWorkerCompleted += (s, args) =>
            {
                btnDeepSearch.Text = "🔍 Search";
                btnDeepSearch.Enabled = true;
                btnClearDeepSearch.Enabled = true;
                btnFetchHistory.Enabled = true;
                progressBarDeepSearch.Visible = false;

                if (args.Cancelled)
                {
                    lblDeepSearchStatus.Text = "Search cancelled.";
                    return;
                }

                if (args.Error != null)
                {
                    lblDeepSearchStatus.Text = "Search failed.";
                    MessageBox.Show($"Deep search error:\n{args.Error.Message}",
                        "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var matchingRuns = args.Result as List<FlowRun>;
                if (matchingRuns == null || matchingRuns.Count == 0)
                {
                    lblDeepSearchStatus.Text = $"No matches found for \"{searchValue}\".";
                    gbFlowRuns.Text = $"Flow Runs — 0 results for \"{searchValue}\"";
                    DataGridBinder.BindFlowRuns(dataGridView1, new List<FlowRun>());
                    _isDeepSearchActive = true;
                    return;
                }

                _isDeepSearchActive = true;
                lblDeepSearchStatus.Text = $"✔ {matchingRuns.Count} match(es) found!";
                gbFlowRuns.Text = $"Flow Runs — {matchingRuns.Count} result(s) for \"{searchValue}\"";

                // Hide pagination controls during deep search result view
                btnPrev.Enabled = false;
                btnNext.Enabled = false;
                lblPageInfo.Text = $"Showing {matchingRuns.Count} filtered result(s)";

                DataGridBinder.BindFlowRuns(dataGridView1, matchingRuns);
            };

            _deepSearchWorker.RunWorkerAsync();
        }

        #endregion

        #region Export

        private void btnExport_Click_1(object sender, EventArgs e)
        {
            var history = dataGridView1.DataSource as List<FlowRun>;
            if (history == null || history.Count == 0) return;

            using (var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv" })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                string ext = Path.GetExtension(sfd.FileName).ToLower();
                if (ext == ".xlsx")
                    ExcelService.Export(history, sfd.FileName);
                else
                    CsvService.Export(history, sfd.FileName);

                MessageBox.Show("Export successful!");
            }
        }

        #endregion

        #region Helpers

        private (DateTime fromDate, DateTime toDate, string status) GetFilterValues()
        {
            DateTime fromDate = dtpDateFrom.Value;
            DateTime toDate = dtpDateTo.Value;
            if (toDate.TimeOfDay == TimeSpan.Zero)
                toDate = toDate.Date.AddDays(1).AddTicks(-1);

            string status = cmbStatus.SelectedItem?.ToString() ?? "All";
            return (fromDate, toDate, status);
        }

        private bool ValidateFetch(List<Flow> selectedFlows)
        {
            if (selectedFlows.Count == 0)
            {
                MessageBox.Show("Please check at least one flow!", "No Flows Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!btnFetchHistory.Enabled)
            {
                MessageBox.Show("Please connect to Power Automate first!", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var (fromDate, toDate, _) = GetFilterValues();
            if (fromDate > toDate)
            {
                MessageBox.Show("The 'From' date must be earlier than or equal to the 'To' date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void ResetPagination()
        {
            _pagination.Reset();
            _flowSkipTokens.Clear();
        }

        private void ShowError(Exception ex) =>
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        private void MyPluginControl_OnCloseTool(object sender, EventArgs e) =>
            SettingsManager.Instance.Save(GetType(), _settings);

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new System.Reflection.AssemblyName(args.Name);
            if (name.Name == "System.Diagnostics.DiagnosticSource")
            {
                string path = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "System.Diagnostics.DiagnosticSource.dll");
                if (File.Exists(path)) return System.Reflection.Assembly.LoadFrom(path);
            }
            return null;
        }

        #endregion
    }
}