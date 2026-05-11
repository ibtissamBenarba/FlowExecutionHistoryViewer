using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Models
{
    public class Flow
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }

        // This ensures the ComboBox shows the name, not "ExecutionFlowHistoryViewer.Models.Flow"
        public override string ToString() => DisplayName;
    }
}
