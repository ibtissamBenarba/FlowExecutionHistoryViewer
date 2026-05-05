using ExecutionFlowHistoryViewer.Contracts;
using McTools.Xrm.Connection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ConnectionDetail _connectionDetail;
        private IPublicClientApplication _pca;

        public AuthenticationService(ConnectionDetail connectionDetail)
        {
            _connectionDetail = connectionDetail ?? throw new ArgumentNullException(nameof(connectionDetail));
        }

        public void Reset() => _pca = null;

        public string GetAccessToken(string[] scopes)
        {
            EnsureInitialized();

            try
            {
                var accounts = _pca.GetAccountsAsync().GetAwaiter().GetResult();
                var account = accounts.FirstOrDefault(a =>
                    a.Username.Equals(_connectionDetail.UserName, StringComparison.OrdinalIgnoreCase));

                if (account != null)
                {
                    return _pca.AcquireTokenSilent(scopes, account)
                        .ExecuteAsync().GetAwaiter().GetResult().AccessToken;
                }
            }
            catch (MsalUiRequiredException) { }

            // Interactive fallback
            string token = null;
            var task = Task.Run(async () =>
            {
                var result = await _pca.AcquireTokenInteractive(scopes)
                    .WithLoginHint(_connectionDetail.UserName)
                    .ExecuteAsync();
                token = result.AccessToken;
            });
            task.Wait();
            return token;
        }

        private void EnsureInitialized()
        {
            if (_pca != null) return;

            _pca = PublicClientApplicationBuilder
                .Create("51f81489-12ee-4a9e-aaae-a2591f45987d")
                .WithAuthority($"https://login.microsoftonline.com/{_connectionDetail.TenantId}")
                .WithRedirectUri("app://58145B91-0C36-4500-8554-080854F2AC97")
                .Build();
        }
    }
}
