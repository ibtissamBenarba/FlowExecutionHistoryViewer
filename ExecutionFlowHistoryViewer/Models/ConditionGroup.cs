using ExecutionFlowHistoryViewer.Enumeration;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Models
{
    public class ConditionGroup
    {
        public GroupOperator GroupOperator { get; set; }
        public List<FilterCondition> FilterConditions { get; set; } = new List<FilterCondition>();
    }
}