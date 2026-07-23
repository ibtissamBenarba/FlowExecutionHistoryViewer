using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Helpers;
using Newtonsoft.Json.Linq;
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
        FlowRunDetailDto GetRunDetails(string flowId, string runId);
        FlowActionsResponseDto GetRunActions(string flowId, string runId);
        JObject GetRunActionsRaw(string flowId, string runId);
        string GetContentFromLink(string linkUri);
        bool ResubmitRun(string flowId, string runId);
        string GetFlowDefinition(string flowId);
        JObject GetTriggerOutputs(string flowId, string runId);
        JObject GetActionOutputs(string flowId, string runId, string actionName);  
    }
}
