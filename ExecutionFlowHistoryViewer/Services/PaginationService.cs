using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Services
{
    public class PaginationService : IPaginationService
    {
        private readonly List<FlowRun> _allRuns = new List<FlowRun>();

        public IReadOnlyList<FlowRun> AllRuns => _allRuns.AsReadOnly();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public bool HasMoreServerPages { get; set; }
        public bool IsLoading { get; set; }

        public void Reset()
        {
            _allRuns.Clear();
            CurrentPage = 1;
            HasMoreServerPages = false;
        }

        public void AppendRuns(IEnumerable<FlowRun> runs) => _allRuns.AddRange(runs);

        public List<FlowRun> GetCurrentPage()
        {
            int startIndex = (CurrentPage - 1) * PageSize;
            if (startIndex >= _allRuns.Count) return new List<FlowRun>();
            return _allRuns.Skip(startIndex).Take(PageSize).ToList();
        }

        public bool CanGoPrevious() => CurrentPage > 1;

        public bool CanGoNext() => (CurrentPage < TotalCachedPages) || HasMoreServerPages;

        public int TotalCachedPages => Math.Max(1, (int)Math.Ceiling((double)_allRuns.Count / PageSize));

        public string GetPageInfoText()
        {
            string info = $"Page {CurrentPage}/{TotalCachedPages} | Total cached: {_allRuns.Count}";
            if (HasMoreServerPages) info += " | More on server...";
            return info;
        }
    }
}
