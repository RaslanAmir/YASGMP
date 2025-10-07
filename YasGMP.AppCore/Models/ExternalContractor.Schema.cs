using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema companion for <see cref="ExternalContractor"/> exposing raw database columns and legacy serialized fields.
    /// </summary>
    public partial class ExternalContractor
    {
        /// <summary>
        /// Represents the service type raw value.
        /// </summary>
        [Column("service_type")]
        public string? ServiceTypeRaw
        {
            get => ServiceType;
            set => ServiceType = value;
        }

        /// <summary>
        /// Represents the contact raw value.
        /// </summary>
        [Column("contact")]
        public string? ContactRaw
        {
            get => Contact;
            set => Contact = value;
        }

        /// <summary>
        /// Represents the doc file raw value.
        /// </summary>
        [Column("doc_file")]
        public string? DocFileRaw
        {
            get => DocFile;
            set => DocFile = value;
        }

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
        /// Gets or sets the supplier label.
        /// </summary>
        [Column("supplier?")]
        public string? SupplierLabel { get; set; }

        /// <summary>
        /// Gets or sets the interventions raw.
        /// </summary>
        [Column("interventions")]
        public string? InterventionsRaw { get; set; }

        /// <summary>
        /// Gets or sets the attachments raw.
        /// </summary>
        [Column("attachments")]
        public string? AttachmentsRaw { get; set; }

        /// <summary>
        /// Gets or sets the audit logs raw.
        /// </summary>
        [Column("audit_logs")]
        public string? AuditLogsRaw { get; set; }

        /// <summary>
        /// Gets or sets the comment raw.
        /// </summary>
        [Column("comment")]
        public string? CommentRaw { get; set; }

        /// <summary>
        /// Represents the status raw value.
        /// </summary>
        [Column("status")]
        public string? StatusRaw
        {
            get => Status;
            set => Status = value;
        }

        /// <summary>
        /// Gets or sets the cooperation start raw.
        /// </summary>
        [Column("cooperation_start")]
        public string? CooperationStartRaw { get; set; }

        /// <summary>
        /// Gets or sets the cooperation end raw.
        /// </summary>
        [Column("cooperation_end")]
        public string? CooperationEndRaw { get; set; }

        /// <summary>
        /// Gets or sets the contractor code.
        /// </summary>
        [Column("code")]
        public string? ContractorCode { get; set; }
    }
}
