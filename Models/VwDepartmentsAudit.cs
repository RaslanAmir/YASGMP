using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("vw_departments_audit")]
    public class VwDepartmentsAudit
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("ts_utc")]
        public DateTime TsUtc { get; set; }

        [Column("event_type")]
        [StringLength(100)]
        public string? EventType { get; set; }

        [Column("record_id")]
        public int? RecordId { get; set; }

        [Column("field_name")]
        [StringLength(100)]
        public string? FieldName { get; set; }

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("severity")]
        [StringLength(20)]
        public string? Severity { get; set; }
    }
}
