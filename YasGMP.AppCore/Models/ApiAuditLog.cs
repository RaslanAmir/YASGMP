using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Database entity that records each API call for audit, linking to the key/user, action, and request context.
    /// </summary>
    [Table("api_audit_log")]
    public class ApiAuditLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the api key id.
        /// </summary>
        [Column("api_key_id")]
        public int? ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [Column("action")]
        [StringLength(255)]
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the request details.
        /// </summary>
        [Column("request_details")]
        public string? RequestDetails { get; set; }

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
        /// Gets or sets the details.
        /// </summary>
        [Column("details")]
        public string? Details { get; set; }
    }
}
