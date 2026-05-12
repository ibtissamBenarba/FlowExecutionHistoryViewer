using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Contracts
{
    public interface IFlowHistoryService
    {
        FlowRunPageResult FetchRuns(List<Flow> flows, DateTime fromDate, DateTime toDate,
            string status, bool isNextPage, Dictionary<string, string> flowSkipTokens, int pageSize);
    }
}
