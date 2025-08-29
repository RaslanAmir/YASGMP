using System;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>IntegrationLog</b> – Logovi integracija s vanjskim sustavima, API poziva i webhookova.
    /// Prati zahtjeve, odgovore, status i vrijeme obrade.
    /// <para>
    /// ✅ Logs every integration/API/webhook request/response<br/>
    /// ✅ Full traceability of request/response and outcome<br/>
    /// ✅ Audit and forensics ready
    /// </para>
    /// </summary>
    public class IntegrationLog
    {
        /// <summary>
        /// Jedinstveni ID log zapisa.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Naziv integriranog sustava.
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint koji je pozvan.
        /// </summary>
        public string ApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Vrijeme zahtjeva.
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// JSON poslani zahtjev.
        /// </summary>
        public string RequestJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON odgovor.
        /// </summary>
        public string ResponseJson { get; set; } = string.Empty;

        /// <summary>
        /// HTTP status kod odgovora.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Označava je li zahtjev obrađen.
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// Vrijeme obrade.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
