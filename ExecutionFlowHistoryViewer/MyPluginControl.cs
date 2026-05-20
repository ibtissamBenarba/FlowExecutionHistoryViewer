// MyPluginControl.cs
using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Forms;
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Forms;
using XrmToolBox.Extensibility;


namespace ExecutionFlowHistoryViewer
{
    public partial class MyPluginControl : PluginControlBase
    {
        #region Fields & Dependencies

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
        #endregion

        #region Constructor & Lifecycle
        
        public MyPluginControl()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            dataGridView1.ReadOnly = true;
            clbFlows.CheckOnClick = true;
            dataGridView1.MultiSelect = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            InitializeFilters();
            InitializePagination();
            InitializeSettings();
            WireEvents();

            if (Service != null) InitializeServices();
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            _authService?.Reset();
            InitializeServices();
            LoadSolutions();
        }

        private void MyPluginControl_OnCloseTool(object sender, EventArgs e) =>
            SettingsManager.Instance.Save(GetType(), _settings);

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        #endregion

        #region Initialization

        private void InitializeServices()
        {
            _authService = new AuthenticationService(ConnectionDetail);
            _dataverseService = new DataverseService(Service);
            _flowClientFactory = new FlowClientFactory(
                _authService,
                ConnectionDetail.EnvironmentId.ToString(),
                ConnectionDetail);  // ← Pass ConnectionDetail here
            _historyService = new FlowHistoryService(_flowClientFactory);
            _pagination = new PaginationService();
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

        private void InitializeFilters()
        {
            cmbStatus.Items.Clear();
            cmbStatus.Items.AddRange(new object[] { "All", "Succeeded", "Failed", "Cancelled", "Running" });
            cmbStatus.SelectedIndex = 0;
        }

        private void InitializePagination()
        {
            // Button wiring
            WirePaginationButton(tsbPrevious, tsbPrevious_Click);
            WirePaginationButton(tsbNext, tsbNext_Click);
            WirePaginationButton(tsbSkipPrevious, tsbSkipPrevious_Click);
            WirePaginationButton(tsbSkipNext, tsbSkipNext_Click);

            // Default values
            if (tstbPageNumber != null) tstbPageNumber.Text = "1";
            if (tslPageNumber != null) tslPageNumber.Text = "of 1";
            if (tslTotalItems != null) tslTotalItems.Text = "0 - 0 of 0 flow runs";

            InitializePageSizeCombo();
        }

        private void InitializePageSizeCombo()
        {
            if (tscNumberOfRuns == null) return;

            tscNumberOfRuns.Items.Clear();
            tscNumberOfRuns.Items.AddRange(new object[] { "25", "50", "100" });
            tscNumberOfRuns.DropDownStyle = ComboBoxStyle.DropDownList;
            tscNumberOfRuns.SelectedItem = _pagination?.PageSize.ToString() ?? "50";

            tscNumberOfRuns.SelectedIndexChanged -= TscNumberOfRuns_SelectedIndexChanged;
            tscNumberOfRuns.SelectedIndexChanged += TscNumberOfRuns_SelectedIndexChanged;
        }

        private void WireEvents()
        {
            tstbPageNumber.KeyDown -= TstbPageNumber_KeyDown;
            tstbPageNumber.KeyDown += TstbPageNumber_KeyDown;

            dataGridView1.CellFormatting -= dataGridView1_CellFormatting;
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;

            dataGridView1.CellClick -= dataGridView1_CellClick;
            dataGridView1.CellClick += dataGridView1_CellClick;

            cbSolutions.SelectedIndexChanged -= cbSolutions_SelectedIndexChanged;
            cbSolutions.SelectedIndexChanged += cbSolutions_SelectedIndexChanged;

            clbFlows.ItemCheck -= clbFlows_ItemCheck;
            clbFlows.ItemCheck += clbFlows_ItemCheck;

            tbSearch.TextChanged -= tbSearch_TextChanged;
            tbSearch.TextChanged += tbSearch_TextChanged;

            cbSelectAllFlows.CheckedChanged -= cbSelectAllFlows_CheckedChanged;
            cbSelectAllFlows.CheckedChanged += cbSelectAllFlows_CheckedChanged;

            // AJOUT : Filtres par statut de flow
            if (cbxFlowStatusActivated != null)
            {
                cbxFlowStatusActivated.CheckedChanged -= CbxFlowStatus_CheckedChanged;
                cbxFlowStatusActivated.CheckedChanged += CbxFlowStatus_CheckedChanged;
            }
            if (cbxFlowStatusDraft != null)
            {
                cbxFlowStatusDraft.CheckedChanged -= CbxFlowStatus_CheckedChanged;
                cbxFlowStatusDraft.CheckedChanged += CbxFlowStatus_CheckedChanged;
            }

            if (tscNumberOfRuns != null)
            {
                tscNumberOfRuns.SelectedIndexChanged -= TscNumberOfRuns_SelectedIndexChanged;
                tscNumberOfRuns.SelectedIndexChanged += TscNumberOfRuns_SelectedIndexChanged;
            }
            btnDeepSearch.Click -= btnDeepSearch_Click;
            btnDeepSearch.Click += btnDeepSearch_Click;

            btnClearDeepSearch.Click -= btnClearDeepSearch_Click;
            btnClearDeepSearch.Click += btnClearDeepSearch_Click;

            tbDeepSearch.KeyDown -= tbDeepSearch_KeyDown;
            tbDeepSearch.KeyDown += tbDeepSearch_KeyDown;
        }

        private void WirePaginationButton(ToolStripButton button, EventHandler handler)
        {
            if (button == null) return;
            button.Enabled = false;
            button.Click -= handler;
            button.Click += handler;
        }

        #endregion

        #region Connection & Authentication

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

        #region Flow Selection & Filtering

        private List<Flow> GetSelectedFlows() =>
            _currentFlows.Where(f => _checkedFlowIds.Contains(f.Id)).ToList();

        private void ApplyFlowFilter()
        {
            string search = tbSearch.Text?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(search)
                ? _currentFlows
                : _currentFlows.Where(f => f.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            bool showActivated = cbxFlowStatusActivated?.Checked ?? true;
            bool showDraft = cbxFlowStatusDraft?.Checked ?? true;

            if (!showActivated || !showDraft)
            {
                filtered = filtered.Where(f =>
                {
                    bool isActivated = f.StateCode == 1;
                    bool isDraft = f.StateCode == 0;

                    if (showActivated && isActivated) return true;
                    if (showDraft && isDraft) return true;
                    return false;
                }).ToList();
            }

            clbFlows.ItemCheck -= clbFlows_ItemCheck;

            clbFlows.Items.Clear();

            foreach (var flow in filtered)
                clbFlows.Items.Add(flow, _checkedFlowIds.Contains(flow.Id));

            clbFlows.ItemCheck += clbFlows_ItemCheck;

            // ADD THIS
            UpdateSelectAllState();
        }

        private void cbSelectAllFlows_CheckedChanged(object sender, EventArgs e)
        {
            // Get only visible/filtered flows
            var visibleFlows = clbFlows.Items.Cast<Flow>().ToList();

            if (cbSelectAllFlows.Checked)
            {
                foreach (var flow in visibleFlows)
                    _checkedFlowIds.Add(flow.Id);
            }
            else
            {
                foreach (var flow in visibleFlows)
                    _checkedFlowIds.Remove(flow.Id);
            }

            ApplyFlowFilter();
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

            ResetPagination();
            DataGridBinder.BindFlowRuns(dataGridView1, new List<FlowRun>());
            UpdatePaginationUI();

            BeginInvoke((MethodInvoker)(() =>
            {
                UpdateSelectAllState();
            }));
        }

        private void UpdateSelectAllState()
        {
            var visibleFlows = clbFlows.Items.Cast<Flow>().ToList();

            bool allVisibleChecked =
                visibleFlows.Count > 0 &&
                visibleFlows.All(f => _checkedFlowIds.Contains(f.Id));

            cbSelectAllFlows.CheckedChanged -= cbSelectAllFlows_CheckedChanged;
            cbSelectAllFlows.Checked = allVisibleChecked;
            cbSelectAllFlows.CheckedChanged += cbSelectAllFlows_CheckedChanged;
        }

        private void cbSolutions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(cbSolutions.SelectedItem is SolutionItem selected)) return;
            LoadFlows(selected.Id == Guid.Empty ? (Guid?)null : selected.Id);
        }

        private void CbxFlowStatus_CheckedChanged(object sender, EventArgs e)
        {
            ApplyFlowFilter();
        }

        private void tbSearch_TextChanged(object sender, EventArgs e) => ApplyFlowFilter();

        #endregion

        #region History Fetching

        private void btnFetchHistory_Click_1(object sender, EventArgs e)
        {
            var selectedFlows = GetSelectedFlows();
            if (!ValidateFetch(selectedFlows)) return;

            var (fromDate, toDate, status) = GetFilterValues();
            var flowIds = selectedFlows.Select(f => f.Id).ToList();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Calculating total history...",
                Work = (worker, args) =>
                {
                    int total = _dataverseService.GetTotalFlowRunsCount(flowIds, fromDate, toDate, status);
                    args.Result = total;
                },
                PostWorkCallBack = (args) =>
                {
                    _pagination.Reset();
                    _pagination.TotalServerCount = (int)args.Result;
                    _flowSkipTokens.Clear();
                    FetchPage(selectedFlows, fromDate, toDate, status, isNextPage: false);
                }
            });
        }

        private void FetchPage(List<Flow> flows, DateTime fromDate, DateTime toDate, string status, bool isNextPage)
        {
            if (_pagination.IsLoading) return;
            _pagination.IsLoading = true;
            UpdatePaginationUI();

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

                    int countBefore = _pagination.AllRuns.Count;
                    _pagination.AppendRuns(result.Runs);
                    _pagination.HasMoreServerPages = result.HasMore;

                    // Only advance page if NEW unique runs were actually added
                    if (isNextPage && _pagination.AllRuns.Count > countBefore)
                        _pagination.CurrentPage++;

                    ShowCurrentPage();
                    UpdatePaginationUI();
                }
            });
        }

        private void FetchTwoPages()
        {
            if (_pagination.IsLoading) return;
            _pagination.IsLoading = true;
            UpdatePaginationUI();

            int targetPage = _pagination.CurrentPage + 2;
            var (fromDate, toDate, status) = GetFilterValues();
            var flows = GetSelectedFlows();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading next 2 pages...",
                Work = (worker, args) =>
                {
                    int requiredItems = targetPage * _pagination.PageSize;
                    var fetchedRuns = new List< FlowRun > ();
                    bool hasMore = _pagination.HasMoreServerPages;
                    int baseCount = _pagination.AllRuns.Count;
                    int safety = 0;

                    while (baseCount + fetchedRuns.Count < requiredItems && hasMore && safety < 50)
                    {
                        var result = _historyService.FetchRuns(
                            flows, fromDate, toDate, status, true, _flowSkipTokens, _pagination.PageSize);

                        if (result?.Runs != null && result.Runs.Count > 0)
                        {
                            fetchedRuns.AddRange(result.Runs);
                            hasMore = result.HasMore;
                        }
                        else
                        {
                            hasMore = false;
                            break;
                        }
                        safety++;
                    }

                    args.Result = new Tuple<List< FlowRun >, bool> (fetchedRuns, hasMore);
                },
                PostWorkCallBack = (args) =>
                {
                    _pagination.IsLoading = false;
                    if (args.Error != null) { ShowError(args.Error); UpdatePaginationUI(); return; }

                    var result = (Tuple < List < FlowRun >, bool >)args.Result;
                    _pagination.AppendRuns(result.Item1);
                    _pagination.HasMoreServerPages = result.Item2;

                    int requiredItems = targetPage * _pagination.PageSize;
                    if (_pagination.AllRuns.Count >= requiredItems || !_pagination.HasMoreServerPages)
                    {
                        _pagination.CurrentPage = targetPage;
                    }
                    else
                    {
                        _pagination.CurrentPage = Math.Max(1, _pagination.TotalCachedPages);
                    }

                    ShowCurrentPage();
                    UpdatePaginationUI();
                }
            });
        }

        #endregion

        #region Pagination UI & Navigation

        private void ShowCurrentPage()
        {
            DataGridBinder.BindFlowRuns(dataGridView1, _pagination.GetCurrentPage());
        }

        private void UpdatePaginationUI()
        {
            if (_pagination == null) return;

            // Sync page size combo
            if (tscNumberOfRuns != null && !tscNumberOfRuns.Focused)
            {
                string currentSize = _pagination.PageSize.ToString();
                if (tscNumberOfRuns.Items.Contains(currentSize) && tscNumberOfRuns.SelectedItem?.ToString() != currentSize)
                    tscNumberOfRuns.SelectedItem = currentSize;
            }

            // Button states
            if (tsbSkipPrevious != null)
                tsbSkipPrevious.Enabled = _pagination.CurrentPage > 2 && !_pagination.IsLoading;

            if (tsbSkipNext != null)
                tsbSkipNext.Enabled = (_pagination.CurrentPage < _pagination.TotalPages) && !_pagination.IsLoading;

            tsbPrevious.Enabled = _pagination.CanGoPrevious() && !_pagination.IsLoading;
            tsbNext.Enabled = _pagination.CanGoNext() && !_pagination.IsLoading;

            // Page info
            tstbPageNumber.Text = _pagination.CurrentPage.ToString();
            tslPageNumber.Text = $"of {_pagination.TotalPages}";

            int startItem = _pagination.TotalServerCount == 0 ? 0 : (_pagination.CurrentPage - 1) * _pagination.PageSize + 1;
            int endItem = Math.Min(_pagination.CurrentPage * _pagination.PageSize, _pagination.TotalServerCount);
            tslTotalItems.Text = $"{startItem} - {endItem} of {_pagination.TotalServerCount} flow runs";
        }

        private void TscNumberOfRuns_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tscNumberOfRuns?.SelectedItem == null) return;
            if (!int.TryParse(tscNumberOfRuns.SelectedItem.ToString(), out int newPageSize)) return;
            if (_pagination == null) return;
            if (_pagination.PageSize == newPageSize) return;
             
            _pagination.PageSize = newPageSize;
            _pagination.CurrentPage = 1;

            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void TstbPageNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            if (int.TryParse(tstbPageNumber.Text, out int targetPage))
            {
                if (targetPage >= 1 && targetPage <= _pagination.TotalPages)
                {
                    int requiredItems = targetPage * _pagination.PageSize;
                    bool hasEnoughData = _pagination.AllRuns.Count >= requiredItems;
                    bool isLastPage = !_pagination.HasMoreServerPages
                        && _pagination.AllRuns.Count > (targetPage - 1) * _pagination.PageSize;

                    if (hasEnoughData || isLastPage)
                    {
                        _pagination.CurrentPage = targetPage;
                        ShowCurrentPage();
                        UpdatePaginationUI();
                    }
                    else if (_pagination.HasMoreServerPages)
                    {
                        MessageBox.Show("Please load intermediate pages using the 'Next' button first.", "Info");
                        tstbPageNumber.Text = _pagination.CurrentPage.ToString();
                    }
                    else
                    {
                        tstbPageNumber.Text = _pagination.CurrentPage.ToString();
                    }
                }
                else
                {
                    tstbPageNumber.Text = _pagination.CurrentPage.ToString();
                }
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void tsbPrevious_Click(object sender, EventArgs e)
        {
            if (!_pagination.CanGoPrevious() || _pagination.IsLoading) return;
            _pagination.CurrentPage--;
            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void tsbNext_Click(object sender, EventArgs e)
        {
            if (_pagination.IsLoading) return;

            int targetPage = _pagination.CurrentPage + 1;
            int requiredItems = targetPage * _pagination.PageSize;

            bool hasEnoughData = _pagination.AllRuns.Count >= requiredItems;
            bool isLastPage = !_pagination.HasMoreServerPages
                && _pagination.AllRuns.Count > (targetPage - 1) * _pagination.PageSize;

            if (hasEnoughData || isLastPage)
            {
                _pagination.CurrentPage = targetPage;
                ShowCurrentPage();
                UpdatePaginationUI();
            }
            else if (_pagination.HasMoreServerPages)
            {
                EnsurePageLoaded(targetPage);
            }
        }

        private void tsbSkipPrevious_Click(object sender, EventArgs e)
        {
            if (_pagination.IsLoading) return;

            int targetPage = Math.Max(1, _pagination.CurrentPage - 2);
            if (targetPage != _pagination.CurrentPage)
            {
                _pagination.CurrentPage = targetPage;
                ShowCurrentPage();
                UpdatePaginationUI();
            }
        }

        private void tsbSkipNext_Click(object sender, EventArgs e)
        {
            if (_pagination.IsLoading) return;

            int targetPage = _pagination.CurrentPage + 2;
            int requiredItems = targetPage * _pagination.PageSize;

            bool hasEnoughData = _pagination.AllRuns.Count >= requiredItems;
            bool isLastPage = !_pagination.HasMoreServerPages
                && _pagination.AllRuns.Count > (targetPage - 1) * _pagination.PageSize;

            if (hasEnoughData || isLastPage)
            {
                _pagination.CurrentPage = targetPage;
                ShowCurrentPage();
                UpdatePaginationUI();
            }
            else if (_pagination.HasMoreServerPages)
            {
                FetchTwoPages();
            }
        }

        private void EnsurePageLoaded(int targetPage)
        {
            if (_pagination.IsLoading) return;
            _pagination.IsLoading = true;
            UpdatePaginationUI();

            var (fromDate, toDate, status) = GetFilterValues();
            var flows = GetSelectedFlows();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading page...",
                Work = (worker, args) =>
                {
                    int requiredItems = targetPage * _pagination.PageSize;
                    var fetchedRuns = new List< FlowRun > ();
                    bool hasMore = _pagination.HasMoreServerPages;
                    int baseCount = _pagination.AllRuns.Count;
                    int safety = 0;

                    while (baseCount + fetchedRuns.Count < requiredItems && hasMore && safety < 50)
                    {
                        var result = _historyService.FetchRuns(
                            flows,
                            fromDate,
                            toDate,
                            status,
                            true,
                            _flowSkipTokens,
                            _pagination.PageSize);

                        if (result?.Runs != null && result.Runs.Count > 0)
                        {
                            fetchedRuns.AddRange(result.Runs);
                            hasMore = result.HasMore;
                        }
                        else
                        {
                            hasMore = false;
                            break;
                        }
                        safety++;
                    }

                    args.Result = new Tuple<List< FlowRun >, bool> (fetchedRuns, hasMore);
                },
                PostWorkCallBack = (args) =>
                {
                    _pagination.IsLoading = false;

                    if (args.Error != null)
                    {
                        ShowError(args.Error);
                        UpdatePaginationUI();
                        return;
                    }

                    var result = (Tuple < List < FlowRun >, bool >)args.Result;
                    _pagination.AppendRuns(result.Item1);
                    _pagination.HasMoreServerPages = result.Item2;

                    int requiredItems = targetPage * _pagination.PageSize;
                    if (_pagination.AllRuns.Count >= requiredItems || !_pagination.HasMoreServerPages)
                    {
                        _pagination.CurrentPage = targetPage;
                    }
                    else
                    {
                        _pagination.CurrentPage = Math.Max(1, _pagination.TotalCachedPages);
                    }

                    ShowCurrentPage();
                    UpdatePaginationUI();
                }
            });
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

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var column = dataGridView1.Columns[e.ColumnIndex];

            if (column.DataPropertyName != "Status")
                return;

            string status = e.Value?.ToString();

            if (string.IsNullOrWhiteSpace(status))
                return;

            // Reset style first
            e.CellStyle.Font = dataGridView1.DefaultCellStyle.Font;

            switch (status)
            {
                case "Succeeded":
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.ForeColor = Color.Black;

                    e.CellStyle.SelectionBackColor = Color.Green;
                    e.CellStyle.SelectionForeColor = Color.White;
                    break;

                case "Failed":
                    e.CellStyle.BackColor = Color.LightCoral;
                    e.CellStyle.ForeColor = Color.Black;

                    e.CellStyle.SelectionBackColor = Color.Red;
                    e.CellStyle.SelectionForeColor = Color.White;
                    break;

                case "Cancelled":
                    e.CellStyle.BackColor = Color.Khaki;
                    e.CellStyle.ForeColor = Color.Black;

                    e.CellStyle.SelectionBackColor = Color.Goldenrod;
                    e.CellStyle.SelectionForeColor = Color.White;
                    break;

                case "Running":
                    e.CellStyle.BackColor = Color.LightBlue;
                    e.CellStyle.ForeColor = Color.Black;

                    e.CellStyle.SelectionBackColor = Color.RoyalBlue;
                    e.CellStyle.SelectionForeColor = Color.White;
                    break;

                default:
                    e.CellStyle.BackColor = Color.White;
                    e.CellStyle.ForeColor = Color.Black;
                    break;
            }

            e.FormattingApplied = true;
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
                    using (var form = new RunDetailForm(run, tuple.Item1, tuple.Item2, tuple.Item3))
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
                //btnPrev.Enabled = false;
                //btnNext.Enabled = false;
                //lblPageInfo.Text = $"Showing {matchingRuns.Count} filtered result(s)";

                DataGridBinder.BindFlowRuns(dataGridView1, matchingRuns);
            };

            _deepSearchWorker.RunWorkerAsync();
        }

        #endregion

        #region Export

        private void btnExport_Click_1(object sender, EventArgs e)
        {
            var currentPage = _pagination?.GetCurrentPage();
            if (currentPage == null || currentPage.Count == 0)
            {
                MessageBox.Show("No data to export. Please fetch history first.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new ExportForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                // Choisir le fichier d'abord
                string filter = form.IsCsv ? "CSV files (*.csv)|*.csv" : "Excel files (*.xlsx)|*.xlsx";
                string filePath;
                using (var sfd = new SaveFileDialog { Filter = filter })
                {
                    if (sfd.ShowDialog() != DialogResult.OK) return;
                    filePath = sfd.FileName;
                }

                // Exporter selon la portée choisie
                if (form.ExportAllPages)
                {
                    FetchAllPagesAndExport(filePath, form);
                }
                else
                {
                    ExecuteExport(currentPage, filePath, form);
                }
            }
        }
 
        private void FetchAllPagesAndExport(string filePath, ExportForm options)
        {
            var (fromDate, toDate, status) = GetFilterValues();
            var flows = GetSelectedFlows();

            var skipTokens = new Dictionary<string, string>(_flowSkipTokens, StringComparer.OrdinalIgnoreCase);
            int pageSize = _pagination.PageSize;
            var accumulatedRuns = new List<FlowRun>(_pagination.AllRuns);

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading all pages for export...",
                Work = (worker, args) =>
                {
                    int safety = 0;
                    bool hasMore = true;
                    bool isFirstFetch = accumulatedRuns.Count == 0;

                    while (hasMore && safety < 500)
                    {
                        var result = _historyService.FetchRuns(
                            flows, fromDate, toDate, status, !isFirstFetch, skipTokens, pageSize);

                        int runsInThisBatch = 0;
                        if (result?.Runs != null)
                        {
                            runsInThisBatch = result.Runs.Count;
                            accumulatedRuns.AddRange(result.Runs);
                        }

                        hasMore = result?.HasMore ?? false;

                        // IMPORTANT: If API says there's more but we got zero items, 
                        // something is wrong - log it but don't infinite loop
                        if (hasMore && runsInThisBatch == 0)
                        {
                            // Force exit to prevent infinite loop on buggy API
                            hasMore = false;
                        }

                        isFirstFetch = false;
                        safety++;
                    }

                    // Final deduplication and chronological sort
                    var distinctRuns = accumulatedRuns
                        .GroupBy(r => r.Id)
                        .Select(g => g.First())
                        .OrderByDescending(r => r.StartDate)
                        .ToList();

                    args.Result = distinctRuns;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null) { ShowError(args.Error); return; }

                    var allRuns = (List<FlowRun>)args.Result;

                    // Sync pagination state with fully loaded data
                    _pagination.Reset();
                    _pagination.AppendRuns(allRuns);
                    _pagination.HasMoreServerPages = false;
                    _pagination.TotalServerCount = allRuns.Count;

                    ExecuteExport(allRuns, filePath, options);
                }
            });
        }

        private void ExecuteExport(List<FlowRun> runs, string filePath, ExportForm options)
        {
            try
            {
                if (options.IsCsv)
                {
                    CsvService.Export(runs, filePath, options.GetSelectedDelimiter(), options.GetSelectedEncoding(), options.IncludeHeaders);
                }
                else
                {
                    ExcelService.Export(runs, filePath, options.IncludeHeaders);
                }

                MessageBox.Show("Export completed successfully!", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Helpers & Validation

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

        private void btnResubmit_Click(object sender, EventArgs e)
        {
            // Get all selected rows
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select one or more flow runs to resubmit.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            // Extract valid FlowRun objects
            var selectedRuns = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as FlowRun)
                .Where(r => r != null)
                .ToList();

            if (selectedRuns.Count == 0)
            {
                MessageBox.Show(
                    "Could not get selected runs.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // Confirmation
            if (MessageBox.Show(
                $"Resubmit {selectedRuns.Count} selected run(s)?",
                "Confirm Bulk Resubmit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Resubmitting {selectedRuns.Count} run(s)...",

                Work = (worker, args) =>
                {
                    var client = _flowClientFactory.Create();

                    int successCount = 0;
                    int failedCount = 0;

                    var errors = new List<string>();

                    foreach (var run in selectedRuns)
                    {
                        try
                        {
                            string flowId = GetFlowIdForRun(run);

                            if (string.IsNullOrEmpty(flowId))
                            {
                                failedCount++;
                                errors.Add($"Run {run.Id}: Flow ID not found.");
                                continue;
                            }

                            bool result = client.ResubmitRun(flowId, run.Id);

                            if (result)
                            {
                                successCount++;
                            }
                            else
                            {
                                failedCount++;
                                errors.Add($"Run {run.Id}: Resubmit failed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            errors.Add($"Run {run.Id}: {ex.Message}");
                        }
                    }

                    args.Result = new
                    {
                        Success = successCount,
                        Failed = failedCount,
                        Errors = errors
                    };
                },

                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error);
                        return;
                    }

                    dynamic result = args.Result;

                    lblDeepSearchStatus.Text =
                        $"✔ {result.Success} run(s) resubmitted, ✖ {result.Failed} failed.";

                    lblDeepSearchStatus.ForeColor =
                        result.Failed > 0 ? Color.DarkOrange : Color.Green;

                    string message =
                        $"Successfully resubmitted: {result.Success}\n" +
                        $"Failed: {result.Failed}";

                    if (result.Errors.Count > 0)
                    {
                        message += "\n\nErrors:\n- " +
                                   string.Join("\n- ", result.Errors);
                    }

                    MessageBox.Show(
                        message,
                        "Bulk Resubmit Completed",
                        MessageBoxButtons.OK,
                        result.Failed > 0
                            ? MessageBoxIcon.Warning
                            : MessageBoxIcon.Information);
                }
            });
        }

        private string GetFlowIdForRun(FlowRun run)
        {
            if (run == null) return null;

            // If the run already has FlowId stored, use it
            if (!string.IsNullOrEmpty(run.FlowId))
                return run.FlowId;

            // Otherwise, look it up from the cached flows list
            var flow = _currentFlows.FirstOrDefault(f =>
                f.DisplayName == run.FlowName ||
                f.Id == run.FlowName);

            return flow?.Id;
        }
    }
}