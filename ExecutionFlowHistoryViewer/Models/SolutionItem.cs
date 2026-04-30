using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Models
{
    internal sealed class SolutionItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // Utile pour l'affichage direct dans une ComboBox ou ListBox
        public override string ToString() => Name;
    }
}
