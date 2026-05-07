using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowHistoryService : IFlowHistoryService
    {
        private readonly IFlowClientFactory _clientFactory;

        public FlowHistoryService(IFlowClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public FlowRunPageResult FetchRuns(List<Flow> flows, DateTime fromDate, DateTime toDate,
            string status, bool isNextPage, Dictionary<string, string> flowSkipTokens, int pageSize)
        {
            var client = _clientFactory.Create();
            var pageResult = new FlowRunPageResult();

            foreach (var flow in flows)
            {
                string flowSkipToken = isNextPage && flowSkipTokens.ContainsKey(flow.Id)
                    ? flowSkipTokens[flow.Id]
                    : null;

                var result = client.GetFlowRuns(flow.Id, top: pageSize, skipToken: flowSkipToken);

                if (result.Runs?.Count == 0) continue;

                // Client-side filtering
                var filtered = result.Runs
                    .Where(r => r.StartDate >= fromDate && r.StartDate <= toDate)
                    .ToList();

                if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(r =>
                        string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                foreach (var run in filtered)
                {
                    run.FlowName = flow.DisplayName;
                    pageResult.Runs.Add(run);
                }

                if (result.HasMore)
                    flowSkipTokens[flow.Id] = result.NextSkipToken;
                else
                    flowSkipTokens.Remove(flow.Id);

                if (result.HasMore) pageResult.HasMore = true;
            }

            return pageResult;
        }
    }
}
