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
        public FilterTarget Target { get; set; }
        public string ActionName { get; set; }
        public string Attribute { get; set; }
        public FilterOperator Operator { get; set; }
        public string Value { get; set; }
    }
}