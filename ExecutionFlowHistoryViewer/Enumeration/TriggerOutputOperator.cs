using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Enumeration
{
    public enum TriggerOutputOperator
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        GreaterThan,
        LessThan,
        IsEmpty,
        IsNotEmpty
    }
}
