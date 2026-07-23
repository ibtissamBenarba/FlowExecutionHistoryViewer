using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Contracts
{
    public interface IDataverseService
    {
        List<SolutionItem> GetSolutions();
        List<Models.Flow> GetFlows(Guid? solutionId = null);
        int GetTotalFlowRunsCount(List<string> flowIds, DateTime from, DateTime to, string status);
        void UpdateFlowState(string flowId, bool enable);
    }
}
