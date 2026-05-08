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
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        #endregion

        #region Constructor & Lifecycle

        public MyPluginControl()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            clbFlows.CheckOnClick = true;
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
            _flowClientFactory = new FlowClientFactory(_authService, ConnectionDetail.EnvironmentId.ToString());
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

            if (tscNumberOfRuns != null)
            {
                tscNumberOfRuns.SelectedIndexChanged -= TscNumberOfRuns_SelectedIndexChanged;
                tscNumberOfRuns.SelectedIndexChanged += TscNumberOfRuns_SelectedIndexChanged;
            }
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

                    // CORRECTION : n'incrémenter la page que si des données ont été reçues
                    if (isNextPage && result.Runs.Count > 0)
                        _pagination.CurrentPage++;

                    ShowCurrentPage();
                    UpdatePaginationUI();
                }
            });
        }

        private void FetchTwoPages()
        {
            var (fromDate, toDate, status) = GetFilterValues();
            var flows = GetSelectedFlows();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading next 2 pages...",
                Work = (worker, args) =>
                {
                    var res1 = _historyService.FetchRuns(flows, fromDate, toDate, status, true, _flowSkipTokens, _pagination.PageSize);

                    if (res1.HasMore)
                    {
                        var res2 = _historyService.FetchRuns(flows, fromDate, toDate, status, true, _flowSkipTokens, _pagination.PageSize);
                        args.Result = new List<FlowRunPageResult> { res1, res2 };
                    }
                    else
                    {
                        args.Result = new List<FlowRunPageResult> { res1 };
                    }
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null) { ShowError(args.Error); return; }

                    var results = (List<FlowRunPageResult>)args.Result;
                    int pagesWithData = 0;

                    foreach (var res in results)
                    {
                        _pagination.AppendRuns(res.Runs);
                        _pagination.HasMoreServerPages = res.HasMore;
                        if (res.Runs.Count > 0)
                            pagesWithData++;
                    }

                    // CORRECTION : n'avancer que du nombre de pages qui ont réellement des données
                    _pagination.CurrentPage += pagesWithData;

                    // Si on est sur une page vide et qu'il n'y a plus de pages serveur, reculer
                    if (_pagination.GetCurrentPage().Count == 0 && !_pagination.HasMoreServerPages && _pagination.CurrentPage > 1)
                        _pagination.CurrentPage--;

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
                    if (targetPage <= _pagination.TotalCachedPages)
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
                        // La page demandée dépasse les données disponibles et il n'y a plus rien sur le serveur
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

            if (_pagination.CurrentPage < _pagination.TotalCachedPages)
            {
                _pagination.CurrentPage++;
                ShowCurrentPage();
                UpdatePaginationUI();
            }
            else if (_pagination.HasMoreServerPages)
            {
                var (fromDate, toDate, status) = GetFilterValues();
                FetchPage(GetSelectedFlows(), fromDate, toDate, status, isNextPage: true);
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

            if (targetPage <= _pagination.TotalCachedPages)
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

            // Récupère TOUTES les runs en cache (toutes les pages chargées)
            var allRuns = (_pagination != null && _pagination.AllRuns != null)
                ? _pagination.AllRuns.ToList()
                : currentPage;

            using (var form = new ExportForm(currentPage, allRuns))
            {
                form.ShowDialog(this);
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
    }
}