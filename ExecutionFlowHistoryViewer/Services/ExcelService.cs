using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ExecutionFlowHistoryViewer.Models;

namespace ExecutionFlowHistoryViewer.Services
{
    public static class ExcelService
    {
        public static void Export(List<FlowRun> flowRuns, string filePath, bool includeHeaders)
        {
            ExcelPackage.License.SetNonCommercialOrganization("MyXrmToolBoxPlugin");

            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Flow History");

                int dataStartRow = includeHeaders ? 2 : 1;
                int lastRow = flowRuns.Count + (includeHeaders ? 1 : 0);

                if (includeHeaders)
                {
                    sheet.Cells[1, 1].Value = "Flow Name";
                    sheet.Cells[1, 2].Value = "Status";
                    sheet.Cells[1, 3].Value = "Start Date";
                    sheet.Cells[1, 4].Value = "End Date";
                    sheet.Cells[1, 5].Value = "Run ID";
                }

                for (int i = 0; i < flowRuns.Count; i++)
                {
                    var run = flowRuns[i];
                    int row = i + dataStartRow;

                    sheet.Cells[row, 1].Value = run.FlowName;
                    sheet.Cells[row, 2].Value = run.Status;
                    sheet.Cells[row, 3].Value = run.StartDate;
                    sheet.Cells[row, 4].Value = run.EndDate;
                    sheet.Cells[row, 5].Value = run.Id;

                    sheet.Cells[row, 3, row, 4].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                }

                if (flowRuns.Count > 0)
                {
                    var fullRange = sheet.Cells[1, 1, lastRow, 5];

                    if (includeHeaders)
                    {
                        var table = sheet.Tables.Add(fullRange, "HistoryTable");
                        table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
                    }

                    int statusStartRow = includeHeaders ? 2 : 1;
                    var statusRange = sheet.Cells[statusStartRow, 2, lastRow, 2];

                    var success = statusRange.ConditionalFormatting.AddEqual();
                    success.Formula = "\"Succeeded\"";
                    success.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    success.Style.Fill.BackgroundColor.Color = Color.LightGreen;

                    var failure = statusRange.ConditionalFormatting.AddEqual();
                    failure.Formula = "\"Failed\"";
                    failure.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    failure.Style.Fill.BackgroundColor.Color = Color.LightCoral;
                }

                sheet.Cells.AutoFitColumns();
                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }
        }
    }
}