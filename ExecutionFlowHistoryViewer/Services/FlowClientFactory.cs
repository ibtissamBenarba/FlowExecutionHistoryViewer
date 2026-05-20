using ExecutionFlowHistoryViewer.Contracts;
using McTools.Xrm.Connection;
using System;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowClientFactory : IFlowClientFactory
    {
        private readonly IAuthenticationService _authService;
        private readonly string _environmentId;
        private readonly ConnectionDetail _connectionDetail;

        public FlowClientFactory(IAuthenticationService authService, string environmentId, ConnectionDetail connectionDetail = null)
        {
            _authService = authService;
            _environmentId = environmentId;
            _connectionDetail = connectionDetail;
        }

        public IFlowClient Create()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var scopes = new[] { "https://service.flow.microsoft.com/.default" };
            string token = _authService.GetAccessToken(scopes);

            string readApiUrl = "https://api.flow.microsoft.com";
            string resubmitApiUrl = DiscoverEnvironmentApiUrl() ?? readApiUrl;

            return new FlowClient(_environmentId, token, readApiUrl, resubmitApiUrl);
        }

        private string DiscoverEnvironmentApiUrl()
        {
            if (_connectionDetail == null) return null;

            // Get the org URL - e.g., "https://org123.crm12.dynamics.com/XRMServices/2011/Organization.svc"
            string orgServiceUrl = _connectionDetail.OrganizationServiceUrl ?? "";

            // Extract the base domain part
            string regionCode = ExtractRegionCode(orgServiceUrl);
            if (string.IsNullOrEmpty(regionCode)) return null;

            // Map to environment API region
            string envRegion = MapToEnvironmentRegion(regionCode);

            // Clean environment ID
            string cleanEnvId = _environmentId.Replace("-", "");
            cleanEnvId = cleanEnvId.Substring(0, cleanEnvId.Length - 2);

            return $"https://{cleanEnvId}.{envRegion}.environment.api.powerplatform.com";
        }

        private string ExtractRegionCode(string url)
        {
            try
            {
                var uri = new Uri(url);
                string host = uri.Host; // e.g., "org123.crm12.dynamics.com"

                // Extract the crmN part
                var parts = host.Split('.');
                foreach (var part in parts)
                {
                    if (part.StartsWith("crm", StringComparison.OrdinalIgnoreCase))
                        return part.ToLower();
                }
            }
            catch { }

            return null;
        }

        private string MapToEnvironmentRegion(string crmRegion)
        {
            switch (crmRegion)
            {
                case "crm": return "us";
                case "crm2": return "us";
                case "crm3": return "de";
                case "crm4": return "eu";
                case "crm5": return "apac";
                case "crm6": return "au";
                case "crm7": return "jp";
                case "crm8": return "in";
                case "crm9": return "uk";
                case "crm10": return "ca";
                case "crm11": return "br";
                case "crm12": return "fa";
                case "crm15": return "fa";
                case "crm16": return "ae";
                case "crm17": return "za";
                case "crm19": return "ch";
                case "crm20": return "no";
                case "crm21": return "kr";
                default: return "us";
            }
        }
    }
}