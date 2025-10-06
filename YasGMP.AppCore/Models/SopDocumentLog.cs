using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `sop_document_log` table.</summary>
    [Table("sop_document_log")]
    public class SopDocumentLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the sop document id.</summary>
        [Column("sop_document_id")]
        public int? SopDocumentId { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        public string? Action { get; set; }

        /// <summary>Gets or sets the performed by.</summary>
        [Column("performed_by")]
        public int? PerformedBy { get; set; }

        /// <summary>Gets or sets the performed at.</summary>
        [Column("performed_at")]
        public DateTime? PerformedAt { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(SopDocumentId))]
        public virtual SopDocument? SopDocument { get; set; }

        [ForeignKey(nameof(PerformedBy))]
        public virtual User? PerformedByNavigation { get; set; }
    }
}

