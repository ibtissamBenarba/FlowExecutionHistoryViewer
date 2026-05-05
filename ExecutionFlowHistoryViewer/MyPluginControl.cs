// MyPluginControl.cs
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
            // CORRECTION : -= avant += pour éviter les doubles attachements
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
            // CORRECTION : -= avant += pour éviter les doublons
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

        #endregion

        #region Grid Interaction

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != "ViewRun") return;

            var run = dataGridView1.Rows[e.RowIndex].DataBoundItem as FlowRun;
            if (run == null || string.IsNullOrEmpty(run.Url))
            {
                MessageBox.Show("Could not open run URL.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = run.Url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}\n\nURL: {run.Url}");
            }
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