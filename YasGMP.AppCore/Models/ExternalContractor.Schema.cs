using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema companion for <see cref="ExternalContractor"/> exposing raw database columns and legacy serialized fields.
    /// </summary>
    public partial class ExternalContractor
    {
        [Column("service_type")]
        public string? ServiceTypeRaw
        {
            get => ServiceType;
            set => ServiceType = value;
        }

        [Column("contact")]
        public string? ContactRaw
        {
            get => Contact;
            set => Contact = value;
        }

        [Column("doc_file")]
        public string? DocFileRaw
        {
            get => DocFile;
            set => DocFile = value;
        }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("supplier?")]
        public string? SupplierLabel { get; set; }

        [Column("interventions")]
        public string? InterventionsRaw { get; set; }

        [Column("attachments")]
        public string? AttachmentsRaw { get; set; }

        [Column("audit_logs")]
        public string? AuditLogsRaw { get; set; }

        [Column("comment")]
        public string? CommentRaw { get; set; }

        [Column("status")]
        public string? StatusRaw
        {
            get => Status;
            set => Status = value;
        }

        [Column("cooperation_start")]
        public string? CooperationStartRaw { get; set; }

        [Column("cooperation_end")]
        public string? CooperationEndRaw { get; set; }

        [Column("code")]
        public string? ContractorCode { get; set; }
    }
}
