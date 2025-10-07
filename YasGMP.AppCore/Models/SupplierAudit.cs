using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Immutable audit trail entry for Supplier changes (21 CFR Part 11 / Annex 11).
    /// </summary>
    public partial class SupplierAudit
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>FK to the audited supplier.</summary>
        [Required]
        public int SupplierId { get; set; }

        /// <summary>User performing the action.</summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>Action performed (Create, Update, Delete, Approve…)</summary>
        [Required, MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>Verbose details/notes.</summary>
        [MaxLength(4000)]
        public string Details { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the action occurred.</summary>
        [Required]
        public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [MaxLength(45)]
        public string SourceIp { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [MaxLength(256)]
        public string DeviceInfo { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [MaxLength(1024)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the supplier version.
        /// </summary>
        [NotMapped]
        public int? SupplierVersion { get; set; }
        /// <summary>
        /// Gets or sets the json snapshot before.
        /// </summary>
        [NotMapped]
        public string? JsonSnapshotBefore { get; set; }
        /// <summary>
        /// Gets or sets the json snapshot after.
        /// </summary>
        [NotMapped]
        public string? JsonSnapshotAfter { get; set; }
        /// <summary>
        /// Gets or sets the extension data json.
        /// </summary>
        [NotMapped]
        public string? ExtensionDataJson { get; set; }

        /// <summary>Human-readable summary.</summary>
        public override string ToString()
            => $"SupplierAudit[ID={Id}, SupplierId={SupplierId}, UserId={UserId}, Action={ActionType}, At={ActionTimestamp:O}]";

        /// <summary>Quick validation of mandatory fields.</summary>
        public void Validate()
        {
            if (SupplierId <= 0) throw new ArgumentException("SupplierId must be positive.");
            if (UserId <= 0) throw new ArgumentException("UserId must be positive.");
            if (string.IsNullOrWhiteSpace(ActionType)) throw new ArgumentException("ActionType is required.");
            if (ActionTimestamp == default) throw new ArgumentException("ActionTimestamp must be set.");
        }

        // ---------------------------------------------------------------------
        //  Backward-compat alias properties (used by older DB/service code)
        // ---------------------------------------------------------------------

        /// <summary>Alias for <see cref="ActionType"/> used by historic code.</summary>
        [NotMapped]
        public string Action
        {
            get => ActionType;
            set => ActionType = value;
        }

        /// <summary>Alias for <see cref="ActionTimestamp"/> used by historic code.</summary>
        [NotMapped]
        public DateTime ChangedAt
        {
            get => ActionTimestamp;
            set => ActionTimestamp = value;
        }
    }
}
