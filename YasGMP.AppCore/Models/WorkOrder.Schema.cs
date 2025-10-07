using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema helpers keeping <see cref="WorkOrder"/> in sync with the MySQL table while preserving domain conveniences.
    /// </summary>
    public partial class WorkOrder
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
        /// Gets or sets the status id.
        /// </summary>
        [Column("status_id")]
        public int? StatusId { get; set; }

        /// <summary>
        /// Gets or sets the type id.
        /// </summary>
        [Column("type_id")]
        public int? TypeId { get; set; }

        /// <summary>
        /// Gets or sets the priority id.
        /// </summary>
        [Column("priority_id")]
        public int? PriorityId { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        [Column("tenant_id")]
        public int? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the related incident id.
        /// </summary>
        [Column("related_incident")]
        public int? RelatedIncidentId { get; set; }

        /// <summary>
        /// Represents the photo before ids raw value.
        /// </summary>
        [Column("photo_before_ids")]
        public string? PhotoBeforeIdsRaw
        {
            get => SerializeIds(PhotoBeforeIds);
            set => PhotoBeforeIds = ParseIds(value);
        }

        /// <summary>
        /// Represents the photo after ids raw value.
        /// </summary>
        [Column("photo_after_ids")]
        public string? PhotoAfterIdsRaw
        {
            get => SerializeIds(PhotoAfterIds);
            set => PhotoAfterIds = ParseIds(value);
        }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy machine label.
        /// </summary>
        [Column("machine?")]
        public string? LegacyMachineLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy machine component label.
        /// </summary>
        [Column("machine_component?")]
        public string? LegacyMachineComponentLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy capa label.
        /// </summary>
        [Column("capa_case?")]
        public string? LegacyCapaLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy incident label.
        /// </summary>
        [Column("incident?")]
        public string? LegacyIncidentLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy parts collection.
        /// </summary>
        [Column("icollection<work_order_part>")]
        public string? LegacyPartsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy photos collection.
        /// </summary>
        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy comments collection.
        /// </summary>
        [Column("icollection<work_order_comment>")]
        public string? LegacyCommentsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy status collection.
        /// </summary>
        [Column("icollection<work_order_status_log>")]
        public string? LegacyStatusCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy signatures collection.
        /// </summary>
        [Column("icollection<work_order_signature>")]
        public string? LegacySignaturesCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy audit collection.
        /// </summary>
        [Column("icollection<work_order_audit>")]
        public string? LegacyAuditCollection { get; set; }

        private static List<int> ParseIds(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<int>();
            }

            var tokens = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = new List<int>(tokens.Length);
            foreach (var token in tokens)
            {
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        private static string? SerializeIds(IReadOnlyCollection<int> values)
        {
            return values != null && values.Count > 0
                ? string.Join(',', values)
                : null;
        }
    }
}
