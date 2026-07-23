using ExecutionFlowHistoryViewer.Contracts;
using McTools.Xrm.Connection;
using System;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowClientFactory : IFlowClientFactory
    {
        private readonly IAuthenticationService _authService;
        private readonly string _environmentId;

        public FlowClientFactory(IAuthenticationService authService, string environmentId, ConnectionDetail connectionDetail = null)
        {
            _authService = authService;
            _environmentId = environmentId;
            // connectionDetail no longer needed
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
            if (string.IsNullOrEmpty(_environmentId)) return null;

            string fullEnvId = _environmentId.Replace("-", "");
            if (fullEnvId.Length < 3) return null;

            string regionCode = fullEnvId.Substring(fullEnvId.Length - 2);
            string envIdWithoutRegion = fullEnvId.Substring(0, fullEnvId.Length - 2);
            return $"https://{envIdWithoutRegion}.{regionCode}.environment.api.powerplatform.com";
        }
    }
}