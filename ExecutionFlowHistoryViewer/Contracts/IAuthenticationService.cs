using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Contracts
{
    public interface IAuthenticationService
    {
        string GetAccessToken(string[] scopes);
        void Reset();
    }
}
