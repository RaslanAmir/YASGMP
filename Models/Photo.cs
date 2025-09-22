using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Photo</b> – Super ultra mega robust, forensic model for all photographic evidence.
    /// <para>
    /// ✅ Full audit, digital signature, hash, session, approval, chain-of-custody, AI tags, geo, device info.<br/>
    /// ✅ Tracks every "who, what, when, where, why, how" with regulatory and forensic fields.<br/>
    /// ✅ Ready for inspector export, blockchain, e-sign, watermark, multi-resolution, versioning, and analytics.
    /// </para>
    /// </summary>
    [Table("photos")]
    public partial class Photo
    {
        #region === Core Identity / Relations ===

        /// <summary>Primary key of the photo record.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Linked work order (if the photo is tied to maintenance activities).</summary>
        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        /// <summary>Navigation to the related work order.</summary>
        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>Linked machine component (equipment part, subassembly, etc.).</summary>
        [Column("component_id")]
        public int? ComponentId { get; set; }

        /// <summary>Navigation to the related component.</summary>
        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent? Component { get; set; }

        /// <summary>Linked asset/equipment (optional direct relationship).</summary>
        [Column("asset_id")]
        public int? AssetId { get; set; }

        /// <summary>Navigation to the related asset.</summary>
        [ForeignKey(nameof(AssetId))]
        public virtual Asset? Asset { get; set; }

        #endregion

        #region === File Metadata ===

        /// <summary>Display name/title of the photo.</summary>
        [Column("title")]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Original file name as provided during upload.</summary>
        [Column("file_name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Storage path, URL, or blob reference.</summary>
        [Column("file_path")]
        [StringLength(1024)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Relative/absolute path of generated thumbnail.</summary>
        [Column("thumbnail_path")]
        [StringLength(1024)]
        public string? ThumbnailPath { get; set; }

        /// <summary>Primary storage provider (disk, s3, azure, db, etc.).</summary>
        [Column("storage_provider")]
        [StringLength(64)]
        public string? StorageProvider { get; set; }

        /// <summary>Exact storage location identifier for cloud/object storage.</summary>
        [Column("storage_key")]
        [StringLength(255)]
        public string? StorageKey { get; set; }

        /// <summary>Size of the file in bytes.</summary>
        [Column("file_size")]
        public long? FileSize { get; set; }

        /// <summary>MIME/content type of the stored image.</summary>
        [Column("mime_type")]
        [StringLength(128)]
        public string? MimeType { get; set; }

        /// <summary>SHA256 or preferred hash of the file (chain-of-custody).</summary>
        [Column("file_hash")]
        [StringLength(128)]
        public string? FileHash { get; set; }

        /// <summary>Optional alternative checksum/hash.</summary>
        [Column("hash_algorithm")]
        [StringLength(32)]
        public string? HashAlgorithm { get; set; }

        /// <summary>Width of the captured image in pixels.</summary>
        [Column("width_px")]
        public int? WidthPixels { get; set; }

        /// <summary>Height of the captured image in pixels.</summary>
        [Column("height_px")]
        public int? HeightPixels { get; set; }

        /// <summary>Optional DPI metadata (X axis).</summary>
        [Column("dpi_x")]
        public int? DpiX { get; set; }

        /// <summary>Optional DPI metadata (Y axis).</summary>
        [Column("dpi_y")]
        public int? DpiY { get; set; }

        /// <summary>Color space information (RGB, CMYK, etc.).</summary>
        [Column("color_space")]
        [StringLength(64)]
        public string? ColorSpace { get; set; }

        #endregion

        #region === Classification / Description ===

        /// <summary>Raw persisted type/category string (legacy friendly).</summary>
        [Column("type")]
        [StringLength(64)]
        public string TypeRaw { get; set; } = PhotoType.Documentation.ToString();

        /// <summary>Optional textual description.</summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>Free-form note for operators/auditors.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Comma/JSON separated tags for indexing and AI search.</summary>
        [Column("tags")]
        public string? Tags { get; set; }

        /// <summary>Whether a visible watermark has been applied.</summary>
        [Column("watermark_applied")]
        public bool WatermarkApplied { get; set; }

        /// <summary>Text or identifier of the watermark template used.</summary>
        [Column("watermark_text")]
        [StringLength(255)]
        public string? WatermarkText { get; set; }

        #endregion

        #region === Audit / Ownership ===

        /// <summary>User who uploaded the file.</summary>
        [Column("uploaded_by_id")]
        public int? UploadedById { get; set; }

        /// <summary>Navigation to uploader.</summary>
        [ForeignKey(nameof(UploadedById))]
        public virtual User? UploadedBy { get; set; }

        /// <summary>Timestamp when the file was uploaded.</summary>
        [Column("uploaded_at")]
        public DateTime? UploadedAt { get; set; }

        /// <summary>User who approved/validated the photo.</summary>
        [Column("approved_by_id")]
        public int? ApprovedById { get; set; }

        /// <summary>Navigation to approver.</summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User? ApprovedBy { get; set; }

        /// <summary>Timestamp of approval.</summary>
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>User who last modified metadata.</summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>Navigation to last modifying user.</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>When metadata was last changed.</summary>
        [Column("last_modified_at")]
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>Digital signature or hash chain for forensic integrity.</summary>
        [Column("digital_signature")]
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>IP of the uploader (traceability / forensics).</summary>
        [Column("source_ip")]
        [StringLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>Device/browser fingerprint of uploader.</summary>
        [Column("device_info")]
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        /// <summary>Session identifier linking to login/audit logs.</summary>
        [Column("session_id")]
        [StringLength(128)]
        public string? SessionId { get; set; }

        /// <summary>Soft-delete flag.</summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>User who deleted/archived the photo.</summary>
        [Column("deleted_by_id")]
        public int? DeletedById { get; set; }

        /// <summary>Navigation to user who deleted/archived the photo.</summary>
        [ForeignKey(nameof(DeletedById))]
        public virtual User? DeletedBy { get; set; }

        /// <summary>Timestamp when the photo was deleted/archived.</summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>Creation timestamp.</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Last database update timestamp.</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region === Geo / Capture Metadata ===

        /// <summary>Latitude where the photo was captured.</summary>
        [Column("latitude")]
        public double? Latitude { get; set; }

        /// <summary>Longitude where the photo was captured.</summary>
        [Column("longitude")]
        public double? Longitude { get; set; }

        /// <summary>GPS/geo accuracy in meters.</summary>
        [Column("location_accuracy")]
        public double? LocationAccuracy { get; set; }

        /// <summary>Date/time when the photo was captured (from EXIF, IoT, etc.).</summary>
        [Column("captured_at")]
        public DateTime? CapturedAt { get; set; }

        /// <summary>Camera or device make/brand.</summary>
        [Column("camera_make")]
        [StringLength(128)]
        public string? CameraMake { get; set; }

        /// <summary>Camera or device model.</summary>
        [Column("camera_model")]
        [StringLength(128)]
        public string? CameraModel { get; set; }

        /// <summary>Camera serial or unique identifier.</summary>
        [Column("camera_serial")]
        [StringLength(128)]
        public string? CameraSerial { get; set; }

        /// <summary>Exposure information from EXIF metadata.</summary>
        [Column("exposure")]
        [StringLength(64)]
        public string? Exposure { get; set; }

        /// <summary>Aperture/f-stop data from EXIF.</summary>
        [Column("aperture")]
        [StringLength(32)]
        public string? Aperture { get; set; }

        /// <summary>ISO setting from EXIF metadata.</summary>
        [Column("iso")]
        public int? Iso { get; set; }

        /// <summary>Focal length used when capturing the image.</summary>
        [Column("focal_length")]
        [StringLength(32)]
        public string? FocalLength { get; set; }

        /// <summary>Indicates if flash fired.</summary>
        [Column("flash_fired")]
        public bool? FlashFired { get; set; }

        #endregion

        #region === AI / Analytics Metadata ===

        /// <summary>Machine learning tags or JSON payload describing detected content.</summary>
        [Column("ai_tags")]
        public string? AiTags { get; set; }

        /// <summary>Confidence score of AI/ML classification.</summary>
        [Column("ai_confidence")]
        public double? AiConfidence { get; set; }

        /// <summary>Anomaly score used for inspections or automated review.</summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>Free-form AI/analytics notes.</summary>
        [Column("ai_notes")]
        public string? AiNotes { get; set; }

        /// <summary>Chain-of-custody or blockchain reference.</summary>
        [Column("chain_id")]
        [StringLength(128)]
        public string? ChainId { get; set; }

        #endregion

        #region === Convenience / Helpers ===

        /// <summary>
        /// Strongly-typed access to the <see cref="PhotoType"/> enum while persisting the legacy string.
        /// Defaults to <see cref="PhotoType.Documentation"/>.
        /// </summary>
        [NotMapped]
        public PhotoType Category
        {
            get
            {
                if (Enum.TryParse<PhotoType>(TypeRaw, true, out var parsed))
                {
                    return parsed;
                }

                return PhotoType.Other;
            }
            set => TypeRaw = value.ToString();
        }

        /// <summary>Indicates whether the photo has been formally approved.</summary>
        [NotMapped]
        public bool IsApproved => ApprovedById.HasValue && ApprovedAt.HasValue;

        /// <summary>Create a deep copy for safe editing / rollback scenarios.</summary>
        public Photo DeepCopy()
        {
            return new Photo
            {
                Id = Id,
                WorkOrderId = WorkOrderId,
                WorkOrder = WorkOrder,
                ComponentId = ComponentId,
                Component = Component,
                AssetId = AssetId,
                Asset = Asset,
                Title = Title,
                FileName = FileName,
                FilePath = FilePath,
                ThumbnailPath = ThumbnailPath,
                StorageProvider = StorageProvider,
                StorageKey = StorageKey,
                FileSize = FileSize,
                MimeType = MimeType,
                FileHash = FileHash,
                HashAlgorithm = HashAlgorithm,
                WidthPixels = WidthPixels,
                HeightPixels = HeightPixels,
                DpiX = DpiX,
                DpiY = DpiY,
                ColorSpace = ColorSpace,
                TypeRaw = TypeRaw,
                Description = Description,
                Note = Note,
                Tags = Tags,
                WatermarkApplied = WatermarkApplied,
                WatermarkText = WatermarkText,
                UploadedById = UploadedById,
                UploadedBy = UploadedBy,
                UploadedAt = UploadedAt,
                ApprovedById = ApprovedById,
                ApprovedBy = ApprovedBy,
                ApprovedAt = ApprovedAt,
                LastModifiedById = LastModifiedById,
                LastModifiedBy = LastModifiedBy,
                LastModifiedAt = LastModifiedAt,
                DigitalSignature = DigitalSignature,
                SourceIp = SourceIp,
                DeviceInfo = DeviceInfo,
                SessionId = SessionId,
                IsDeleted = IsDeleted,
                DeletedById = DeletedById,
                DeletedBy = DeletedBy,
                DeletedAt = DeletedAt,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Latitude = Latitude,
                Longitude = Longitude,
                LocationAccuracy = LocationAccuracy,
                CapturedAt = CapturedAt,
                CameraMake = CameraMake,
                CameraModel = CameraModel,
                CameraSerial = CameraSerial,
                Exposure = Exposure,
                Aperture = Aperture,
                Iso = Iso,
                FocalLength = FocalLength,
                FlashFired = FlashFired,
                AiTags = AiTags,
                AiConfidence = AiConfidence,
                AnomalyScore = AnomalyScore,
                AiNotes = AiNotes,
                ChainId = ChainId
            };
        }

        /// <summary>Debug/diagnostic representation.</summary>
        public override string ToString()
            => $"Photo #{Id}: {FileName} ({TypeRaw})";

        #endregion
    }
}
