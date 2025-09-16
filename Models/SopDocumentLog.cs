using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("sop_document_log")]
    public class SopDocumentLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sop_document_id")]
        public int? SopDocumentId { get; set; }

        [Column("action")]
        public string? Action { get; set; }

        [Column("performed_by")]
        public int? PerformedBy { get; set; }

        [Column("performed_at")]
        public DateTime? PerformedAt { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
