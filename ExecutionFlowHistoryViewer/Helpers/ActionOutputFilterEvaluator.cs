using ExecutionFlowHistoryViewer.Enumeration;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public static class ActionOutputFilterEvaluator
    {
        public static bool EvaluateGroup(JObject actionOutputs, ConditionGroup group, out string debugLog)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ACTION FILTER EVALUATION ===");

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
                finalResult = EvaluateAnd(actionOutputs, group, sb);
            else
                finalResult = EvaluateOr(actionOutputs, group, sb);

            sb.AppendLine();
            sb.AppendLine($"FINAL RESULT: {finalResult}");
            sb.AppendLine("========================");

            debugLog = sb.ToString();
            return finalResult;
        }

        private static bool EvaluateAnd(JObject actionOutputs, ConditionGroup group, StringBuilder sb)
        {
            foreach (var condition in group.FilterConditions)
            {
                bool result = EvaluateSingle(actionOutputs, condition, out string reason);
                sb.AppendLine($"[AND] {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");

                if (!result)
                    return false;
            }
            return true;
        }

        private static bool EvaluateOr(JObject actionOutputs, ConditionGroup group, StringBuilder sb)
        {
            foreach (var condition in group.FilterConditions)
            {
                bool result = EvaluateSingle(actionOutputs, condition, out string reason);
                sb.AppendLine($"[OR]  {condition.Attribute} {condition.Operator} '{condition.Value}' => {result} ({reason})");

                if (result)
                    return true;
            }
            return false;
        }

        public static bool EvaluateSingle(JObject actionOutputs, FilterCondition condition, out string reason)
        {
            reason = "ok";

            if (actionOutputs == null)
            {
                reason = "actionOutputs is null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(condition.Attribute))
            {
                reason = "attribute is empty";
                return false;
            }

            // --- DEBUG: dump the action outputs to Output window ---
            System.Diagnostics.Debug.WriteLine("=== EvaluateSingle ===");
            System.Diagnostics.Debug.WriteLine($"Attribute: {condition.Attribute}");
            System.Diagnostics.Debug.WriteLine($"actionOutputs: {actionOutputs.ToString()}");
            JToken token = null;

            // Try direct access
            token = actionOutputs[condition.Attribute];

            // Try "body" shortcut
            if (token == null || token.Type == JTokenType.Null)
                token = actionOutputs["body"]?[condition.Attribute];

            // Try nested path (supports dots and slashes)
            if (token == null || token.Type == JTokenType.Null)
                token = NavigateTokenPath(actionOutputs, condition.Attribute);

            // Try inside "parameters" (common for inputs)
            if (token == null || token.Type == JTokenType.Null)
            {
                var parameters = actionOutputs["parameters"] as JObject;
                if (parameters != null)
                    token = NavigateTokenPath(parameters, condition.Attribute);
            }

            string actualValue = token.ToString();
            string compareValue = condition.Value ?? string.Empty;

            switch (condition.Operator)
            {
                case FilterOperator.Equals:
                    reason = $"'{actualValue}' == '{compareValue}'";
                    return string.Equals(actualValue, compareValue, StringComparison.OrdinalIgnoreCase);

                case FilterOperator.NotEquals:
                    reason = $"'{actualValue}' != '{compareValue}'";
                    return !string.Equals(actualValue, compareValue, StringComparison.OrdinalIgnoreCase);

                case FilterOperator.Contains:
                    reason = $"'{actualValue}'.Contains('{compareValue}')";
                    return actualValue.IndexOf(compareValue, StringComparison.OrdinalIgnoreCase) >= 0;

                case FilterOperator.NotContains:
                    reason = $"!('{actualValue}'.Contains('{compareValue}'))";
                    return actualValue.IndexOf(compareValue, StringComparison.OrdinalIgnoreCase) < 0;

                case FilterOperator.StartsWith:
                    reason = $"'{actualValue}'.StartsWith('{compareValue}')";
                    return actualValue.StartsWith(compareValue, StringComparison.OrdinalIgnoreCase);

                case FilterOperator.EndsWith:
                    reason = $"'{actualValue}'.EndsWith('{compareValue}')";
                    return actualValue.EndsWith(compareValue, StringComparison.OrdinalIgnoreCase);

                case FilterOperator.GreaterThan:
                    if (double.TryParse(actualValue, out double a1) && double.TryParse(compareValue, out double c1))
                    {
                        reason = $"{a1} > {c1}";
                        return a1 > c1;
                    }
                    reason = "non-numeric comparison";
                    return string.Compare(actualValue, compareValue, StringComparison.OrdinalIgnoreCase) > 0;

                case FilterOperator.LessThan:
                    if (double.TryParse(actualValue, out double a2) && double.TryParse(compareValue, out double c2))
                    {
                        reason = $"{a2} < {c2}";
                        return a2 < c2;
                    }
                    reason = "non-numeric comparison";
                    return string.Compare(actualValue, compareValue, StringComparison.OrdinalIgnoreCase) < 0;

                case FilterOperator.IsEmpty:
                    reason = $"string.IsNullOrWhiteSpace('{actualValue}')";
                    return string.IsNullOrWhiteSpace(actualValue);

                case FilterOperator.IsNotEmpty:
                    reason = $"!string.IsNullOrWhiteSpace('{actualValue}')";
                    return !string.IsNullOrWhiteSpace(actualValue);

                default:
                    reason = "unknown operator";
                    return false;
            }
        }

        private static JToken NavigateTokenPath(JObject root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path)) return null;

            var parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            JToken current = root;

            foreach (var part in parts)
            {
                if (current is JObject obj)
                {
                    current = obj[part];
                    if (current == null) return null;
                }
                else if (current is JArray arr && int.TryParse(part, out int index))
                {
                    if (index >= 0 && index < arr.Count)
                        current = arr[index];
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            return current;
        }
    }
}