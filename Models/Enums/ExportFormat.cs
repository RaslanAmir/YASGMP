namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>ExportFormat</b> – Supported export, print, and reporting formats in YasGMP.
    /// <para>
    /// Used for document exports, report generation, audit trails, regulatory submissions, and interoperability.
    /// </para>
    /// <b>Extensible for new digital standards and hybrid formats. Compliant with GxP, GMP, CSV, and FDA 21 CFR Part 11 requirements for traceable data output.</b>
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// Microsoft Excel spreadsheet (.xlsx, .xls).
        /// </summary>
        Excel = 0,

        /// <summary>
        /// Portable Document Format (.pdf) – suitable for signed records, regulatory, and immutable reports.
        /// </summary>
        Pdf = 1,

        /// <summary>
        /// Comma-Separated Values (.csv) – machine-friendly, for import/export, analytics, and API.
        /// </summary>
        Csv = 2,

        /// <summary>
        /// XML (eXtensible Markup Language) – for data exchange, ERP, and regulatory systems.
        /// </summary>
        Xml = 3,

        /// <summary>
        /// JSON (JavaScript Object Notation) – for API, IoT, and system-to-system integrations.
        /// </summary>
        Json = 4,

        /// <summary>
        /// Other or custom/proprietary export formats (must specify via custom field).
        /// </summary>
        Other = 99
    }
}
