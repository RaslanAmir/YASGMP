using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extension exposing raw columns for <see cref="MachineLifecycleEvent"/>.
    /// </summary>
    public partial class MachineLifecycleEvent
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("event_type_id")]
        public int? EventTypeId { get; set; }

        [Column("machine")]
        [MaxLength(255)]
        public string? MachineLabel { get; set; }

        [Column("performed_by")]
        public int? PerformedByLegacy { get; set; }

        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }
}

