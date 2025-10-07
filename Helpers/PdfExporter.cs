using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Represents the pdf exporter value.
    /// </summary>
    public static class PdfExporter
    {
        /// <summary>
        /// Executes the write table operation.
        /// </summary>
        public static string WriteTable<T>(IEnumerable<T> rows, string filePrefix, IEnumerable<(string Header, Func<T, object?> Selector)> columns, string? title = null)
        {
            string dir = CsvExportHelper.EnsureExportDirectory();
            string path = Path.Combine(dir, $"{filePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");

            rows ??= Array.Empty<T>();
            var rowsList = new List<T>(rows);

            // Configure QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header()
                        .Text(title ?? filePrefix)
                        .SemiBold().FontSize(14).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                    page.Content().Table(table =>
                    {
                        // Columns
                        var colArray = new List<(string Header, Func<T, object?> Selector)>(columns);
                        table.ColumnsDefinition(cols =>
                        {
                            for (int i = 0; i < colArray.Count; i++) cols.RelativeColumn();
                        });

                        // Header row
                        table.Header(header =>
                        {
                            int ci = 0;
                            foreach (var col in colArray)
                            {
                                header.Cell().Element(CellStyle).Text(col.Header).SemiBold();
                                ci++;
                            }
                            static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
                                => container.DefaultTextStyle(x => x.FontSize(10)).PaddingVertical(4).PaddingHorizontal(2);
                        });

                        // Data rows
                        foreach (var item in rowsList)
                        {
                            foreach (var col in colArray)
                            {
                                var v = col.Selector(item);
                                table.Cell().Element(c => c.PaddingVertical(2).PaddingHorizontal(2))
                                    .Text(v?.ToString() ?? string.Empty);
                            }
                        }
                    });

                    page.Footer().AlignRight().Text($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                });
            }).GeneratePdf(path);

            return path;
        }
        /// <summary>
        /// Executes the write simple text pdf operation.
        /// </summary>

        public static void WriteSimpleTextPdf(string filePath, string title, IEnumerable<string> lines)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(20);
                    p.Size(PageSizes.A4);
                    p.PageColor(QuestPDF.Helpers.Colors.White);
                    p.Header().Text(title).SemiBold().FontSize(14).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);
                    p.Content().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Consolas));
                        foreach (var line in (lines ?? Array.Empty<string>()))
                            text.Line(line ?? string.Empty);
                    });
                    p.Footer().AlignRight().Text($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                });
            }).GeneratePdf(filePath);
        }
    }
}
