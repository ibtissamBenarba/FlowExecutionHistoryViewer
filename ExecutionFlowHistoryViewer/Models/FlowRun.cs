using System;
using System.ComponentModel;

namespace ExecutionFlowHistoryViewer.Models
{
    public class FlowRun
    {
        [Browsable(false)]
        public string FlowId { get; set; }

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
        public string Duration
        {
            get
            {
                var duration = EndDate - StartDate;

                // Less than 1 second
                if (duration.TotalSeconds < 1)
                {
                    return $"{duration.TotalMilliseconds:F0} ms";
                }

                // Less than 1 minute
                if (duration.TotalMinutes < 1)
                {
                    return $"{duration.Seconds}s {duration.Milliseconds}ms";
                }

                // Less than 1 hour
                if (duration.TotalHours < 1)
                {
                    return $"{duration.Minutes:D2}m {duration.Seconds:D2}s";
                }

                // 1 hour or more
                return $"{(int)duration.TotalHours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
            }
        }

        [DisplayName("Run URL")]
        public string Url { get; set; }

        public string TriggerName { get; set; }
    }
}