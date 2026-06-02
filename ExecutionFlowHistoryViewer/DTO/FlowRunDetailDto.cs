using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.DTO
{
    public class FlowRunDetailDto
    {
        public string Name { get; set; }
        public FlowRunDetailPropertiesDto Properties { get; set; }
    }

    public class FlowRunDetailPropertiesDto
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public string CorrelationClientTrackingId { get; set; }
        public FlowRunTriggerDto Trigger { get; set; }
    }

    public class FlowRunTriggerDto
    {
        public string Name { get; set; }
        public FlowRunInputsOutputsLinkDto InputsLink { get; set; }
        public FlowRunInputsOutputsLinkDto OutputsLink { get; set; }
        public object Inputs { get; set; }
        public object Outputs { get; set; }
        public string Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class FlowRunInputsOutputsLinkDto
    {
        public string Uri { get; set; }
        public string ContentVersion { get; set; }
        public int ContentSize { get; set; }
    }
}
