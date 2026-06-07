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
using Newtonsoft.Json.Linq;
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

        // Dynamic UI controls
        private ToolStripLabel tslblQuickSearch;
        private ToolStripTextBox tstbQuickSearch;
        private ToolStripDropDownButton tsddColumns;

        // In-memory details and actions caches for high-performance retrieval and instant search refinement
        private readonly Dictionary<string, FlowRunDetailDto> _runDetailsCache = new Dictionary<string, FlowRunDetailDto>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowActionsResponseDto> _runActionsCache = new Dictionary<string, FlowActionsResponseDto>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _triggerContentsCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            InitializeAdditionalUi();
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
            if (_settings.CustomTriggerColumns == null)
            {
                _settings.CustomTriggerColumns = new List<CustomTriggerColumnSetting>();
            }
        }

        private void InitializeAdditionalUi()
        {
            // Add a separator before our custom view actions
            tsmContainer.Items.Add(new ToolStripSeparator());

            // 1. Column Selector Button
            tsddColumns = new ToolStripDropDownButton
            {
                Text = "Columns ⚙️",
                ToolTipText = "Add or remove columns from the view"
            };
            tsmContainer.Items.Add(tsddColumns);

            // Add another separator
            tsmContainer.Items.Add(new ToolStripSeparator());

            // 2. Search View Label & Box
            tslblQuickSearch = new ToolStripLabel("Search View: 🔍 ");
            tstbQuickSearch = new ToolStripTextBox
            {
                Size = new System.Drawing.Size(180, 25),
                ToolTipText = "Filter loaded runs by name, ID, status or trigger..."
            };
            tstbQuickSearch.TextChanged += (s, e) => ApplyLocalFilter();

            tsmContainer.Items.Add(tslblQuickSearch);
            tsmContainer.Items.Add(tstbQuickSearch);

            // Setup defaults for settings
            if (_settings.VisibleColumns == null || _settings.VisibleColumns.Count == 0)
            {
                _settings.VisibleColumns = new List<string>
                {
                    "FlowName", "Id", "Status", "StartDate", "EndDate", "Duration", "ViewRun", "ViewDetails"
                };
            }

            InitializeColumnSelector();
        }

        private void InitializeColumnSelector()
        {
            tsddColumns.DropDownItems.Clear();

            // Define all available columns
            var columnsInfo = new[]
            {
                new { Key = "FlowName", Text = "Flow Name" },
                new { Key = "Id", Text = "Run ID" },
                new { Key = "Status", Text = "Status" },
                new { Key = "StartDate", Text = "Start Time" },
                new { Key = "EndDate", Text = "End Time" },
                new { Key = "Duration", Text = "Duration" },
                new { Key = "TriggerName", Text = "Trigger" },
                new { Key = "TriggerStatus", Text = "Trigger Status" },
                new { Key = "ViewRun", Text = "Action" },
                new { Key = "ViewDetails", Text = "Details" }
            };

            var columnsList = columnsInfo.ToList();
            if (_settings.CustomTriggerColumns != null)
            {
                foreach (var cc in _settings.CustomTriggerColumns)
                {
                    columnsList.Add(new { Key = "col_custom_trigger_" + cc.JsonPath, Text = cc.HeaderText });
                }
            }

            foreach (var col in columnsList)
            {
                var item = new ToolStripMenuItem(col.Text)
                {
                    Name = $"tsmCol_{col.Key}",
                    CheckOnClick = true,
                    Checked = _settings.VisibleColumns.Contains(col.Key)
                };

                item.CheckedChanged += (s, e) =>
                {
                    if (item.Checked)
                    {
                        if (!_settings.VisibleColumns.Contains(col.Key))
                            _settings.VisibleColumns.Add(col.Key);
                    }
                    else
                    {
                        _settings.VisibleColumns.Remove(col.Key);
                    }
                    
                    ApplyColumnVisibility();
                    SettingsManager.Instance.Save(GetType(), _settings);
                };

                tsddColumns.DropDownItems.Add(item);
            }

            tsddColumns.DropDownItems.Add(new ToolStripSeparator());

            var addCustomItem = new ToolStripMenuItem("Add Custom Trigger Column...") { Image = null };
            addCustomItem.Click += (s, e) => AddCustomTriggerColumnAction();
            tsddColumns.DropDownItems.Add(addCustomItem);

            var clearCustomItem = new ToolStripMenuItem("Clear Custom Trigger Columns") { Image = null };
            clearCustomItem.Click += (s, e) => ClearCustomTriggerColumnsAction();
            tsddColumns.DropDownItems.Add(clearCustomItem);
        }

        private void ApplyColumnVisibility()
        {
            if (dataGridView1.Columns.Count == 0) return;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (!string.IsNullOrEmpty(col.Name))
                {
                    col.Visible = _settings.VisibleColumns.Contains(col.Name);
                }
            }
        }

        private void ApplyLocalFilter()
        {
            if (tstbQuickSearch == null) return;
            string filterText = tstbQuickSearch.Text?.Trim();
            
            var runsToFilter = _pagination.AllRuns.ToList();
            if (string.IsNullOrEmpty(filterText))
            {
                ShowCurrentPage();
                UpdatePaginationUI();
                return;
            }

            var filteredRuns = runsToFilter.Where(r =>
                (r.FlowName != null && r.FlowName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.Id != null && r.Id.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.Status != null && r.Status.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.TriggerName != null && r.TriggerName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.TriggerStatus != null && r.TriggerStatus.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                CheckTriggerLocalSearchMatch(r.Id, filterText)
            ).ToList();

            DataGridBinder.BindFlowRuns(dataGridView1, filteredRuns, _settings.CustomTriggerColumns);
            ApplyColumnVisibility();

            btnPrev.Enabled = false;
            btnNext.Enabled = false;
            lblPageInfo.Text = $"Filtered: {filteredRuns.Count} of {_pagination.AllRuns.Count} runs";
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var run = dataGridView1.Rows[e.RowIndex].DataBoundItem as FlowRun;
            if (run != null)
            {
                ShowRunDetails(run);
            }
        }

        private void WireEvents()
        {
            // -= avant += pour éviter les doublons
            dataGridView1.CellClick -= dataGridView1_CellClick;
            dataGridView1.CellClick += dataGridView1_CellClick;

            dataGridView1.CellDoubleClick -= dataGridView1_CellDoubleClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;

            dataGridView1.CellFormatting -= dataGridView1_CellFormatting;
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;

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
            DataGridBinder.BindFlowRuns(dataGridView1, _pagination.GetCurrentPage(), _settings.CustomTriggerColumns);
            ApplyColumnVisibility();
        }

        private void UpdatePaginationUI()
        {
            lblPageInfo.Text = _pagination.GetPageInfoText();
            btnPrev.Enabled = _pagination.CanGoPrevious() && !_pagination.IsLoading;
            btnNext.Enabled = _pagination.CanGoNext() && !_pagination.IsLoading;
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (_isDeepSearchActive) return;
            // CORRECTION : Protection contre double-clic et chargement
            if (!_pagination.CanGoPrevious() || _pagination.IsLoading) return;

            _pagination.CurrentPage--;
            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (_isDeepSearchActive) return;
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
                        var openHistoryBrowserItem = new ToolStripMenuItem("Open Flow History in Browser", null, (s, ev) => OpenFlowHistoryInBrowser(flow));

                        ctx.Items.Add(enableItem);
                        ctx.Items.Add(disableItem);
                        ctx.Items.Add(openBrowserItem);
                        ctx.Items.Add(openHistoryBrowserItem);

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

        private void OpenFlowHistoryInBrowser(Flow flow)
        {
            if (ConnectionDetail == null) return;
            string url = $"https://make.powerautomate.com/environments/{ConnectionDetail.EnvironmentId}/flows/{flow.Id}/runs";
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

                    using (var form = new RunDetailForm(run, detail, actions, client, _triggerContentsCache))  // ← Passer client et cache
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
                object processedLock = new object();
                object matchesLock = new object();

                var options = new System.Threading.Tasks.ParallelOptions
                {
                    MaxDegreeOfParallelism = 8 // Fetch up to 8 runs concurrently!
                };

                try
                {
                    System.Threading.Tasks.Parallel.ForEach(runsToSearch, options, (run, state) =>
                    {
                        if (_deepSearchWorker.CancellationPending)
                        {
                            state.Stop();
                            return;
                        }

                        try
                        {
                            // Find the flow for this run
                            var flow = _currentFlows.FirstOrDefault(f => f.DisplayName == run.FlowName);
                            if (flow == null)
                            {
                                lock (processedLock)
                                {
                                    processed++;
                                    _deepSearchWorker.ReportProgress(processed);
                                }
                                return;
                            }

                            bool matched = false;

                            // 1) Get Run Details (cached or from API)
                            FlowRunDetailDto detail = null;
                            lock (_runDetailsCache)
                            {
                                _runDetailsCache.TryGetValue(run.Id, out detail);
                            }

                            if (detail == null)
                            {
                                detail = client.GetRunDetails(flow.Id, run.Id);
                                if (detail != null)
                                {
                                    lock (_runDetailsCache)
                                    {
                                        _runDetailsCache[run.Id] = detail;
                                    }
                                }
                            }

                            if (detail?.Properties?.Trigger != null)
                            {
                                var trigger = detail.Properties.Trigger;

                                // Check inputs
                                string inputs = null;
                                if (trigger.InputsLink?.Uri != null)
                                {
                                    lock (_triggerContentsCache)
                                    {
                                        _triggerContentsCache.TryGetValue(trigger.InputsLink.Uri, out inputs);
                                    }
                                    if (inputs == null)
                                    {
                                        try
                                        {
                                            inputs = client.GetContentFromLink(trigger.InputsLink.Uri);
                                            if (inputs != null)
                                            {
                                                lock (_triggerContentsCache)
                                                {
                                                    _triggerContentsCache[trigger.InputsLink.Uri] = inputs;
                                                }
                                            }
                                        }
                                        catch { /* skip */ }
                                    }
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
                                        lock (_triggerContentsCache)
                                        {
                                            _triggerContentsCache.TryGetValue(trigger.OutputsLink.Uri, out outputs);
                                        }
                                        if (outputs == null)
                                        {
                                            try
                                            {
                                                outputs = client.GetContentFromLink(trigger.OutputsLink.Uri);
                                                if (outputs != null)
                                                {
                                                    lock (_triggerContentsCache)
                                                    {
                                                        _triggerContentsCache[trigger.OutputsLink.Uri] = outputs;
                                                    }
                                                }
                                            }
                                            catch { /* skip */ }
                                        }
                                    }
                                    else if (trigger.Outputs != null)
                                    {
                                        outputs = JsonConvert.SerializeObject(trigger.Outputs);
                                    }

                                    if (outputs != null && outputs.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                        matched = true;
                                }
                            }

                            // 2) Get Run Actions (cached or from API)
                            if (!matched)
                            {
                                FlowActionsResponseDto actions = null;
                                lock (_runActionsCache)
                                {
                                    _runActionsCache.TryGetValue(run.Id, out actions);
                                }

                                if (actions == null)
                                {
                                    actions = client.GetRunActions(flow.Id, run.Id);
                                    if (actions != null)
                                    {
                                        lock (_runActionsCache)
                                        {
                                            _runActionsCache[run.Id] = actions;
                                        }
                                    }
                                }

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
                            {
                                lock (matchesLock)
                                {
                                    matchingRuns.Add(run);
                                }
                            }
                        }
                        catch
                        {
                            // Skip runs that fail to load — don't crash the search
                        }
                        finally
                        {
                            lock (processedLock)
                            {
                                processed++;
                                _deepSearchWorker.ReportProgress(processed);
                            }
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    args.Cancel = true;
                    return;
                }

                if (_deepSearchWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
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
                    DataGridBinder.BindFlowRuns(dataGridView1, new List<FlowRun>(), _settings.CustomTriggerColumns);
                    ApplyColumnVisibility();
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

                DataGridBinder.BindFlowRuns(dataGridView1, matchingRuns, _settings.CustomTriggerColumns);
                ApplyColumnVisibility();
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

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col.Name.StartsWith("col_custom_trigger_"))
            {
                string jsonPath = col.Name.Substring("col_custom_trigger_".Length);
                var run = dataGridView1.Rows[e.RowIndex].DataBoundItem as FlowRun;
                if (run != null)
                {
                    if (_runDetailsCache.TryGetValue(run.Id, out var detail))
                    {
                        e.Value = ExtractValueFromJsonPath(detail, jsonPath) ?? "";
                    }
                    else
                    {
                        e.Value = "";
                    }
                }
            }
        }

        private string ExtractValueFromJsonPath(FlowRunDetailDto detail, string jsonPath)
        {
            if (detail?.Properties?.Trigger == null) return null;
            var trigger = detail.Properties.Trigger;

            var parts = jsonPath.Split('/');
            if (parts.Length < 2) return null;

            string root = parts[0].ToLower(); // inputs or outputs
            string section = parts[1]; // body, headers, parameters, host

            string jsonContent = null;
            if (root == "inputs")
            {
                if (trigger.InputsLink?.Uri != null)
                {
                    lock (_triggerContentsCache)
                    {
                        _triggerContentsCache.TryGetValue(trigger.InputsLink.Uri, out jsonContent);
                    }
                }
                else if (trigger.Inputs != null)
                {
                    jsonContent = JsonConvert.SerializeObject(trigger.Inputs);
                }
            }
            else if (root == "outputs")
            {
                if (trigger.OutputsLink?.Uri != null)
                {
                    lock (_triggerContentsCache)
                    {
                        _triggerContentsCache.TryGetValue(trigger.OutputsLink.Uri, out jsonContent);
                    }
                }
                else if (trigger.Outputs != null)
                {
                    jsonContent = JsonConvert.SerializeObject(trigger.Outputs);
                }
            }

            if (string.IsNullOrEmpty(jsonContent)) return null;

            try
            {
                var jObject = JObject.Parse(jsonContent);
                JToken currentToken = jObject[section];
                if (currentToken == null)
                {
                    foreach (var prop in jObject.Properties())
                    {
                        if (string.Equals(prop.Name, section, StringComparison.OrdinalIgnoreCase))
                        {
                            currentToken = prop.Value;
                            break;
                        }
                    }
                }

                if (currentToken == null) return null;

                for (int i = 2; i < parts.Length; i++)
                {
                    if (currentToken is JObject obj)
                    {
                        string part = parts[i];
                        JToken nextToken = obj[part];
                        if (nextToken == null)
                        {
                            foreach (var prop in obj.Properties())
                            {
                                if (string.Equals(prop.Name, part, StringComparison.OrdinalIgnoreCase))
                                {
                                    nextToken = prop.Value;
                                    break;
                                }
                            }
                        }
                        currentToken = nextToken;
                        if (currentToken == null) return null;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (currentToken == null) return null;
                if (currentToken is JValue jVal) return jVal.Value?.ToString();
                return currentToken.ToString(Formatting.None);
            }
            catch
            {
                return null;
            }
        }

        private List<string> GetDetectedTriggerPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var detail in _runDetailsCache.Values)
            {
                if (detail?.Properties?.Trigger == null) continue;
                var trigger = detail.Properties.Trigger;

                // Inputs
                string inputs = null;
                if (trigger.InputsLink?.Uri != null)
                {
                    lock (_triggerContentsCache)
                    {
                        _triggerContentsCache.TryGetValue(trigger.InputsLink.Uri, out inputs);
                    }
                }
                else if (trigger.Inputs != null)
                {
                    inputs = JsonConvert.SerializeObject(trigger.Inputs);
                }

                if (!string.IsNullOrEmpty(inputs))
                {
                    try
                    {
                        var token = JToken.Parse(inputs);
                        if (token is JObject obj)
                        {
                            if (obj["parameters"] is JObject paramsObj)
                                GetJsonPathsRecursive(paramsObj, "inputs/parameters/", paths);
                            if (obj["host"] is JObject hostObj)
                                GetJsonPathsRecursive(hostObj, "inputs/host/", paths);
                        }
                    }
                    catch { }
                }

                // Outputs
                string outputs = null;
                if (trigger.OutputsLink?.Uri != null)
                {
                    lock (_triggerContentsCache)
                    {
                        _triggerContentsCache.TryGetValue(trigger.OutputsLink.Uri, out outputs);
                    }
                }
                else if (trigger.Outputs != null)
                {
                    outputs = JsonConvert.SerializeObject(trigger.Outputs);
                }

                if (!string.IsNullOrEmpty(outputs))
                {
                    try
                    {
                        var token = JToken.Parse(outputs);
                        if (token is JObject obj)
                        {
                            if (obj["headers"] is JObject headersObj)
                                GetJsonPathsRecursive(headersObj, "outputs/headers/", paths);
                            if (obj["body"] is JObject bodyObj)
                                GetJsonPathsRecursive(bodyObj, "outputs/body/", paths);
                        }
                    }
                    catch { }
                }
            }
            return paths.OrderBy(p => p).ToList();
        }

        private void GetJsonPathsRecursive(JObject obj, string prefix, HashSet<string> paths)
        {
            foreach (var prop in obj.Properties())
            {
                string path = prefix + prop.Name;
                if (prop.Value is JObject subObj)
                {
                    GetJsonPathsRecursive(subObj, path + "/", paths);
                }
                else
                {
                    paths.Add(path);
                }
            }
        }

        private void AddCustomTriggerColumnAction()
        {
            var detectedPaths = GetDetectedTriggerPaths();
            using (var form = new AddCustomColumnForm(detectedPaths))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    string headerText = form.HeaderText;
                    string jsonPath = form.JsonPath;

                    if (string.IsNullOrEmpty(headerText) || string.IsNullOrEmpty(jsonPath))
                    {
                        MessageBox.Show("Header text and JSON path are required.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (_settings.CustomTriggerColumns.Any(c => string.Equals(c.JsonPath, jsonPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("A column with this JSON path already exists.", "Duplicate Column", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var newCol = new CustomTriggerColumnSetting { HeaderText = headerText, JsonPath = jsonPath };
                    _settings.CustomTriggerColumns.Add(newCol);

                    string colKey = "col_custom_trigger_" + jsonPath;
                    if (!_settings.VisibleColumns.Contains(colKey))
                    {
                        _settings.VisibleColumns.Add(colKey);
                    }

                    SettingsManager.Instance.Save(GetType(), _settings);
                    
                    ShowCurrentPage();
                    InitializeColumnSelector();
                }
            }
        }

        private void ClearCustomTriggerColumnsAction()
        {
            if (_settings.CustomTriggerColumns == null || _settings.CustomTriggerColumns.Count == 0) return;

            var confirm = MessageBox.Show("Are you sure you want to clear all custom trigger columns?", "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                foreach (var cc in _settings.CustomTriggerColumns)
                {
                    string colKey = "col_custom_trigger_" + cc.JsonPath;
                    _settings.VisibleColumns.Remove(colKey);
                }

                _settings.CustomTriggerColumns.Clear();
                SettingsManager.Instance.Save(GetType(), _settings);

                ShowCurrentPage();
                InitializeColumnSelector();
            }
        }

        private bool CheckTriggerLocalSearchMatch(string runId, string filterText)
        {
            if (!_runDetailsCache.TryGetValue(runId, out var detail))
                return false;

            if (detail?.Properties?.Trigger == null)
                return false;

            var trigger = detail.Properties.Trigger;

            string inputs = null;
            if (trigger.InputsLink?.Uri != null)
            {
                lock (_triggerContentsCache)
                {
                    _triggerContentsCache.TryGetValue(trigger.InputsLink.Uri, out inputs);
                }
            }
            else if (trigger.Inputs != null)
            {
                inputs = JsonConvert.SerializeObject(trigger.Inputs);
            }

            if (inputs != null && inputs.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            string outputs = null;
            if (trigger.OutputsLink?.Uri != null)
            {
                lock (_triggerContentsCache)
                {
                    _triggerContentsCache.TryGetValue(trigger.OutputsLink.Uri, out outputs);
                }
            }
            else if (trigger.Outputs != null)
            {
                outputs = JsonConvert.SerializeObject(trigger.Outputs);
            }

            if (outputs != null && outputs.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        #endregion
    }

    public class AddCustomColumnForm : Form
    {
        private System.Windows.Forms.Label lblHeader;
        private TextBox txtHeader;
        private System.Windows.Forms.Label lblPath;
        private ComboBox cmbPath;
        private Button btnOk;
        private Button btnCancel;

        public string HeaderText => txtHeader.Text?.Trim();
        public string JsonPath => cmbPath.Text?.Trim();

        public AddCustomColumnForm(List<string> detectedPaths)
        {
            InitializeUi(detectedPaths);
        }

        private void InitializeUi(List<string> detectedPaths)
        {
            this.Text = "Add Custom Trigger Column";
            this.Size = new System.Drawing.Size(420, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblHeader = new System.Windows.Forms.Label { Text = "Column Header:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(120, 20) };
            txtHeader = new TextBox { Location = new System.Drawing.Point(150, 18), Size = new System.Drawing.Size(230, 25) };

            lblPath = new System.Windows.Forms.Label { Text = "JSON Path:", Location = new System.Drawing.Point(20, 60), Size = new System.Drawing.Size(120, 20) };
            cmbPath = new ComboBox { Location = new System.Drawing.Point(150, 58), Size = new System.Drawing.Size(230, 25), DropDownStyle = ComboBoxStyle.DropDown };
            cmbPath.Items.AddRange(detectedPaths.Cast<object>().ToArray());

            btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(210, 120), Size = new System.Drawing.Size(80, 30) };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(300, 120), Size = new System.Drawing.Size(80, 30) };

            this.Controls.Add(lblHeader);
            this.Controls.Add(txtHeader);
            this.Controls.Add(lblPath);
            this.Controls.Add(cmbPath);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}