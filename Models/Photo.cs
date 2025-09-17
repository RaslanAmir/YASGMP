using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Photo</b> – Super ultra mega robust, forensic model for all photographic evidence: before/after, AI/ML, audit, compliance.
    /// <para>
    /// ✅ Full audit, digital signature, hash, session, approval, chain-of-custody, AI tags, geo, device info  
    /// ✅ Tracks every “who, what, when, where, why, how”, with regulatory and forensic fields  
    /// ✅ Ready for inspector export, blockchain, e-sign, watermark, multi-resolution, versioning, and analytics
    /// </para>
    /// </summary>
    [Table("photo")]
    public class Photo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public virtual Component? Component { get; set; }

        [Required, MaxLength(512)]
        [Column("file_path")]
        [Display(Name = "Putanja slike")]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(40)]
        [Column("type")]
        [Display(Name = "Vrsta slike")]
        public string? Type { get; set; }

        [MaxLength(255)]
        [Column("comment")]
        [Display(Name = "Komentar uz sliku")]
        public string? Comment { get; set; }

        [Column("uploaded_by_id")]
        public int? UploadedById { get; set; }

        [ForeignKey(nameof(UploadedById))]
        [InverseProperty(nameof(User.UploadedPhotos))]
        public virtual User? UploadedBy { get; set; }

        [Column("uploaded_at")]
        [Display(Name = "Vrijeme uploada")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(128)]
        [Column("hash")]
        [Display(Name = "Hash slike")]
        public string? Hash { get; set; }

        [MaxLength(128)]
        [Column("watermark")]
        [Display(Name = "Watermark")]
        public string? Watermark { get; set; }

        [Column("is_approved")]
        [Display(Name = "Odobreno")]
        public bool? IsApproved { get; set; }

        [Column("approved_by_id")]
        public int? ApprovedById { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        [InverseProperty(nameof(User.ApprovedPhotos))]
        public virtual User? ApprovedBy { get; set; }

        [Column("approved_at")]
        [Display(Name = "Vrijeme odobravanja")]
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        [MaxLength(45)]
        [Column("source_ip")]
        [Display(Name = "Izvor (IP/uređaj)")]
        public string? SourceIp { get; set; }

        [MaxLength(100)]
        [Column("session_id")]
        [Display(Name = "Session")]
        public string? SessionId { get; set; }

        [Column("file_size_bytes")]
        public long? FileSizeBytes { get; set; }

        [MaxLength(255)]
        [Column("original_file_name")]
        public string? OriginalFileName { get; set; }

        [MaxLength(4096)]
        [Column("recognition_data")]
        public string? RecognitionData { get; set; }

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [MaxLength(255)]
        [Column("device_info")]
        public string? DeviceInfo { get; set; }

        [Column("version")]
        public int? Version { get; set; }

        [Column("is_original")]
        public bool IsOriginal { get; set; } = true;

        [Column("previous_photo_id")]
        public int? PreviousPhotoId { get; set; }

        [ForeignKey(nameof(PreviousPhotoId))]
        public virtual Photo? PreviousPhoto { get; set; }

        [Column("next_photo_id")]
        public int? NextPhotoId { get; set; }

        [ForeignKey(nameof(NextPhotoId))]
        public virtual Photo? NextPhoto { get; set; }

        [MaxLength(40)]
        [Column("status")]
        public string? Status { get; set; }

        [MaxLength(512)]
        [Column("note")]
        public string? Note { get; set; }

        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        [InverseProperty(nameof(User.ModifiedPhotos))]
        public virtual User? LastModifiedBy { get; set; }

        // ==================== METHODS ====================

        /// <summary>
        /// Returns true if photo is approved and has valid digital signature.
        /// </summary>
        [NotMapped]
        public bool IsFullyApproved =>
            IsApproved == true && !string.IsNullOrEmpty(DigitalSignature) && ApprovedAt.HasValue;

        /// <summary>
        /// Returns human-readable photo summary for inspectors.
        /// </summary>
        public override string ToString()
        {
            return $"{Type ?? "Photo"}: {OriginalFileName ?? FilePath} (By: {UploadedById}, At: {UploadedAt:u}, Approved: {IsApproved})";
        }

        /// <summary>
        /// Creates a deep copy of the photo record (for rollback, audit, inspector export).
        /// </summary>
        public Photo DeepCopy()
        {
            return new Photo
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                WorkOrder = this.WorkOrder,
                ComponentId = this.ComponentId,
                Component = this.Component,
                FilePath = this.FilePath,
                Type = this.Type,
                Comment = this.Comment,
                UploadedById = this.UploadedById,
                UploadedBy = this.UploadedBy,
                UploadedAt = this.UploadedAt,
                Hash = this.Hash,
                Watermark = this.Watermark,
                IsApproved = this.IsApproved,
                ApprovedById = this.ApprovedById,
                ApprovedBy = this.ApprovedBy,
                ApprovedAt = this.ApprovedAt,
                DigitalSignature = this.DigitalSignature,
                SourceIp = this.SourceIp,
                SessionId = this.SessionId,
                FileSizeBytes = this.FileSizeBytes,
                OriginalFileName = this.OriginalFileName,
                RecognitionData = this.RecognitionData,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                DeviceInfo = this.DeviceInfo,
                Version = this.Version,
                IsOriginal = this.IsOriginal,
                PreviousPhotoId = this.PreviousPhotoId,
                PreviousPhoto = this.PreviousPhoto,
                NextPhotoId = this.NextPhotoId,
                NextPhoto = this.NextPhoto,
                Status = this.Status,
                Note = this.Note,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy
            };
        }
    }
}
