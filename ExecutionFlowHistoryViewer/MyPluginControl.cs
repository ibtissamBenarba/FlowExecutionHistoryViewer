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

        private FlowClient CreateFlowClient()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var envId = ConnectionDetail.EnvironmentId.ToString();
            var scopes = new[] { "https://service.flow.microsoft.com/.default" };

            // MSAL Configuration
            var pca = PublicClientApplicationBuilder.Create("51f81489-12ee-4a9e-aaae-a2591f45987d")
                .WithAuthority($"https://login.microsoftonline.com/{ConnectionDetail.TenantId}")
                .WithRedirectUri("app://58145B91-0C36-4500-8554-080854F2AC97")
                .Build();

            string token = GetAccessToken(pca, scopes);

            // You can make this dynamic based on the region, but France is hardcoded here as per your request
            string regionalUrl = "https://france.api.flow.microsoft.com";

            return new FlowClient(envId, token, regionalUrl);
        }

        private void btnLoadFlows_Click(object sender, EventArgs e)
        {
            // 1. Verify we are connected to Dataverse
            if (Service == null) return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Flows from Dataverse...",
                Work = (worker, args) =>
                {
                    // 2. Query Dataverse for Cloud Flows (category 5)
                    var query = new QueryExpression("workflow")
                    {
                        ColumnSet = new ColumnSet("workflowid", "name"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                    {
                        new ConditionExpression("category", ConditionOperator.Equal, 5), // 5 = Cloud Flow
                        new ConditionExpression("type", ConditionOperator.Equal, 1)      // 1 = Definition
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

                    // 3. Fill the ComboBox with the results
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

        private void btnFetchHistory_Click(object sender, EventArgs e)
        {
            // Check if the user actually picked a flow first
            if (cmbFlows.SelectedItem == null)
            {
                MessageBox.Show("Please select a flow from the list first!");
                return;
            }

            // Get the selected Flow object
            var selectedFlow = (Flow)cmbFlows.SelectedItem;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Fetching history...",
                Work = (worker, args) =>
                {
                    // Use the same authentication logic you already have
                    var client = CreateFlowClient();

                    // HERE IS THE CHANGE: Use selectedFlow.Id instead of "304d85b8-..."
                    args.Result = client.GetFlowRuns(selectedFlow.Id);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error == null)
                    {
                        dataGridView1.DataSource = (List<FlowRun>)args.Result;
                    }
                }
            });
        }

        private string GetAccessToken(IPublicClientApplication pca, string[] scopes)
        {
            try
            {
                var accounts = pca.GetAccountsAsync().GetAwaiter().GetResult();
                var account = accounts.FirstOrDefault(a => a.Username.Equals(ConnectionDetail.UserName, StringComparison.OrdinalIgnoreCase));

                if (account != null)
                {
                    return pca.AcquireTokenSilent(scopes, account).ExecuteAsync().GetAwaiter().GetResult().AccessToken;
                }
            }
            catch (MsalUiRequiredException) { /* On continue vers l'interactif */ }

            // Pour l'interactif, on utilise une petite astuce pour ne pas bloquer le thread UI
            string interactiveToken = null;
            var task = Task.Run(async () =>
            {
                var result = await pca.AcquireTokenInteractive(scopes)
                    .WithLoginHint(ConnectionDetail.UserName)
                    .ExecuteAsync();
                interactiveToken = result.AccessToken;
            });

            task.Wait(); // On attend la fin de la tâche asynchrone
            return interactiveToken;
        }

        // Méthode d'aide pour gérer l'affichage de la fenêtre sur le bon thread
        private string ShowLoginWindow(IPublicClientApplication pca, string[] scopes)
        {
            string token = null;
            this.Invoke(new MethodInvoker(delegate
            {
                var result = pca.AcquireTokenInteractive(scopes)
                    .WithLoginHint(ConnectionDetail.UserName)
                    .WithParentActivityOrWindow(this.Handle)
                    .ExecuteAsync().GetAwaiter().GetResult();
                token = result.AccessToken;
            }));
            return token;
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

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            btnFetchHistory_Click(sender, e);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            btnLoadFlows_Click(sender, e);
        }
    }
}