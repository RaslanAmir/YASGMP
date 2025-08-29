using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Validation record (IQ/OQ/PQ/URS/DQ/FAT/SAT...).
    /// </summary>
    [Table("validations")]
    public class Validation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [MaxLength(40)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Column("machine_id")]
        public int? MachineId { get; set; }
        [ForeignKey(nameof(MachineId))]
        public virtual Machine? Machine { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }
        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent? Component { get; set; }

        [Column("date_start")]
        public DateTime? DateStart { get; set; }

        [Column("date_end")]
        public DateTime? DateEnd { get; set; }

        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Convenience: end date, start date, or <see cref="DateTime.MinValue"/>.</summary>
        [NotMapped]
        public DateTime ValidationDate => DateEnd ?? DateStart ?? DateTime.MinValue;

        [MaxLength(1024)]
        [Column("documentation")]
        public string Documentation { get; set; } = string.Empty;

        [Column("signed_by_id")]
        public int? SignedById { get; set; }
        [ForeignKey(nameof(SignedById))]
        public virtual User? SignedBy { get; set; }

        [MaxLength(100)]
        [Column("signed_by_name")]
        public string SignedByName { get; set; } = string.Empty;

        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        [MaxLength(500)]
        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        [MaxLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("created_by_id")]
        public int? CreatedById { get; set; }
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        [MaxLength(64)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        [MaxLength(40)]
        [Column("workflow_status")]
        public string WorkflowStatus { get; set; } = string.Empty;

        [MaxLength(512)]
        [Column("additional_signers")]
        public string AdditionalSigners { get; set; } = string.Empty;

        [MaxLength(60)]
        [Column("regulator")]
        public string Regulator { get; set; } = string.Empty;

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        [Column("linked_capa_id")]
        public int? LinkedCapaId { get; set; }
        [ForeignKey(nameof(LinkedCapaId))]
        public virtual CapaCase? LinkedCapa { get; set; }

        [Column("signature_timestamp")]
        public DateTime? SignatureTimestamp { get; set; }

        [MaxLength(80)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        #region VM/Binding alias properties (NotMapped)

        /// <summary>Alias used by some ViewModels (maps to <see cref="Type"/>).</summary>
        [NotMapped]
        public string ValidationType
        {
            get => Type;
            set => Type = value;
        }

        /// <summary>Alias used by some exports (maps to <see cref="Code"/>).</summary>
        [NotMapped]
        public string ProtocolNumber
        {
            get => Code;
            set => Code = value;
        }

        /// <summary>Computed display: machine or component name.</summary>
        [NotMapped]
        public string TargetName => Machine?.Name ?? Component?.Name ?? string.Empty;

        /// <summary>Alias for bindings in XAML (maps to <see cref="Comment"/>).</summary>
        [NotMapped]
        public string Note
        {
            get => Comment;
            set => Comment = value;
        }

        /// <summary>Main documentation file if JSON array provided.</summary>
        [NotMapped]
        public string? DocFile
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Documentation) && Documentation.StartsWith("["))
                {
                    try
                    {
                        var files = System.Text.Json.JsonSerializer.Deserialize<string[]>(Documentation);
                        return files?.Length > 0 ? files[0] : null;
                    }
                    catch
                    {
                        // Intentionally swallow – fall through to return Documentation as-is.
                    }
                }
                return string.IsNullOrWhiteSpace(Documentation) ? null : Documentation;
            }
        }

        #endregion

        /// <summary>Creates a deep copy of this validation entity.</summary>
        public Validation DeepCopy()
        {
            return new Validation
            {
                Id = this.Id,
                Code = this.Code,
                Type = this.Type,
                MachineId = this.MachineId,
                Machine = this.Machine,
                ComponentId = this.ComponentId,
                Component = this.Component,
                DateStart = this.DateStart,
                DateEnd = this.DateEnd,
                Status = this.Status,
                Documentation = this.Documentation,
                SignedById = this.SignedById,
                SignedBy = this.SignedBy,
                SignedByName = this.SignedByName,
                NextDue = this.NextDue,
                Comment = this.Comment,
                DigitalSignature = this.DigitalSignature,
                EntryHash = this.EntryHash,
                CreatedAt = this.CreatedAt,
                CreatedById = this.CreatedById,
                CreatedBy = this.CreatedBy,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy,
                SourceIp = this.SourceIp,
                WorkflowStatus = this.WorkflowStatus,
                AdditionalSigners = this.AdditionalSigners,
                Regulator = this.Regulator,
                AnomalyScore = this.AnomalyScore,
                LinkedCapaId = this.LinkedCapaId,
                LinkedCapa = this.LinkedCapa,
                SignatureTimestamp = this.SignatureTimestamp,
                SessionId = this.SessionId
            };
        }

        /// <summary>Readable string for logs/diagnostics.</summary>
        public override string ToString() => $"Validation {Code} ({Type}) – Status: {Status}, Signed by: {SignedByName}";
    }
}
