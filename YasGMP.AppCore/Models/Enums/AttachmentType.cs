using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>AttachmentType</b> – Enumerates all allowed types of attachments/documents in YasGMP.
    /// Enables filtering, security, and regulatory enforcement for uploads and linking files.
    /// <para>Used by: WorkOrderAttachment, SopDocument, Validation, Inspection, Photo, and all doc control modules.</para>
    /// </summary>
    public enum AttachmentType
    {
        /// <summary>PDF document</summary>
        [Display(Name = "PDF")]
        Pdf = 0,

        /// <summary>Word document (DOC, DOCX)</summary>
        [Display(Name = "Word")]
        Word = 1,

        /// <summary>Excel spreadsheet (XLS, XLSX)</summary>
        [Display(Name = "Excel")]
        Excel = 2,

        /// <summary>PowerPoint presentation (PPT, PPTX)</summary>
        [Display(Name = "PowerPoint")]
        PowerPoint = 3,

        /// <summary>Image: JPEG/JPG</summary>
        [Display(Name = "JPEG")]
        Jpeg = 4,

        /// <summary>Image: PNG</summary>
        [Display(Name = "PNG")]
        Png = 5,

        /// <summary>Image: TIFF</summary>
        [Display(Name = "TIFF")]
        Tiff = 6,

        /// <summary>Image: BMP</summary>
        [Display(Name = "BMP")]
        Bmp = 7,

        /// <summary>Plain text file</summary>
        [Display(Name = "Tekstualni dokument")]
        Txt = 8,

        /// <summary>XML document</summary>
        [Display(Name = "XML")]
        Xml = 9,

        /// <summary>CSV spreadsheet</summary>
        [Display(Name = "CSV")]
        Csv = 10,

        /// <summary>ZIP or archive</summary>
        [Display(Name = "ZIP/Arhiva")]
        Zip = 11,

        /// <summary>Email file (EML, MSG)</summary>
        [Display(Name = "E-mail")]
        Email = 12,

        /// <summary>Audio recording</summary>
        [Display(Name = "Audio zapis")]
        Audio = 13,

        /// <summary>Video recording</summary>
        [Display(Name = "Video zapis")]
        Video = 14,

        /// <summary>Other/Unknown file type (for extensibility)</summary>
        [Display(Name = "Drugo/Neodređeno")]
        Other = 1000
    }
}

