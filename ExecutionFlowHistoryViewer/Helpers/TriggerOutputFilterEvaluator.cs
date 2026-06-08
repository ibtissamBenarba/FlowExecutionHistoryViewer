using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public static class TriggerOutputFilterEvaluator
    {
        public static bool EvaluateGroup(JObject triggerOutputs, ConditionGroup group, out string debugLog)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== FILTER EVALUATION ===");

            if (group?.FilterConditions == null || group.FilterConditions.Count == 0)
            {
                sb.AppendLine("No conditions => MATCH (pass-through)");
                debugLog = sb.ToString();
                return true;
            }

            sb.AppendLine($"Group Operator: {group.GroupOperator}");
            sb.AppendLine($"Conditions count: {group.FilterConditions.Count}");
            sb.AppendLine();

            bool finalResult;
            if (group.GroupOperator == GroupOperator.And)
                finalResult = EvaluateAnd(triggerOutputs, group, sb);
            else
                finalResult = EvaluateOr(triggerOutputs, group, sb);

            sb.AppendLine();
            sb.AppendLine($"FINAL RESULT: {finalResult}");
            sb.AppendLine("========================");

            debugLog = sb.ToString();
            return finalResult;
        }

        private static bool EvaluateAnd(JObject triggerOutputs, ConditionGroup group, StringBuilder sb)
        {
            // AND: every condition must be true. One false => false.
            foreach (var condition in group.FilterConditions)
            {
                bool result = EvaluateSingle(triggerOutputs, condition, out string reason);
                sb.AppendLine($"[AND] {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");

                if (!result)
                    return false;
            }
            return true;
        }

        private static bool EvaluateOr(JObject triggerOutputs, ConditionGroup group, StringBuilder sb)
        {
            // OR: at least one condition must be true. One true => true.
            foreach (var condition in group.FilterConditions)
            {
                bool result = EvaluateSingle(triggerOutputs, condition, out string reason);
                sb.AppendLine($"[OR]  {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");

                if (result)
                    return true;
            }
            return false;
        }

        private static bool EvaluateSingle(JObject triggerOutputs, FilterCondition condition, out string reason)
        {
            reason = "ok";

            if (triggerOutputs == null)
            {
                reason = "triggerOutputs is null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(condition.Attribute))
            {
                reason = "attribute is empty";
                return false;
            }

            // Power Automate Dataverse triggers wrap entity data inside "body"
            JToken token = triggerOutputs["body"]?[condition.Attribute] ?? triggerOutputs[condition.Attribute];

            if (token == null || token.Type == JTokenType.Null)
            {
                reason = $"attribute '{condition.Attribute}' not found";
                return false;
            }

            string actualValue = token.ToString();
            string compareValue = condition.Value ?? string.Empty;

            switch (condition.Operator)
            {
                case TriggerOutputOperator.Equals:
                    reason = $"'{actualValue}' == '{compareValue}'";
                    return string.Equals(actualValue, compareValue, StringComparison.OrdinalIgnoreCase);

                case TriggerOutputOperator.NotEquals:
                    reason = $"'{actualValue}' != '{compareValue}'";
                    return !string.Equals(actualValue, compareValue, StringComparison.OrdinalIgnoreCase);

                case TriggerOutputOperator.Contains:
                    reason = $"'{actualValue}'.Contains('{compareValue}')";
                    return actualValue.IndexOf(compareValue, StringComparison.OrdinalIgnoreCase) >= 0;

                case TriggerOutputOperator.NotContains:
                    reason = $"!('{actualValue}'.Contains('{compareValue}'))";
                    return actualValue.IndexOf(compareValue, StringComparison.OrdinalIgnoreCase) < 0;

                case TriggerOutputOperator.StartsWith:
                    reason = $"'{actualValue}'.StartsWith('{compareValue}')";
                    return actualValue.StartsWith(compareValue, StringComparison.OrdinalIgnoreCase);

                case TriggerOutputOperator.EndsWith:
                    reason = $"'{actualValue}'.EndsWith('{compareValue}')";
                    return actualValue.EndsWith(compareValue, StringComparison.OrdinalIgnoreCase);

                case TriggerOutputOperator.GreaterThan:
                    if (double.TryParse(actualValue, out double a1) && double.TryParse(compareValue, out double c1))
                    {
                        reason = $"{a1} > {c1}";
                        return a1 > c1;
                    }
                    reason = "non-numeric comparison";
                    return string.Compare(actualValue, compareValue, StringComparison.OrdinalIgnoreCase) > 0;

                case TriggerOutputOperator.LessThan:
                    if (double.TryParse(actualValue, out double a2) && double.TryParse(compareValue, out double c2))
                    {
                        reason = $"{a2} < {c2}";
                        return a2 < c2;
                    }
                    reason = "non-numeric comparison";
                    return string.Compare(actualValue, compareValue, StringComparison.OrdinalIgnoreCase) < 0;

                case TriggerOutputOperator.IsEmpty:
                    reason = $"string.IsNullOrWhiteSpace('{actualValue}')";
                    return string.IsNullOrWhiteSpace(actualValue);

                case TriggerOutputOperator.IsNotEmpty:
                    reason = $"!string.IsNullOrWhiteSpace('{actualValue}')";
                    return !string.IsNullOrWhiteSpace(actualValue);

                default:
                    reason = "unknown operator";
                    return false;
            }
        }
    }
}