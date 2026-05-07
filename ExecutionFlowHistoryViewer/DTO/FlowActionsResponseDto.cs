using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.DTO
{
    public class FlowActionsResponseDto
    {
        public List<FlowActionDto> Value { get; set; }
    }

    public class FlowActionDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public FlowActionPropertiesDto Properties { get; set; }
    }

    public class FlowActionPropertiesDto
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public object Inputs { get; set; }
        public object Outputs { get; set; }
        public FlowActionErrorDto Error { get; set; }
    }

    public class FlowActionErrorDto
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
