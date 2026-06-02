using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.DTO
{
    public class FlowRunPropertiesDto
    {
        public string Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public FlowRunTriggerDto Trigger { get; set; }
    }
}
