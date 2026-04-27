using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Services;
using McTools.Xrm.Connection;
using Microsoft.Identity.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace ExecutionFlowHistoryViewer
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;
        // Cached MSAL client and connection state
        private IPublicClientApplication _pca;
        private bool _isPowerAutomateConnected = false;

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
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));

            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }

            // Fetch History is disabled until PA is connected
            btnFetchHistory.Enabled = false;
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsbSample_Click(object sender, EventArgs e)
        {
            // The ExecuteMethod method handles connecting to an
            // organization if XrmToolBox is not yet connected
            ExecuteMethod(GetAccounts);
        }

        private void GetAccounts()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting accounts",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("account")
                    {
                        TopCount = 50
                    });
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        MessageBox.Show($"Found {result.Entities.Count} accounts");
                    }
                }
            });
        }

        private void btnConnectPA_Click(object sender, EventArgs e)
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
                    // This triggers the first login (interactive only if needed)
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

        // ==================== BUTTON: Fetch History ====================
        private void btnFetchHistory_Click(object sender, EventArgs e)
        {
            if (cmbFlows.SelectedItem == null)
            {
                MessageBox.Show("Please select a flow from the list first!");
                return;
            }

            if (!_isPowerAutomateConnected)
            {
                MessageBox.Show("Please click 'Connect to Power Automate' first!",
                    "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedFlow = (Flow)cmbFlows.SelectedItem;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Fetching history...",
                Work = (worker, args) =>
                {
                    // Reuses cached _pca — token is acquired silently, no popup
                    var client = CreateFlowClient();
                    args.Result = client.GetFlowRuns(selectedFlow.Id);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        // If token expired or was revoked, force reconnect
                        if (args.Error.Message.Contains("401") || args.Error.Message.Contains("Unauthorized"))
                        {
                            _isPowerAutomateConnected = false;
                            btnFetchHistory.Enabled = false;
                            MessageBox.Show("Your Power Automate session expired. Please connect again.",
                                "Session Expired", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        return;
                    }

                    dataGridView1.DataSource = (List<FlowRun>)args.Result;
                }
            });
        }

        // ==================== AUTHENTICATION ====================
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

            string regionalUrl = "https://france.api.flow.microsoft.com";
            return new FlowClient(envId, token, regionalUrl);
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
            catch (MsalUiRequiredException) { /* Fallback to interactive */ }

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

        // ==================== LOAD FLOWS (Dataverse) ====================
        private void btnLoadFlows_Click(object sender, EventArgs e)
        {
            if (Service == null) return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Flows from Dataverse...",
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
                        }
                    };

                    args.Result = Service.RetrieveMultiple(query);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.Message);
                        return;
                    }

                    var results = (EntityCollection)args.Result;
                    cmbFlows.Items.Clear();

                    foreach (var entity in results.Entities)
                    {
                        cmbFlows.Items.Add(new Flow
                        {
                            Id = entity.Id.ToString(),
                            DisplayName = entity.GetAttributeValue<string>("name")
                        });
                    }
                }
            });
        }


        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            // Reset cached auth when switching organizations
            _pca = null;

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
            
        }

        

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        

        
    }
}