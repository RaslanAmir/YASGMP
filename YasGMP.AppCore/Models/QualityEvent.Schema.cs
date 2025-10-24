using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema helpers keeping <see cref="QualityEvent"/> aligned with the underlying table
    /// while preserving the domain-focused surface.
    /// </summary>
    public partial class QualityEvent
    {
        [Column("event_type")]
        [MaxLength(50)]
        public string? EventTypeRaw
        {
            get => QualityEventSchemaMapper.EventTypeToString(EventType);
            set => EventType = QualityEventSchemaMapper.StringToEventType(value);
        }

        [Column("status")]
        [MaxLength(50)]
        public string? StatusRaw
        {
            get => QualityEventSchemaMapper.StatusToString(Status);
            set => Status = QualityEventSchemaMapper.StringToStatus(value);
        }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("related_machine")]
        public int? LegacyRelatedMachine { get; set; }

        [Column("related_component")]
        public int? LegacyRelatedComponent { get; set; }

        [Column("created_by")]
        [MaxLength(255)]
        public string? CreatedByName { get; set; }

        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }

    internal static class QualityEventSchemaMapper
    {
        public static string EventTypeToString(QualityEventType value)
            => value switch
            {
                QualityEventType.Deviation => "deviation",
                QualityEventType.Complaint => "complaint",
                QualityEventType.Recall => "recall",
                QualityEventType.OutOfSpec => "out_of_spec",
                QualityEventType.ChangeControl => "change_control",
                QualityEventType.Audit => "audit",
                QualityEventType.Training => "training",
                QualityEventType.Other => "other",
                _ => "other"
            };

        public static QualityEventType StringToEventType(string? value)
            => value?.ToLowerInvariant() switch
            {
                "deviation" => QualityEventType.Deviation,
                "complaint" => QualityEventType.Complaint,
                "recall" => QualityEventType.Recall,
                "out_of_spec" => QualityEventType.OutOfSpec,
                "change_control" => QualityEventType.ChangeControl,
                "audit" => QualityEventType.Audit,
                "training" => QualityEventType.Training,
                "other" => QualityEventType.Other,
                _ => QualityEventType.Other
            };

        public static string StatusToString(QualityEventStatus value)
            => value switch
            {
                QualityEventStatus.Open => "open",
                QualityEventStatus.Closed => "closed",
                QualityEventStatus.UnderReview => "under_review",
                _ => "open"
            };

        public static QualityEventStatus StringToStatus(string? value)
            => value?.ToLowerInvariant() switch
            {
                "open" => QualityEventStatus.Open,
                "closed" => QualityEventStatus.Closed,
                "under_review" => QualityEventStatus.UnderReview,
                _ => QualityEventStatus.Open
            };
    }
}

