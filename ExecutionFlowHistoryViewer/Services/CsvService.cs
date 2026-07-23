using ExecutionFlowHistoryViewer.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExecutionFlowHistoryViewer.Services
{
    public static class CsvService
    {
        public static void Export(
            List<FlowRun> flowRuns,
            string filePath,
            string delimiter,
            Encoding encoding,
            bool includeHeaders)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var sb = new StringBuilder();
            string[] headers = { "Flow Name", "Status", "Start Date", "End Date", "Run ID" };

            if (includeHeaders)
            {
                sb.AppendLine(string.Join(delimiter, headers.Select(h => EscapeField(h, delimiter))));
            }

            foreach (var run in flowRuns)
            {
                var fields = new[]
                {
                    run.FlowName ?? string.Empty,
                    run.Status ?? string.Empty,
                    run.StartDate.ToString(),
                    run.EndDate.ToString(),
                    run.Id ?? string.Empty
                };

                sb.AppendLine(string.Join(delimiter, fields.Select(f => EscapeField(f, delimiter))));
            }

            File.WriteAllText(filePath, sb.ToString(), encoding);
        }

        private static string EscapeField(string field, string delimiter)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            if (field.Contains(delimiter) || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}