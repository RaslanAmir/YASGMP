using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WorkOrderExportFormat</b> — Supported file formats for exporting, printing, reporting, and interoperability of work orders.
    /// Includes standard, regulatory, and extensible (custom/automation) types.
    /// </summary>
    public enum WorkOrderExportFormat
    {
        /// <summary>
        /// Portable Document Format (PDF) — universal for regulatory and printable exports.
        /// </summary>
        [Display(Name = "PDF")]
        Pdf = 0,

        /// <summary>
        /// Microsoft Excel format (.xlsx) — for data analysis, reporting, or batch import/export.
        /// </summary>
        [Display(Name = "Excel")]
        Excel = 1,

        /// <summary>
        /// Comma-Separated Values (CSV) — lightweight for import/export and interoperability.
        /// </summary>
        [Display(Name = "CSV")]
        Csv = 2,

        /// <summary>
        /// XML format — for data exchange, regulatory, and API integration.
        /// </summary>
        [Display(Name = "XML")]
        Xml = 3,

        /// <summary>
        /// JSON format — for modern APIs, automation, and system-to-system integration.
        /// </summary>
        [Display(Name = "JSON")]
        Json = 4,

        /// <summary>
        /// Microsoft Word (.docx) — for reports requiring rich formatting or signatures.
        /// </summary>
        [Display(Name = "Word (DOCX)")]
        Word = 5,

        /// <summary>
        /// HTML — for browser previews or email integration.
        /// </summary>
        [Display(Name = "HTML")]
        Html = 6,

        /// <summary>
        /// PNG image file — for visual snapshots, dashboards, or diagrams.
        /// </summary>
        [Display(Name = "PNG")]
        Png = 7,

        /// <summary>
        /// Zipped archive (.zip) — for bulk export, packaging, and long-term archiving.
        /// </summary>
        [Display(Name = "ZIP")]
        Zip = 8,

        /// <summary>
        /// Proprietary, 3rd-party, or AI-generated formats (for future plugins, vendor APIs, automation, etc.).
        /// </summary>
        [Display(Name = "Custom/Other")]
        Custom = 1000
    }
}

