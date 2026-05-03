using System;
using System.ComponentModel;

namespace ExecutionFlowHistoryViewer.Models
{
    public class FlowRun
    {
        [DisplayName("Nom du Flux")]
        public string FlowName { get; set; }

        [DisplayName("ID Exécution")]
        public string Id { get; set; }

        [DisplayName("Statut")]
        public string Status { get; set; }

        [DisplayName("Début")]
        public DateTime StartDate { get; set; }

        [DisplayName("Fin")]
        public DateTime EndDate { get; set; }

        [DisplayName("Durée")]
        public string Duration => (EndDate - StartDate).ToString(@"hh\:mm\:ss");

        [DisplayName("Run URL")]
        public string Url { get; set; }  
    }
}