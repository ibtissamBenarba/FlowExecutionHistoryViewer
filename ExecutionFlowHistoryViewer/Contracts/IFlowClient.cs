using ExecutionFlowHistoryViewer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Contracts
{
    public interface IFlowClient
    {
        FlowRunPageResult GetFlowRuns(string flowId, int top = 100, string skipToken = null);
    }
}
