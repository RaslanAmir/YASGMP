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
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }

        [Column("priority_id")]
        public int? PriorityId { get; set; }

        [Column("tenant_id")]
        public int? TenantId { get; set; }

        [Column("related_incident")]
        public int? RelatedIncidentId { get; set; }

        [Column("photo_before_ids")]
        public string? PhotoBeforeIdsRaw
        {
            get => SerializeIds(PhotoBeforeIds);
            set => PhotoBeforeIds = ParseIds(value);
        }

        [Column("photo_after_ids")]
        public string? PhotoAfterIdsRaw
        {
            get => SerializeIds(PhotoAfterIds);
            set => PhotoAfterIds = ParseIds(value);
        }

        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        [Column("machine?")]
        public string? LegacyMachineLabel { get; set; }

        [Column("machine_component?")]
        public string? LegacyMachineComponentLabel { get; set; }

        [Column("capa_case?")]
        public string? LegacyCapaLabel { get; set; }

        [Column("incident?")]
        public string? LegacyIncidentLabel { get; set; }

        [Column("icollection<work_order_part>")]
        public string? LegacyPartsCollection { get; set; }

        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        [Column("icollection<work_order_comment>")]
        public string? LegacyCommentsCollection { get; set; }

        [Column("icollection<work_order_status_log>")]
        public string? LegacyStatusCollection { get; set; }

        [Column("icollection<work_order_signature>")]
        public string? LegacySignaturesCollection { get; set; }

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

