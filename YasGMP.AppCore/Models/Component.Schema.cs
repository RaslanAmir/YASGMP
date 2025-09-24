using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extensions for <see cref="Component"/> surfacing snake_case columns and legacy collections from the database dump.
    /// </summary>
    public partial class Component
    {
        [Column("component_type_id")]
        public int? ComponentTypeId { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("deleted_by")]
        public int? DeletedById { get; set; }

        [Column("machine?")]
        public string? LegacyMachineLabel { get; set; }

        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        [Column("documents")]
        public string? DocumentsRaw
        {
            get => LinkedDocuments != null && LinkedDocuments.Count > 0
                ? string.Join(',', LinkedDocuments)
                : null;
            set => LinkedDocuments = string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : new List<string>(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        [Column("icollection<calibration>")]
        public string? LegacyCalibrationsCollection { get; set; }

        [Column("icollection<capa_case>")]
        public string? LegacyCapaCasesCollection { get; set; }

        [Column("icollection<work_order>")]
        public string? LegacyWorkOrdersCollection { get; set; }
    }
}
