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
        public static void Export(List<FlowRun> flowRuns, string filePath)
        {
            // Définir le contexte de licence pour EPPlus
            ExcelPackage.License.SetNonCommercialOrganization("MyXrmToolBoxPlugin");

            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Flow History");

                // En-têtes
                sheet.Cells[1, 1].Value = "Flow Name";
                sheet.Cells[1, 2].Value = "Status";
                sheet.Cells[1, 3].Value = "Start Date";
                sheet.Cells[1, 4].Value = "End Date";
                sheet.Cells[1, 5].Value = "Run ID";

                // Remplissage des données
                for (int i = 0; i < flowRuns.Count; i++)
                {
                    var run = flowRuns[i];
                    int row = i + 2;

                    sheet.Cells[row, 1].Value = run.FlowName; // Assurez-vous que ces propriétés existent dans votre modèle
                    sheet.Cells[row, 2].Value = run.Status;
                    sheet.Cells[row, 3].Value = run.StartDate;
                    sheet.Cells[row, 4].Value = run.EndDate;
                    sheet.Cells[row, 5].Value = run.Id;

                    // Formatage Date
                    sheet.Cells[row, 3, row, 4].Style.Numberformat.Format = "dd/mm/yyyy hh:mm:ss";
                }

                // Création d'un tableau Excel (Table)
                if (flowRuns.Count > 0)
                {
                    var range = sheet.Cells[1, 1, flowRuns.Count + 1, 5];
                    var table = sheet.Tables.Add(range, "HistoryTable");
                    table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;

                    // Mise en forme conditionnelle pour le Statut (Colonne B)
                    var statusRange = sheet.Cells[2, 2, flowRuns.Count + 1, 2];

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

                // Sauvegarde
                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }
        }
    }
}