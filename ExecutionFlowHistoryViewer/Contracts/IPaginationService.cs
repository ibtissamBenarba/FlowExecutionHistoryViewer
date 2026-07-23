using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Contracts
{
    public interface IPaginationService
    {
        IReadOnlyList<FlowRun> AllRuns { get; }
        int CurrentPage { get; set; }
        int PageSize { get; set; }
        bool HasMoreServerPages { get; set; }
        bool IsLoading { get; set; }

        void Reset();
        void AppendRuns(IEnumerable<FlowRun> runs);
        List<FlowRun> GetCurrentPage();
        bool CanGoPrevious();
        bool CanGoNext();

        int TotalServerCount { get; set; }
        int TotalCachedPages { get; }
        int TotalPages { get; }
        string GetPageInfoText();
    }
}
