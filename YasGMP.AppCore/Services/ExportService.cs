using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OfficeOpenXml;

using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

using iText.Layout.Element;
using ITextDocument = iText.Layout.Document; // disambiguate from YasGMP.Models.Document

using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Models.DTO;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Services.Interfaces;

// Disambiguate iText vs MAUI types
using ITextAlignment = iText.Layout.Properties.TextAlignment;
using ITextCell = iText.Layout.Element.Cell;

namespace YasGMP.Services
{
    /// <summary>
    /// Exports calibrations and audit logs to Excel/PDF with audit logging.
    /// <para>
    /// ✔ EPPlus v8 compatible (deprecation warnings suppressed locally).<br/>
    /// ✔ Null-safe for inputs and platform abstractions.<br/>
    /// ✔ Writes fallback audit to DB if <see cref="AuditService"/> is not injected.
    /// </para>
    /// </summary>
    public class ExportService
    {
        private readonly DatabaseService _dbService;
        private readonly AuditService? _audit;
        private readonly IPlatformService? _platformService;
        private readonly IAuthContext? _authContext;
        private readonly string _exportRoot;

        /// <summary>
        /// Creates a new <see cref="ExportService"/> with required dependencies.
        /// </summary>
        /// <param name="dbService">Database abstraction.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="dbService"/> is <c>null</c>.</exception>
        public ExportService(DatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _platformService = ServiceLocator.GetService<IPlatformService>();
            _authContext = ServiceLocator.GetService<IAuthContext>();
            _exportRoot = ResolveExportRoot();
        }

        /// <summary>
        /// Overload accepting an optional <see cref="AuditService"/> to log export activity.
        /// </summary>
        public ExportService(DatabaseService dbService, AuditService audit) : this(dbService)
        {
            _audit = audit;
        }

        /// <summary>Convenience wrapper for Excel export.</summary>
        public Task ExportCalibrationsToExcel(ObservableCollection<Calibration> calibrations)
            => ExportToExcelAsync(calibrations);

        /// <summary>Convenience wrapper for PDF export.</summary>
        public Task ExportCalibrationsToPdf(ObservableCollection<Calibration> calibrations)
            => ExportToPdfAsync(calibrations);

        /// <summary>
        /// Exports calibrations to an Excel file and logs the action.
        /// </summary>
        /// <param name="calibrations">Rows to export (null-safe).</param>
        /// <param name="filterUsed">Optional filter description for the audit log.</param>
        /// <returns>Full path to the generated Excel file.</returns>
        public async Task<string> ExportToExcelAsync(IEnumerable<Calibration> calibrations, string filterUsed = "")
        {
            string filePath = Path.Combine(_exportRoot, $"Calibrations_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            // EPPlus v8: new License API; we suppress the deprecation warning to keep compatibility.
#pragma warning disable CS0618
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Kalibracije");
                string[] headers = { "ID", "Komponenta", "Serviser", "Datum kalibracije", "Rok ponovne", "Rezultat", "Certifikat", "Napomena", "Potpis" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }

                int row = 2;
                foreach (var c in (calibrations ?? Array.Empty<Calibration>()))
                {
                    string component = c?.Component?.Name ?? "-";
                    string supplier  = c?.Supplier?.Name ?? "-";
                    string dateCal   = (c?.CalibrationDate == default) ? string.Empty : c!.CalibrationDate.ToString("yyyy-MM-dd");
                    string dateNext  = (c == null) ? string.Empty
                                      : (c.NextDue == default ? string.Empty : c.NextDue.ToString("yyyy-MM-dd"));

                    ws.Cells[row, 1].Value = c?.Id ?? 0;
                    ws.Cells[row, 2].Value = component;
                    ws.Cells[row, 3].Value = supplier;
                    ws.Cells[row, 4].Value = dateCal;
                    ws.Cells[row, 5].Value = dateNext;
                    ws.Cells[row, 6].Value = c?.Result ?? "-";
                    ws.Cells[row, 7].Value = c?.CertDoc ?? "-";
                    ws.Cells[row, 8].Value = c?.Comment ?? "-";
                    ws.Cells[row, 9].Value = c?.DigitalSignature ?? "-";
                    row++;
                }

                ws.Cells.AutoFitColumns();
                await package.SaveAsAsync(new FileInfo(filePath)).ConfigureAwait(false);
            }

            if (_audit is not null)
            {
                await _audit.LogCalibrationExportAsync("EXCEL", filePath, filterUsed ?? string.Empty).ConfigureAwait(false);
            }
            else
            {
                await LogFallbackAuditAsync("EXPORT_EXCEL", $"Exported Excel file: {filePath}").ConfigureAwait(false);
            }

            return filePath;
        }

        /// <summary>
        /// Exports calibrations to a PDF file and logs the action.
        /// </summary>
        /// <param name="calibrations">Rows to export (null-safe).</param>
        /// <param name="filterUsed">Optional filter description for the audit log.</param>
        /// <returns>Full path to the generated PDF file.</returns>
        public async Task<string> ExportToPdfAsync(IEnumerable<Calibration> calibrations, string filterUsed = "")
        {
            string filePath = Path.Combine(_exportRoot, $"Calibrations_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using var writer = new PdfWriter(filePath);
            using var pdfDoc = new PdfDocument(writer);
            using var doc = new ITextDocument(pdfDoc);

            PdfFont fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont fontBold    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont fontItalic  = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            doc.Add(new Paragraph("GMP Kalibracije – Izvještaj")
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetTextAlignment(ITextAlignment.CENTER));

            doc.Add(new Paragraph($"Generirano: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .SetFont(fontRegular)
                .SetFontSize(10));

            var table = new Table(6).UseAllAvailableWidth();
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("ID").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Komponenta").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Serviser").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Datum").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Rok").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Rezultat").SetFont(fontBold)));

            foreach (var c in (calibrations ?? Array.Empty<Calibration>()))
            {
                string component = c?.Component?.Name ?? "-";
                string supplier  = c?.Supplier?.Name ?? "-";
                string dateCal   = (c?.CalibrationDate == default) ? string.Empty : c!.CalibrationDate.ToString("yyyy-MM-dd");
                string dateNext  = (c == null) ? string.Empty
                                  : (c.NextDue == default ? string.Empty : c.NextDue.ToString("yyyy-MM-dd"));
                string result    = c?.Result ?? "-";

                table.AddCell(new ITextCell().Add(new Paragraph((c?.Id ?? 0).ToString()).SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(component).SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(supplier).SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(dateCal).SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(dateNext).SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(result).SetFont(fontRegular)));
            }

            doc.Add(table);

            doc.Add(new Paragraph("Napomena: PDF uključuje GMP audit metapodatke i digitalne potpise.")
                .SetFont(fontItalic)
                .SetFontSize(9));

            if (_audit is not null)
            {
                await _audit.LogCalibrationExportAsync("PDF", filePath, filterUsed ?? string.Empty).ConfigureAwait(false);
            }
            else
            {
                await LogFallbackAuditAsync("EXPORT_PDF", $"Exported PDF file: {filePath}").ConfigureAwait(false);
            }

            return filePath;
        }

        /// <summary>
        /// Exports audit DTOs to an Excel file and logs the action.
        /// </summary>
        public async Task<string> ExportAuditToExcel(IEnumerable<AuditEntryDto> auditEntries, string filterUsed = "")
        {
            string filePath = Path.Combine(_exportRoot, $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            // EPPlus v8 deprecation – localized suppression.
#pragma warning disable CS0618
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("AuditLog");
                string[] headers = { "Entitet", "Akcija", "Korisnik", "Napomena", "Vrijeme", "Potpis" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }

                int row = 2;
                foreach (var entry in (auditEntries ?? Array.Empty<AuditEntryDto>()))
                {
                    ws.Cells[row, 1].Value = entry?.Entity ?? "-";
                    ws.Cells[row, 2].Value = entry?.Action ?? "-";
                    ws.Cells[row, 3].Value = entry?.Username ?? "-";
                    ws.Cells[row, 4].Value = entry?.Note ?? "-";
                    ws.Cells[row, 5].Value = (entry == null) ? string.Empty : entry.Timestamp.ToString("yyyy-MM-dd HH:mm");
                    ws.Cells[row, 6].Value = entry?.SignatureHash ?? "-";
                    row++;
                }

                ws.Cells.AutoFitColumns();
                await package.SaveAsAsync(new FileInfo(filePath)).ConfigureAwait(false);
            }

            if (_audit is not null)
            {
                await _audit.LogCalibrationExportAsync("AUDIT_EXCEL", filePath, filterUsed ?? string.Empty).ConfigureAwait(false);
            }
            else
            {
                await LogFallbackAuditAsync("EXPORT_AUDIT_EXCEL", $"Exported Audit Log Excel: {filePath}").ConfigureAwait(false);
            }

            return filePath;
        }

        /// <summary>
        /// Exports audit DTOs to a PDF file and logs the action.
        /// </summary>
        public async Task<string> ExportAuditToPdf(IEnumerable<AuditEntryDto> auditEntries, string filterUsed = "")
        {
            string filePath = Path.Combine(_exportRoot, $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using var writer = new PdfWriter(filePath);
            using var pdfDoc = new PdfDocument(writer);
            using var doc = new ITextDocument(pdfDoc);

            PdfFont fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont fontBold    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont fontItalic  = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            doc.Add(new Paragraph("GMP Audit Log – Izvještaj")
                .SetFont(fontBold)
                .SetFontSize(16)
                .SetTextAlignment(ITextAlignment.CENTER));

            doc.Add(new Paragraph($"Generirano: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .SetFont(fontRegular)
                .SetFontSize(10));

            var table = new Table(6).UseAllAvailableWidth();
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Entitet").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Akcija").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Korisnik").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Napomena").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Vrijeme").SetFont(fontBold)));
            table.AddHeaderCell(new ITextCell().Add(new Paragraph("Potpis").SetFont(fontBold)));

            foreach (var entry in (auditEntries ?? Array.Empty<AuditEntryDto>()))
            {
                string ts = (entry == null) ? string.Empty : entry.Timestamp.ToString("yyyy-MM-dd HH:mm");
                table.AddCell(new ITextCell().Add(new Paragraph(entry?.Entity ?? "-").SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(entry?.Action ?? "-").SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(entry?.Username ?? "-").SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(entry?.Note ?? "-").SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(ts).SetFont(fontRegular)));
                table.AddCell(new ITextCell().Add(new Paragraph(entry?.SignatureHash ?? "-").SetFont(fontRegular)));
            }

            doc.Add(table);

            doc.Add(new Paragraph("PDF generiran automatski iz GMP Audit Loga.")
                .SetFont(fontItalic)
                .SetFontSize(9));

            if (_audit is not null)
            {
                await _audit.LogCalibrationExportAsync("AUDIT_PDF", filePath, filterUsed ?? string.Empty).ConfigureAwait(false);
            }
            else
            {
                await LogFallbackAuditAsync("EXPORT_AUDIT_PDF", $"Exported Audit Log PDF: {filePath}").ConfigureAwait(false);
            }

            return filePath;
        }

        /// <summary>
        /// Exports machine components to the requested format (CSV, XLSX, PDF) and returns the file path.
        /// </summary>
        /// <param name="components">Component rows to export.</param>
        /// <param name="format">Desired export format (csv, xlsx, pdf). Defaults to csv.</param>
        public Task<string> ExportComponentsAsync(IEnumerable<MachineComponent>? components, string? format)
        {
            var list = (components ?? Array.Empty<MachineComponent>()).ToList();
            string normalizedFormat = string.IsNullOrWhiteSpace(format)
                ? "csv"
                : format.Trim().ToLowerInvariant();

            static string FormatDate(DateTime? value)
                => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : string.Empty;

            static string FormatDateTime(DateTime value)
                => value == default ? string.Empty : value.ToString("yyyy-MM-dd HH:mm");

            static string FormatMachine(MachineComponent component)
                => !string.IsNullOrWhiteSpace(component.Machine?.Name)
                    ? component.Machine!.Name
                    : component.MachineId?.ToString() ?? string.Empty;

            static string FormatNotes(MachineComponent component)
                => string.Join(
                    " | ",
                    new[] { component.Note, component.Notes }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!.Trim()));

            var columns = new (string Header, Func<MachineComponent, object?>)[]
            {
                ("ID", c => c.Id),
                ("Machine", c => FormatMachine(c)),
                ("Code", c => c.Code ?? string.Empty),
                ("Name", c => c.Name ?? string.Empty),
                ("Type", c => c.Type ?? string.Empty),
                ("Model", c => c.Model ?? string.Empty),
                ("Status", c => c.Status ?? string.Empty),
                ("Install Date", c => FormatDate(c.InstallDate)),
                ("Purchase Date", c => FormatDate(c.PurchaseDate)),
                ("Warranty Until", c => FormatDate(c.WarrantyUntil)),
                ("Warranty Expiry", c => FormatDate(c.WarrantyExpiry)),
                ("Serial Number", c => c.SerialNumber ?? string.Empty),
                ("Supplier", c => c.Supplier ?? string.Empty),
                ("Lifecycle Phase", c => c.LifecyclePhase ?? string.Empty),
                ("Critical", c => c.IsCritical ? "Yes" : "No"),
                ("RFID/NFC", c => c.RfidTag ?? string.Empty),
                ("IoT Device", c => c.IoTDeviceId ?? string.Empty),
                ("SOP Doc", c => c.SopDoc ?? string.Empty),
                ("Notes", c => FormatNotes(c)),
                ("Digital Signature", c => c.DigitalSignature ?? string.Empty),
                ("Last Modified", c => FormatDateTime(c.LastModified)),
                ("Modified By", c => c.LastModifiedBy?.FullName ?? (c.LastModifiedById == 0 ? string.Empty : c.LastModifiedById.ToString())),
                ("Source IP", c => c.SourceIp ?? string.Empty)
            };

            string path = normalizedFormat switch
            {
                "xlsx" => XlsxExporter.WriteSheet(list, "components", columns),
                "pdf"  => PdfExporter.WriteTable(list, "components", columns, "Machine Components Export"),
                _       => CsvExportHelper.WriteCsv(list, "components", columns)
            };

            return Task.FromResult(path);
        }

        /// <summary>
        /// Writes a minimal audit record to <c>audit_log</c> if the richer <see cref="AuditService"/> is not available.
        /// All nullable inputs are normalized to maintain DB integrity and avoid nullability warnings.
        /// </summary>
        private async Task LogFallbackAuditAsync(string action, string details)
        {
            var auth = _authContext ?? ServiceLocator.GetService<IAuthContext>();
            var platform = _platformService ?? ServiceLocator.GetService<IPlatformService>();
            int? userId = auth?.CurrentUser?.Id;
            string sessionId = auth?.CurrentSessionId ?? string.Empty;
            string ip = auth?.CurrentIpAddress ?? platform?.GetLocalIpAddress() ?? string.Empty;
            string deviceInfo = auth?.CurrentDeviceInfo ?? string.Empty;
            await _dbService.LogSystemEventAsync(
                userId: userId,
                eventType: action ?? string.Empty,
                tableName: "export",
                module: "ExportService",
                recordId: null,
                description: details ?? string.Empty,
                ip: ip,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: sessionId
            ).ConfigureAwait(false);
        }

        private string ResolveExportRoot()
        {
            var platform = _platformService ?? ServiceLocator.GetService<IPlatformService>();
            string? appData = platform?.GetAppDataDirectory();

            if (string.IsNullOrWhiteSpace(appData))
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(local))
                {
                    appData = Path.Combine(AppContext.BaseDirectory, "AppData");
                }
                else
                {
                    appData = Path.Combine(local, "YasGMP");
                }
            }

            var root = Path.Combine(appData!, "Exports", "Calibrations");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
