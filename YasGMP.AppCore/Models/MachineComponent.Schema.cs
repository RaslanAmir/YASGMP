using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extensions for <see cref="MachineComponent"/> exposing raw table columns and legacy metadata blobs.
    /// </summary>
    public partial class MachineComponent
    {
        /// <summary>
        /// Gets or sets the component type id.
        /// </summary>
        [Column("component_type_id")]
        public int? ComponentTypeId { get; set; }

        /// <summary>
        /// Gets or sets the status id.
        /// </summary>
        [Column("status_id")]
        public int? StatusId { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the deleted at.
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the deleted by id.
        /// </summary>
        [Column("deleted_by")]
        public int? DeletedById { get; set; }

        /// <summary>
        /// Gets or sets the legacy machine label.
        /// </summary>
        [Column("machine?")]
        public string? LegacyMachineLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        /// <summary>
        /// Represents the documents raw value.
        /// </summary>
        [Column("documents")]
        public string? DocumentsRaw
        {
            get => Documents != null && Documents.Count > 0 ? string.Join(',', Documents) : null;
            set => Documents = string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : new List<string>(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        /// <summary>
        /// Gets or sets the legacy photos collection.
        /// </summary>
        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy calibrations collection.
        /// </summary>
        [Column("icollection<calibration>")]
        public string? LegacyCalibrationsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy capa cases collection.
        /// </summary>
        [Column("icollection<capa_case>")]
        public string? LegacyCapaCasesCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy work orders collection.
        /// </summary>
        [Column("icollection<work_order>")]
        public string? LegacyWorkOrdersCollection { get; set; }
    }
}
