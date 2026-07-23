// MyPluginControl.cs
using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Forms;
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        // Dynamic UI controls
        private ToolStripLabel tslblQuickSearch;
        private ToolStripTextBox tstbQuickSearch;
        private ToolStripDropDownButton tsddColumns;

        // In-memory details and actions caches for high-performance retrieval and instant search refinement
        private readonly Dictionary<string, FlowRunDetailDto> _runDetailsCache = new Dictionary<string, FlowRunDetailDto>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowActionsResponseDto> _runActionsCache = new Dictionary<string, FlowActionsResponseDto>(StringComparer.OrdinalIgnoreCase);

        private ConditionGroup _currentTriggerFilter;
        private readonly Dictionary<string, JObject> _triggerOutputsCache = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        private bool _isTriggerFilterActive;

        private readonly object _triggerOutputsCacheLock = new object();

        // Flow run selection state (grid checkboxes)
        private readonly HashSet<string> _checkedRunIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _isUpdatingRunCheckState = false;

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
            dataGridView1.MultiSelect = true;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            InitializeFilters();
            InitializePagination();
            InitializeSettings();
            InitializeAdditionalUi();
            WireEvents();
            InitializeTheme();

            if (Service != null) InitializeServices();

            // Wire checkbox-specific events
            WireCheckboxEvents();
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
            DataGridBinder.SyncCheckboxStates(dataGridView1, _checkedRunIds);
            UpdateSelectAllHeaderState();
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

            foreach (var col in columnsInfo)
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
        }

        private void ApplyColumnVisibility()
        {
            if (dataGridView1.Columns.Count == 0) return;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (!string.IsNullOrEmpty(col.Name))
                {
                    // Always keep the Select checkbox visible
                    if (col.Name == "Select")
                    {
                        col.Visible = true;
                        continue;
                    }
                    col.Visible = _settings.VisibleColumns.Contains(col.Name);
                }
            }
        }

        private void ApplyLocalFilter()
        {
            // GARDE NULL CRITIQUE
            if (_pagination == null) return;
            if (tbDeepSearch == null) return;

            string filterText = tbDeepSearch.Text?.Trim() ?? "";

            var runsToFilter = _pagination.AllRuns?.ToList() ?? new List<FlowRun>();

            if (string.IsNullOrEmpty(filterText))
            {
                // Ne rien faire si pas de données chargées
                if (_pagination.AllRuns == null || _pagination.AllRuns.Count == 0)
                {
                    lblDeepSearchStatus.Text = "";
                    return;
                }
                ShowCurrentPage();
                UpdatePaginationUI();
                lblDeepSearchStatus.Text = "";
                return;
            }

            var filteredRuns = runsToFilter.Where(r =>
                (r.FlowName != null && r.FlowName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.Id != null && r.Id.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.Status != null && r.Status.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.TriggerName != null && r.TriggerName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (r.TriggerStatus != null && r.TriggerStatus.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            DataGridBinder.BindFlowRuns(dataGridView1, filteredRuns, _checkedRunIds);
            ApplyColumnVisibility();
            UpdateSelectAllHeaderState();
            lblDeepSearchStatus.Text = $"✔ {filteredRuns.Count} result(s) found";
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

            dataGridView1.CellDoubleClick -= dataGridView1_CellDoubleClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;

            cbSolutions.SelectedIndexChanged -= cbSolutions_SelectedIndexChanged;
            cbSolutions.SelectedIndexChanged += cbSolutions_SelectedIndexChanged;

            clbFlows.ItemCheck -= clbFlows_ItemCheck;
            clbFlows.ItemCheck += clbFlows_ItemCheck;

            clbFlows.MouseDown -= clbFlows_MouseDown;
            clbFlows.MouseDown += clbFlows_MouseDown;

            tbDeepSearch.TextChanged -= TbDeepSearch_TextChanged;
            tbDeepSearch.TextChanged += TbDeepSearch_TextChanged;

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
            // SUPPRIMER tout le bloc Deep Search et remplacer par :
            tbDeepSearch.TextChanged -= TbDeepSearch_TextChanged;
            tbDeepSearch.TextChanged += TbDeepSearch_TextChanged;

            btnClearDeepSearch.Click -= BtnClearLocalFilter_Click;
            btnClearDeepSearch.Click += BtnClearLocalFilter_Click;

            if (tsbDarkMode != null)
            {
                tsbDarkMode.Click -= ToggleDarkMode;
                tsbDarkMode.Click += ToggleDarkMode;
            }

            // Trigger Output Filter buttons (add these buttons to your WinForms designer first)
            if (btnTriggerFilter != null)
            {
                btnTriggerFilter.Click -= btnTriggerFilter_Click;
                btnTriggerFilter.Click += btnTriggerFilter_Click;
            }

            if (tsbCompareRuns != null)
            {
                tsbCompareRuns.Click -= tsbCompareRuns_Click;
                tsbCompareRuns.Click += tsbCompareRuns_Click;
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

        #region Grid Checkbox Selection

        private void WireCheckboxEvents()
        {
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
            dataGridView1.RowPrePaint += DataGridView1_RowPrePaint;
        }

        private void DataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dataGridView1.Rows[e.RowIndex];
            var run = row.DataBoundItem as FlowRun;
            if (run == null) return;

            // Use _checkedRunIds as single source of truth
            bool isChecked = _checkedRunIds.Contains(run.Id);

            if (isChecked)
            {
                row.DefaultCellStyle.BackColor = Color.LightBlue;
                row.DefaultCellStyle.SelectionBackColor = Color.LightBlue;
            }
            else
            {
                row.DefaultCellStyle.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                row.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.SelectionBackColor;
            }
        }
        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

            var row = dataGridView1.Rows[e.RowIndex];
            var run = row.DataBoundItem as FlowRun;
            if (run == null) return;

            // Toggle based on current _checkedRunIds state (single source of truth)
            bool isCurrentlyChecked = _checkedRunIds.Contains(run.Id);
            bool newState = !isCurrentlyChecked;

            if (newState)
                _checkedRunIds.Add(run.Id);
            else
                _checkedRunIds.Remove(run.Id);

            // Update cell value directly
            row.Cells["Select"].Value = newState;

            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            dataGridView1.Invalidate();
            UpdateSelectAllHeaderState();
        }

        private void DataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != 0) return;

            _isUpdatingRunCheckState = true;

            // Determine current selection state
            int visibleCount = 0;
            int checkedCount = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var run = row.DataBoundItem as FlowRun;
                if (run == null) continue;
                visibleCount++;
                if (_checkedRunIds.Contains(run.Id)) checkedCount++;
            }

            bool newState = checkedCount < visibleCount;

            // Apply to all rows
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var run = row.DataBoundItem as FlowRun;
                if (run == null) continue;

                if (newState)
                    _checkedRunIds.Add(run.Id);
                else
                    _checkedRunIds.Remove(run.Id);

                // Directly set the cell value
                row.Cells["Select"].Value = newState;
            }

            // Commit and refresh
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            dataGridView1.RefreshEdit();
            dataGridView1.InvalidateColumn(0);
            dataGridView1.Refresh();

            _isUpdatingRunCheckState = false;
            UpdateSelectAllHeaderState();
        }
        private void UpdateSelectAllHeaderState()
        {
            var selectCol = dataGridView1.Columns["Select"];
            if (selectCol == null) return;

            int visibleCount = 0;
            int checkedCount = 0;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var run = row.DataBoundItem as FlowRun;
                if (run == null) continue;
                visibleCount++;
                if (_checkedRunIds.Contains(run.Id))
                    checkedCount++;
            }

            // Store current font family and base size
            var baseFont = selectCol.HeaderCell.Style.Font ?? dataGridView1.Font;
            float normalSize = 12F;   
            float checkedAllSize = 9.5F; 

            if (visibleCount == 0 || checkedCount == 0)
            {
                selectCol.HeaderText = "☐";
                selectCol.HeaderCell.Style.Font = new Font(baseFont.FontFamily, normalSize, FontStyle.Regular);
            }
            else if (checkedCount == visibleCount)
            {
                selectCol.HeaderText = "☑";
                // Use smaller font for the checkmark box
                selectCol.HeaderCell.Style.Font = new Font(baseFont.FontFamily, checkedAllSize, FontStyle.Regular);
            }
            else
            {
                selectCol.HeaderText = "▣";
                selectCol.HeaderCell.Style.Font = new Font(baseFont.FontFamily, normalSize, FontStyle.Regular);
            }
        }

        /// <summary>
        /// Gets all checked runs across all pages
        /// </summary>
        public List<FlowRun> GetCheckedRuns()
        {
            var allRuns = _pagination?.AllRuns;
            if (allRuns == null) return new List<FlowRun>();

            return allRuns.Where(r => _checkedRunIds.Contains(r.Id)).ToList();
        }

        /// <summary>
        /// Clears all run selections
        /// </summary>
        public void ClearRunSelections()
        {
            _checkedRunIds.Clear();
            DataGridBinder.SyncCheckboxStates(dataGridView1, _checkedRunIds);
            UpdateSelectAllHeaderState();
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

        public List<Flow> GetSelectedFlows() =>
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
            ClearOutputsFilter();
            _triggerOutputsCache.Clear();
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
                    _triggerOutputsCache.Clear();
                    _checkedRunIds.Clear();
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
                    var fetchedRuns = new List<FlowRun>();
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

                    args.Result = new Tuple<List<FlowRun>, bool>(fetchedRuns, hasMore);
                },
                PostWorkCallBack = (args) =>
                {
                    _pagination.IsLoading = false;
                    if (args.Error != null) { ShowError(args.Error); UpdatePaginationUI(); return; }

                    var result = (Tuple<List<FlowRun>, bool>)args.Result;
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
            if (_pagination == null) return;
            DataGridBinder.BindFlowRuns(dataGridView1, _pagination.GetCurrentPage(), _checkedRunIds);
            UpdateSelectAllHeaderState();
            dataGridView1.ClearSelection();
            dataGridView1.Invalidate();      // Let RowPrePaint handle all colors
            DataGridBinder.BindFlowRuns(dataGridView1, _pagination.GetCurrentPage());
            ApplyColumnVisibility();
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
            if (_isDeepSearchActive) return;
            // CORRECTION : Protection contre double-clic et chargement
            if (!_pagination.CanGoPrevious() || _pagination.IsLoading) return;
            _pagination.CurrentPage--;
            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void tsbNext_Click(object sender, EventArgs e)
        {
            if (_isDeepSearchActive) return;
            // CORRECTION : Protection contre chargement
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
                    var fetchedRuns = new List<FlowRun>();
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

                    args.Result = new Tuple<List<FlowRun>, bool>(fetchedRuns, hasMore);
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

                    var result = (Tuple<List<FlowRun>, bool>)args.Result;
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

        private void RefreshHistory()
        {
            var selectedFlows = GetSelectedFlows();
            if (selectedFlows.Count == 0) return;

            var (fromDate, toDate, status) = GetFilterValues();
            var flowIds = selectedFlows.Select(f => f.Id).ToList();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Refreshing history...",
                Work = (worker, args) =>
                {
                    int total = _dataverseService.GetTotalFlowRunsCount(flowIds, fromDate, toDate, status);
                    args.Result = total;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        ShowError(args.Error);
                        return;
                    }

                    _pagination.Reset();
                    _pagination.TotalServerCount = (int)args.Result;
                    _flowSkipTokens.Clear();
                    _checkedRunIds.Clear();
                    FetchPage(selectedFlows, fromDate, toDate, status, isNextPage: false);
                }
            });
        }

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

            // Ignore clicks on the Select checkbox column — let the checkbox handle itself
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Select")
                return;

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
            if (e.RowIndex < 0) return;

            var column = dataGridView1.Columns[e.ColumnIndex];

            // === STATUS COLUMN (existing color formatting) ===
            if (column.DataPropertyName != "Status") return;

            string status = e.Value?.ToString();
            if (string.IsNullOrWhiteSpace(status)) return;
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

        private void tsbCompareRuns_Click(object sender, EventArgs e)
        {
            // Use checkbox-selected runs instead of DataGridView.SelectedRows
            var checkedRuns = GetCheckedRuns();

            if (checkedRuns.Count != 2)
            {
                MessageBox.Show("Please check exactly two flow runs to compare them side-by-side.",
                    "Select Two Runs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var run1 = checkedRuns[0];
            var run2 = checkedRuns[1];

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

        // SUPPRIMER complètement ces méthodes :
        // - tbDeepSearch_KeyDown
        // - btnDeepSearch_Click  
        // - btnClearDeepSearch_Click (ancienne version)
        // - PerformDeepSearch

        // AJOUTER ces nouvelles méthodes :

        private void TbDeepSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyLocalFilter();
        }

        private void BtnClearLocalFilter_Click(object sender, EventArgs e)
        {
            tbDeepSearch.Text = "";
            lblDeepSearchStatus.Text = "";
            ShowCurrentPage();
            UpdatePaginationUI();
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
            var selectedRuns = GetCheckedRuns();

            if (selectedRuns.Count == 0)
            {
                MessageBox.Show(
                    "Please check at least one flow run to resubmit.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

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

                    // Update status label (optional)
                    lblDeepSearchStatus.Text = $"✔ {result.Success} run(s) resubmitted, ✖ {result.Failed} failed.";
                    lblDeepSearchStatus.ForeColor = result.Failed > 0 ? Color.DarkOrange : Color.Green;

                    // Show summary message box
                    string message = $"Successfully resubmitted: {result.Success}\nFailed: {result.Failed}";
                    if (result.Errors.Count > 0)
                        message += "\n\nErrors:\n- " + string.Join("\n- ", result.Errors);

                    MessageBox.Show(message, "Bulk Resubmit Completed",
                        MessageBoxButtons.OK, result.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                    // --- Refresh the grid to show updated run history ---
                    RefreshHistory();
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

        #region Theme Management
        private void InitializeTheme()
        {
            // Apply saved theme (defaults to light if never set)
            ThemeManager.Apply(this, _settings?.DarkMode ?? false);
            UpdateDarkModeButtonText();
        }

        private void ToggleDarkMode(object sender, EventArgs e)
        {
            bool newMode = !ThemeManager.IsDarkMode;
            _settings.DarkMode = newMode;
            ThemeManager.Apply(this, newMode);
            UpdateDarkModeButtonText();
        }

        private void UpdateDarkModeButtonText()
        {
            if (ThemeManager.IsDarkMode)
            {
                tsbDarkMode.Text = "Light Mode";
                tsbDarkMode.Image = Properties.Resources.sunny_16dp_FFFF55_FILL0_wght400_GRAD0_opsz20;

                tsbDarkMode.BackColor = Color.FromArgb(45, 45, 48);
                tsbDarkMode.ForeColor = Color.White;
            }
            else
            {
                tsbDarkMode.Text = "Dark Mode";
                tsbDarkMode.Image = Properties.Resources.moon_stars_16dp_1F1F1F_FILL0_wght400_GRAD0_opsz20;

                tsbDarkMode.BackColor = Color.FromArgb(240, 240, 240);
                tsbDarkMode.ForeColor = Color.Black;
            }

            tsbDarkMode.Owner?.Invalidate();
        }
        #endregion

        public List<FlowRun> GetAllRuns() => _pagination?.AllRuns?.ToList() ?? new List<FlowRun>();

        public JObject GetTriggerOutputsForRun(FlowRun run)
        {
            if (run == null) return null;

            if (_triggerOutputsCache.TryGetValue(run.Id, out JObject cached))
                return cached;

            var flow = _currentFlows.FirstOrDefault(f =>
                (!string.IsNullOrEmpty(run.FlowId) && f.Id.Equals(run.FlowId, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(run.FlowName) && f.DisplayName.Equals(run.FlowName, StringComparison.OrdinalIgnoreCase)));

            if (flow == null) return null;

            try
            {
                var client = _flowClientFactory.Create();
                var outputs = client.GetTriggerOutputs(flow.Id, run.Id);

                if (outputs != null)
                {
                    lock (_triggerOutputsCacheLock) { _triggerOutputsCache[run.Id] = outputs; }
                    run.TriggerOutputs = outputs;
                }

                return outputs;
            }
            catch
            {
                return null;
            }
        }

        public void ApplyOutputsFilter(ConditionGroup filter, int maxRuns = 0)
        {
            _currentTriggerFilter = filter;
            _isTriggerFilterActive = true;

            System.Diagnostics.Debug.WriteLine("=== APPLYING UNIFIED FILTER ===");
            System.Diagnostics.Debug.WriteLine($"Group: {filter.GroupOperator}");
            foreach (var c in filter.FilterConditions)
            {
                var target = c.Target == FilterTarget.Trigger ? "TRIGGER" : $"ACTION:{c.ActionName}";
                System.Diagnostics.Debug.WriteLine($"  - [{target}] {c.Attribute} {c.Operator} '{c.Value}'");
            }
            System.Diagnostics.Debug.WriteLine("=========================");

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Applying output filter...",
                Work = (worker, args) =>
                {
                    var allRuns = _pagination?.AllRuns ?? new List<FlowRun>();
                    var runsToProcess = (maxRuns > 0 && maxRuns < allRuns.Count)
                        ? allRuns.Take(maxRuns).ToList() : allRuns;

                   

                    var matching = new List<FlowRun>();
                    var lockObj = new object();
                    int processed = 0;
                    int total = runsToProcess.Count;

                    Parallel.ForEach(runsToProcess, new ParallelOptions { MaxDegreeOfParallelism = 10 }, run =>
                    {
                        if (worker.CancellationPending) return;

                        bool isMatch = EvaluateRunAgainstFilter(run, filter, out string debugLog);

                        lock (lockObj)
                        {
                            if (isMatch)
                                matching.Add(run);
                            processed++;
                            if (total > 0 && (processed % 5 == 0 || processed == total))
                                worker.ReportProgress((int)((double)processed / total * 100));
                        }
                    });

                    if (worker.CancellationPending) { args.Cancel = true; return; }
                    args.Result = new { Matching = matching.OrderByDescending(r => r.StartDate).ToList(), TotalProcessed = total };

                    args.Result = new
                    {
                        Matching = matching.OrderByDescending(r => r.StartDate).ToList(),
                        TotalProcessed = total
                    };
                },
                ProgressChanged = (args) =>
                {
                    lblDeepSearchStatus.Text = $"Scanning outputs... {args.ProgressPercentage}%";
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null) { ShowError(args.Error); return; }
                    if (args.Cancelled) { lblDeepSearchStatus.Text = "Filter cancelled."; return; }

                    dynamic result = args.Result;
                    var matchingRuns = result.Matching as List<FlowRun>;
                    int processedCount = result.TotalProcessed;

                    lblDeepSearchStatus.Text = $"Output filter: {matchingRuns.Count} match(es) (scanned {processedCount})";
                    gbFlowRuns.Text = $"Flow Runs — {matchingRuns.Count} filtered result(s)";
                    DataGridBinder.BindFlowRuns(dataGridView1, matchingRuns);
                }
            });
        }

        private bool EvaluateRunAgainstFilter(FlowRun run, ConditionGroup filter, out string debugLog)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== UNIFIED OUTPUT FILTER EVALUATION ===");

            if (filter?.FilterConditions == null || filter.FilterConditions.Count == 0)
            {
                debugLog = "No conditions => MATCH";
                return true;
            }

            var triggerConditions = filter.FilterConditions.Where(c => c.Target == FilterTarget.Trigger).ToList();
            var actionConditions = filter.FilterConditions.Where(c => c.Target == FilterTarget.Action).ToList();
            var actionGroups = actionConditions.GroupBy(c => c.ActionName, StringComparer.OrdinalIgnoreCase).ToList();

            // Get trigger outputs if needed
            JObject triggerOutputs = null;
            if (triggerConditions.Count > 0)
            {
                triggerOutputs = GetTriggerOutputsForRun(run);
            }

            // Get raw actions if needed
            JArray runActionsRaw = null;
            if (actionGroups.Count > 0)
            {
                var flow = _currentFlows.FirstOrDefault(f =>
                    (!string.IsNullOrEmpty(run.FlowId) && f.Id.Equals(run.FlowId, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(run.FlowName) && f.DisplayName.Equals(run.FlowName, StringComparison.OrdinalIgnoreCase)));

                if (flow != null)
                {
                    try
                    {
                        var client = _flowClientFactory.Create();
                        var raw = client.GetRunActionsRaw(flow.Id, run.Id);
                        runActionsRaw = raw["value"] as JArray;
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"Failed to get run actions: {ex.Message}");
                    }
                }
            }

            bool finalResult;
            if (filter.GroupOperator == GroupOperator.And)
                finalResult = EvaluateAndMixed(triggerOutputs, runActionsRaw, triggerConditions, actionGroups, sb);
            else
                finalResult = EvaluateOrMixed(triggerOutputs, runActionsRaw, triggerConditions, actionGroups, sb);

            sb.AppendLine($"FINAL RESULT: {finalResult}");
            debugLog = sb.ToString();
            return finalResult;
        }

        private bool EvaluateAndMixed(JObject triggerOutputs, JArray runActionsRaw,
            List<FilterCondition> triggerConditions, List<IGrouping<string, FilterCondition>> actionGroups, StringBuilder sb)
        {
            foreach (var condition in triggerConditions)
            {
                bool result = TriggerOutputFilterEvaluator.EvaluateSingle(triggerOutputs, condition, out string reason);
                sb.AppendLine($"[TRIGGER AND] {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");
                if (!result) return false;
            }

            foreach (var group in actionGroups)
            {
                var actionName = group.Key;
                JObject actionOutputs = GetActionOutputsFromRaw(runActionsRaw, actionName);

                foreach (var condition in group)
                {
                    bool result = ActionOutputFilterEvaluator.EvaluateSingle(actionOutputs, condition, out string reason);
                    sb.AppendLine($"[ACTION AND] [{actionName}] {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");
                    if (!result) return false;
                }
            }

            return true;
        }

        private bool EvaluateOrMixed(JObject triggerOutputs, JArray runActionsRaw,
            List<FilterCondition> triggerConditions, List<IGrouping<string, FilterCondition>> actionGroups, StringBuilder sb)
        {
            foreach (var condition in triggerConditions)
            {
                bool result = TriggerOutputFilterEvaluator.EvaluateSingle(triggerOutputs, condition, out string reason);
                sb.AppendLine($"[TRIGGER OR] {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");
                if (result) return true;
            }

            foreach (var group in actionGroups)
            {
                var actionName = group.Key;
                JObject actionOutputs = GetActionOutputsFromRaw(runActionsRaw, actionName);

                foreach (var condition in group)
                {
                    bool result = ActionOutputFilterEvaluator.EvaluateSingle(actionOutputs, condition, out string reason);
                    sb.AppendLine($"[ACTION OR] [{actionName}] {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");
                    if (result) return true;
                }
            }

            return false;
        }

        private JObject GetActionOutputsFromRaw(JArray actions, string actionName)
        {
            if (actions == null) return null;

            // Try exact match first
            var actionObj = actions.FirstOrDefault(a =>
                a["name"]?.ToString().Equals(actionName, StringComparison.OrdinalIgnoreCase) == true);

            // If not found, try ignoring spaces/parentheses/underscores
            if (actionObj == null)
            {
                string normalizedSearch = actionName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("_", "").ToLower();
                actionObj = actions.FirstOrDefault(a =>
                {
                    string normalizedApi = a["name"]?.ToString().Replace(" ", "").Replace("(", "").Replace(")", "").Replace("_", "").ToLower();
                    return normalizedApi == normalizedSearch;
                });
            }

            if (actionObj == null) return null;
            var props = actionObj["properties"];
            if (props == null) return null;

            // --- Merge outputs and inputs ---
            JObject outputsObj = null;
            JObject inputsObj = null;

            // Try outputs
            var outputs = props["outputs"];
            if (outputs is JObject jOut) outputsObj = jOut;
            else
            {
                var outputsLink = props["outputsLink"]?["uri"]?.ToString();
                if (!string.IsNullOrEmpty(outputsLink))
                {
                    try
                    {
                        var client = _flowClientFactory.Create();
                        outputsObj = JObject.Parse(client.GetContentFromLink(outputsLink));
                    }
                    catch { }
                }
            }

            // Try inputs
            // Get inputs - prefer direct token
            if (props["inputs"] is JObject jInDirect)
                inputsObj = jInDirect;
            else
            {
                var inputsLink = props["inputsLink"]?["uri"]?.ToString();
                if (!string.IsNullOrEmpty(inputsLink))
                {
                    try
                    {
                        var client = _flowClientFactory.Create();
                        inputsObj = JObject.Parse(client.GetContentFromLink(inputsLink));
                    }
                    catch { }
                }
            }

            // Merge: start with empty, then inputs, then outputs (outputs take precedence)
            var merged = new JObject();
            if (inputsObj != null)
                merged.Merge(inputsObj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            if (outputsObj != null)
                merged.Merge(outputsObj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

            System.Diagnostics.Debug.WriteLine($"Merged action outputs for {actionName}: {merged.ToString()}");
            return merged;
        }

        public void ClearOutputsFilter()
        {
            _currentTriggerFilter = null;
            _isTriggerFilterActive = false;
            lblDeepSearchStatus.Text = "";
            gbFlowRuns.Text = "Flow Runs";
            ShowCurrentPage();
            UpdatePaginationUI();
        }

        private void btnTriggerFilter_Click(object sender, EventArgs e)
        {
            var selectedFlows = GetSelectedFlows();
            if (selectedFlows.Count == 0)
            {
                MessageBox.Show("Please select at least one flow first.", "No Flows Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var allRuns = _pagination?.AllRuns;
            if (allRuns == null || allRuns.Count == 0)
            {
                MessageBox.Show("Please fetch run history first before applying a filter.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new Forms.OutputsFilterForm(this))
            {
                form.ShowDialog(this);
            }
        }

        private void btnClearTriggerFilter_Click(object sender, EventArgs e)
        {
            ClearOutputsFilter();
        }

        public IFlowClient CreateFlowClient()
        {
            return _flowClientFactory?.Create();
        }

        public int GetCurrentPageSize()
        {
            return _pagination?.PageSize ?? 50;
        }

        private void SyncAllCheckboxCells(bool newState)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var cell = row.Cells["Select"];
                if (cell != null)
                {
                    // Force the cell value to match the bound property
                    cell.Value = newState;
                }
            }
        }
    }
}