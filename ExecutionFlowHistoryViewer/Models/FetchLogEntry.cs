using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Models
{
    public class FetchLogEntry
    {
        public int PageNumber { get; set; }
        public string FlowId { get; set; }
        public string FlowName { get; set; }
        public double DurationMs { get; set; }
        public int RunsFetched { get; set; }
        public bool HasNextPage { get; set; }
        public string StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
