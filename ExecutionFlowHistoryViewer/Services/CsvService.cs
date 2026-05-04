using System.Collections.Generic;
using System.IO;
using System.Text;
using ExecutionFlowHistoryViewer.Models;

namespace ExecutionFlowHistoryViewer.Services
{
    public static class CsvService
    {
        public static void Export(List<FlowRun> flowRuns, string filePath)
        {
            var sb = new StringBuilder();
            // headers
            sb.AppendLine("Flow Name;Status;Start Date;End Date;Run ID");

            foreach (var run in flowRuns)
            {
                sb.AppendLine($"{run.FlowName};{run.Status};{run.StartDate};{run.EndDate};{run.Id}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}