using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExecutionFlowHistoryViewer.Services
{
    public class PaginationService : IPaginationService
    {
        private readonly List<FlowRun> _allRuns = new List<FlowRun>();
        private int _pageSize = 50;

        public IReadOnlyList<FlowRun> AllRuns => _allRuns.AsReadOnly();
        public int CurrentPage { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("PageSize must be greater than 0.", nameof(value));
                _pageSize = value;
            }
        }

        public bool HasMoreServerPages { get; set; }
        public bool IsLoading { get; set; }

        public int TotalServerCount { get; set; }

        public void Reset()
        {
            _allRuns.Clear();
            CurrentPage = 1;
            HasMoreServerPages = false;
            TotalServerCount = 0;
        }

        public void AppendRuns(IEnumerable<FlowRun> runs) => _allRuns.AddRange(runs);

        public List<FlowRun> GetCurrentPage()
        {
            int startIndex = (CurrentPage - 1) * PageSize;
            if (startIndex >= _allRuns.Count) return new List<FlowRun>();
            return _allRuns.Skip(startIndex).Take(PageSize).ToList();
        }

        public bool CanGoPrevious() => CurrentPage > 1;

        public bool CanGoNext() => (CurrentPage < TotalPages) || HasMoreServerPages;

        public int TotalPages => TotalServerCount > 0
            ? (int)Math.Ceiling((double)TotalServerCount / PageSize)
            : 1;

        public int TotalCachedPages => Math.Max(1, (int)Math.Ceiling((double)_allRuns.Count / PageSize));

        public string GetPageInfoText()
        {
            int startItem = TotalServerCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
            int endItem = Math.Min(CurrentPage * PageSize, TotalServerCount);
            return $"{startItem}-{endItem} of {TotalServerCount} items | Page {CurrentPage}/{TotalPages}";
        }
    }
}