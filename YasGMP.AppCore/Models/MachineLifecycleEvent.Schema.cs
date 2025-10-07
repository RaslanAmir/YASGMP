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
        /// Gets or sets the event type id.
        /// </summary>
        [Column("event_type_id")]
        public int? EventTypeId { get; set; }

        /// <summary>
        /// Gets or sets the machine label.
        /// </summary>
        [Column("machine")]
        [MaxLength(255)]
        public string? MachineLabel { get; set; }

        /// <summary>
        /// Gets or sets the performed by legacy.
        /// </summary>
        [Column("performed_by")]
        public int? PerformedByLegacy { get; set; }

        /// <summary>
        /// Gets or sets the last modified by name.
        /// </summary>
        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }
}

