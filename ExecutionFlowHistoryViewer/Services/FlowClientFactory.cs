using ExecutionFlowHistoryViewer.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowClientFactory : IFlowClientFactory
    {
        private readonly IAuthenticationService _authService;
        private readonly string _environmentId;

        public FlowClientFactory(IAuthenticationService authService, string environmentId)
        {
            _authService = authService;
            _environmentId = environmentId;
        }

        public IFlowClient Create()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var scopes = new[] { "https://service.flow.microsoft.com/.default" };
            string token = _authService.GetAccessToken(scopes);

            return new FlowClient(_environmentId, token, "https://api.flow.microsoft.com");
        }
    }
}
