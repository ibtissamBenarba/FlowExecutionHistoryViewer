using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowSafetyAnalyzer
    {
        private readonly GeminiService _ai;

        public FlowSafetyAnalyzer(GeminiService ai)
        {
            _ai = ai;
        }

        public string Analyze(string flowDefinitionJson, FlowRun failedRun,
                              FlowRunDetailDto details, FlowActionsResponseDto actions)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== FAILED RUN ===");
            sb.AppendLine($"Flow: {failedRun.FlowName}");
            sb.AppendLine($"Failed At: {failedRun.StartDate:g}");
            sb.AppendLine($"Status: {failedRun.Status}");

            if (actions?.Value != null)
            {
                var failed = actions.Value
                    .Where(a => a.Properties.Status?.ToString() == "Failed" || a.Properties?.Error != null)
                    .Take(5)
                    .ToList();

                sb.AppendLine($"Failed Actions Count: {failed.Count}");
                foreach (var a in failed)
                {
                    sb.AppendLine($"- Action: {a.Name} (type: {a.Type})");
                    if (a.Properties?.Error != null)
                        sb.AppendLine($"  Error: {a.Properties.Error.Code} - {a.Properties.Error.Message}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== CURRENT FLOW DEFINITION ===");
            if (!string.IsNullOrEmpty(flowDefinitionJson))
            {
                try
                {
                    var root = JObject.Parse(flowDefinitionJson);
                    var def = root["properties"]?["definition"];
                    if (def != null)
                    {
                        var acts = def["actions"] as JObject;
                        if (acts != null)
                        {
                            sb.AppendLine("Current actions in flow:");
                            foreach (var prop in acts.Properties().Take(30))
                            {
                                var t = prop.Value?["type"]?.ToString() ?? "unknown";
                                sb.AppendLine($"- {prop.Name}: {t}");
                            }
                        }
                        var trig = def["triggers"]?.First?.First;
                        if (trig != null)
                            sb.AppendLine($"Trigger type: {trig["type"]?.ToString() ?? "unknown"}");
                    }
                    else
                    {
                        sb.AppendLine(flowDefinitionJson.Substring(0, Math.Min(800, flowDefinitionJson.Length)));
                    }
                }
                catch
                {
                    sb.AppendLine("(Could not parse definition JSON)");
                }
            }
            else
            {
                sb.AppendLine("(Flow definition unavailable)");
            }

            sb.AppendLine();
            sb.AppendLine("=== QUESTION ===");
            sb.AppendLine("Is it safe to resubmit this failed run NOW?");
            sb.AppendLine("Rules:");
            sb.AppendLine("1. Transient errors (network, timeout, temporary auth, throttling, 502/503) = SAFE if flow unchanged.");
            sb.AppendLine("2. Structural errors (missing field, wrong expression, deleted item, schema mismatch) = UNSAFE if flow still has same flawed logic.");
            sb.AppendLine("3. If the failing action was MODIFIED or REMOVED in current definition, mention it.");
            sb.AppendLine();
            sb.AppendLine("Respond EXACTLY in this format:");
            sb.AppendLine("SAFE: YES or NO");
            sb.AppendLine("CONFIDENCE: 0-100");
            sb.AppendLine("REASON: <short 2-sentence explanation>");

            return _ai.Ask(sb.ToString());
        }
    }
}