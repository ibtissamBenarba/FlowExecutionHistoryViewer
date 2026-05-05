using ExecutionFlowHistoryViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public class FlowRunPageResult
    {
        public List<FlowRun> Runs { get; set; } = new List<FlowRun>();
        public bool HasMore { get; set; }
        public string NextSkipToken { get; set; }
    }
}
