using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extensions for <see cref="Deviation"/> exposing raw database columns and serialised helpers.
    /// </summary>
    public partial class Deviation
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
        /// Represents the corrective actions raw value.
        /// </summary>
        [Column("corrective_actions")]
        public string? CorrectiveActionsRaw
        {
            get => CorrectiveActions == null || CorrectiveActions.Count == 0
                ? null
                : string.Join(',', CorrectiveActions);
            set => CorrectiveActions = string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        /// <summary>
        /// Represents the attachment ids raw value.
        /// </summary>
        [Column("attachment_ids")]
        public string? AttachmentIdsRaw
        {
            get => AttachmentIds == null || AttachmentIds.Count == 0
                ? null
                : string.Join(',', AttachmentIds);
            set
            {
                AttachmentIds = string.IsNullOrWhiteSpace(value)
                    ? new List<int>()
                    : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(token => int.TryParse(token, out var parsed) ? (int?)parsed : null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToList();
            }
        }

        /// <summary>
        /// Raw attachment manifest stored in the database (UI consumes <see cref="Attachments"/> instead).
        /// </summary>
        [Column("attachments")]
        public string? AttachmentsRaw { get; set; }

        /// <summary>
        /// Serialized audit trail snapshot; interactive lists live in <see cref="AuditTrail"/>.
        /// </summary>
        [Column("audit_trail")]
        public string? AuditTrailRaw { get; set; }
    }
}
