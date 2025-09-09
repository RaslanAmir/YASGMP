using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace YasGMP.Helpers
{
    public static class XlsxExporter
    {
        public static string WriteSheet<T>(IEnumerable<T> rows, string filePrefix, IEnumerable<(string Header, Func<T, object?> Selector)> columns)
        {
            string dir = CsvExportHelper.EnsureExportDirectory();
            string path = Path.Combine(dir, $"{filePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Export");

            int col = 1;
            foreach (var c in columns)
            {
                ws.Cell(1, col).Value = c.Header;
                ws.Cell(1, col).Style.Font.Bold = true;
                col++;
            }

            int row = 2;
            foreach (var item in rows ?? Array.Empty<T>())
            {
                col = 1;
                foreach (var c in columns)
                {
                    var v = c.Selector(item);
                    ws.Cell(row, col++).SetValue(v?.ToString() ?? string.Empty);
                }
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(path);
            return path;
        }

        public static void WriteSingleSheet(string filePath, string sheetName, IList<string> headers, IList<IList<string>> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Export" : sheetName);

            for (int i = 0; i < headers.Count; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i] ?? string.Empty;
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < headers.Count && c < row.Count; c++)
                {
                    ws.Cell(r + 2, c + 1).SetValue(row[c] ?? string.Empty);
                }
            }

            ws.Columns().AdjustToContents();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            wb.SaveAs(filePath);
        }
    }
}
