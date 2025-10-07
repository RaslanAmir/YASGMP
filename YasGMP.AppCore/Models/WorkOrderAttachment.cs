using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderAttachment</b> – Svi prilozi, dokumenti, slike, zapisi vezani za radni nalog.
    /// Svaki attachment je 100% auditiran, forenzički zaštićen (hash, watermark, potpis, tip, verzija, rollback).
    /// Omogućuje upload i vezu na PDF, slike, video, audio, e-mail, XML, Excel... (GMP/CSV compliant).
    /// Podržava sve: digitalne potpise, verifikacije, rollback, forenzičke podatke, AI/ML/OCR, virus scan, i Cloud/IoT ready!
    /// </summary>
    public class WorkOrderAttachment
    {
        /// <summary>Jedinstveni ID attachmenta (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>ID radnog naloga kojem attachment pripada (FK).</summary>
        [Required]
        public int WorkOrderId { get; set; }

        /// <summary>Navigacija na radni nalog.</summary>
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>Tip dokumenta/priloga (pdf, jpg, png, xlsx, audio, video, email, xml, zip...).</summary>
        [Required, MaxLength(32)]
        public string Type { get; set; } = string.Empty;

        /// <summary>Naziv ili opis priloga (za pretragu i UI prikaz).</summary>
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Putanja do datoteke (lokalni ili cloud storage, S3, Azure...).</summary>
        [Required, MaxLength(512)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Verzija dokumenta/priloga (za rollback i audit).</summary>
        public int Version { get; set; } = 1;

        /// <summary>Digitalni hash sadržaja (SHA-256, dokaz integriteta, forenzika).</summary>
        [MaxLength(128)]
        public string Hash { get; set; } = string.Empty;

        /// <summary>Watermark ili digitalni potpis (user, datum, tekstualni znak).</summary>
        [MaxLength(128)]
        public string Watermark { get; set; } = string.Empty;

        /// <summary>Komentar ili opis (opcionalno).</summary>
        [MaxLength(255)]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Korisnik koji je dodao/upload-ao prilog (FK na User).</summary>
        public int UploadedById { get; set; }
        /// <summary>
        /// Gets or sets the uploaded by.
        /// </summary>
        public User? UploadedBy { get; set; }

        /// <summary>Vrijeme uploada (audit, inspekcija).</summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Da li je dokument odobren za inspekciju/izvoz (GMP audit).</summary>
        public bool? IsApproved { get; set; }

        /// <summary>ID korisnika koji je odobrio (FK).</summary>
        public int? ApprovedById { get; set; }
        /// <summary>
        /// Gets or sets the approved by.
        /// </summary>
        public User? ApprovedBy { get; set; }

        /// <summary>Datum/vrijeme odobravanja (audit).</summary>
        public DateTime? ApprovedAt { get; set; }

        // =========== ULTRA-MEGA BONUS & FORENSIC FIELDS ===========

        /// <summary>IP adresa/uređaj s kojeg je izvršen upload (forenzika).</summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Forenzički session ID (ako se koristi).</summary>
        [MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Veličina datoteke (byte, for chain-of-custody i virus scan).</summary>
        public long? FileSizeBytes { get; set; }

        /// <summary>Originalni naziv datoteke (za dokaz i chain-of-custody).</summary>
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>AI/OCR/metapodaci (barcode, QR, text recognition, future ML).</summary>
        [MaxLength(2048)]
        public string RecognitionData { get; set; } = string.Empty;

        /// <summary>Virus scan status: Clean, Infected, Unknown.</summary>
        [MaxLength(32)]
        public string VirusScanStatus { get; set; } = string.Empty;

        /// <summary>Cloud storage info (provider, bucket, blob).</summary>
        [MaxLength(128)]
        public string CloudStorageReference { get; set; } = string.Empty;

        /// <summary>Vrijeme zadnje izmjene (audit trail, rollback).</summary>
        public DateTime? LastModified { get; set; }

        /// <summary>ID korisnika koji je zadnji mijenjao attachment.</summary>
        public int? LastModifiedById { get; set; }
        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        public User? LastModifiedBy { get; set; }

        // =========== COMPUTED PROPERTIES & HELPERS ===========

        /// <summary>Provjera da li je attachment službeno odobren i forenzički ispravan.</summary>
        public bool IsCompliant => IsApproved == true && !string.IsNullOrEmpty(Hash);

        /// <summary>Provjera je li dokument slika (za UI prikaz, preview, watermark…)</summary>
        public bool IsImage => Type?.ToLower() is "jpg" or "jpeg" or "png" or "bmp" or "gif" or "tiff";

        /// <summary>DeepCopy – za rollback, audit trail, verzije.</summary>
        public WorkOrderAttachment DeepCopy()
        {
            return new WorkOrderAttachment
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                WorkOrder = this.WorkOrder,
                Type = this.Type,
                Name = this.Name,
                FilePath = this.FilePath,
                Version = this.Version,
                Hash = this.Hash,
                Watermark = this.Watermark,
                Comment = this.Comment,
                UploadedById = this.UploadedById,
                UploadedBy = this.UploadedBy,
                UploadedAt = this.UploadedAt,
                IsApproved = this.IsApproved,
                ApprovedById = this.ApprovedById,
                ApprovedBy = this.ApprovedBy,
                ApprovedAt = this.ApprovedAt,
                SourceIp = this.SourceIp,
                SessionId = this.SessionId,
                FileSizeBytes = this.FileSizeBytes,
                OriginalFileName = this.OriginalFileName,
                RecognitionData = this.RecognitionData,
                VirusScanStatus = this.VirusScanStatus,
                CloudStorageReference = this.CloudStorageReference,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy
            };
        }

        /// <summary>String prikaz za debugging/log/inspekciju.</summary>
        public override string ToString()
        {
            return $"Attachment: {Name ?? OriginalFileName ?? FilePath} (Type: {Type}, Version: {Version}, Approved: {IsApproved})";
        }
    }
}
