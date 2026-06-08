using ExecutionFlowHistoryViewer.Enumeration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Models
{
    public class FilterCondition
    {
        public string Attribute { get; set; }
        public TriggerOutputOperator Operator { get; set; }
        public string Value { get; set; }
    }
}