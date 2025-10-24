using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>IntegrationLog</b> – Logovi integracija s vanjskim sustavima, API poziva i webhookova.
    /// Prati zahtjeve, odgovore, status i vrijeme obrade.
    /// <para>
    /// ✓ Logs every integration/API/webhook request/response<br/>
    /// ✓ Full traceability of request/response and outcome<br/>
    /// ✓ Audit and forensics ready
    /// </para>
    /// </summary>
    [Table("integration_log")]
    public partial class IntegrationLog
    {
        /// <summary>
        /// Jedinstveni ID log zapisa.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Naziv integriranog sustava.
        /// </summary>
        [MaxLength(100)]
        [Column("system_name")]
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint koji je pozvan.
        /// </summary>
        [MaxLength(255)]
        [Column("api_endpoint")]
        public string ApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Vrijeme zahtjeva.
        /// </summary>
        [Column("request_time")]
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// JSON poslani zahtjev.
        /// </summary>
        [Column("request_json")]
        public string RequestJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON odgovor.
        /// </summary>
        [Column("response_json")]
        public string ResponseJson { get; set; } = string.Empty;

        /// <summary>
        /// HTTP status kod odgovora.
        /// </summary>
        [Column("status_code")]
        public int? StatusCode { get; set; }

        /// <summary>
        /// Označava je li zahtjev obrađen.
        /// </summary>
        [Column("processed")]
        public bool Processed { get; set; }

        /// <summary>
        /// Vrijeme obrade.
        /// </summary>
        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Vrijeme kreiranja zapisa.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Vrijeme posljednje izmjene zapisa.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

