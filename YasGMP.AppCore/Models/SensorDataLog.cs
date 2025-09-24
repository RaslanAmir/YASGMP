using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <c>SensorDataLog</c> – Ultra-robust forensic history of all IoT/sensor measurements in the GMP system.
    /// Tracks measurement details such as type, value, unit, device, timestamp, integrity hashes, AI anomaly flags,
    /// approval metadata, geolocation, and more for comprehensive audit and regulatory compliance.
    /// </summary>
    [Table("sensor_data_log")]
    public class SensorDataLog
    {
        /// <summary>
        /// Unique log entry identifier (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Component identifier where the measurement occurred (Foreign Key).
        /// </summary>
        [Required]
        [Column("component_id")]
        public int ComponentId { get; set; }

        /// <summary>
        /// Associated component details (navigation property).
        /// </summary>
        [ForeignKey(nameof(ComponentId))]
        public Component Component { get; set; } = default!;

        /// <summary>
        /// Sensor type (e.g., "temperature", "pressure", "humidity").
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Column("sensor_type")]
        public string SensorType { get; set; } = string.Empty;

        /// <summary>
        /// Unique sensor identifier (serial, MAC, or UUID) for traceability.
        /// </summary>
        [MaxLength(64)]
        [Column("sensor_id")]
        public string SensorId { get; set; } = string.Empty;

        /// <summary>
        /// Measured value (high precision decimal).
        /// </summary>
        [Column("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// Unit of measurement (e.g., "°C", "bar", "%RH").
        /// </summary>
        [MaxLength(20)]
        [Column("unit")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of measurement in UTC for audit purposes.
        /// </summary>
        [Required]
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Source device or system name (hostname, PLC, datalogger, etc.).
        /// </summary>
        [MaxLength(100)]
        [Column("device_name")]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Session or batch identifier if part of a data stream.
        /// </summary>
        [MaxLength(100)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Location descriptor (plant, room, pipeline, or GPS).
        /// </summary>
        [MaxLength(100)]
        [Column("location")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Optional geographic coordinates (latitude, longitude) in JSON or "lat,long" format.
        /// </summary>
        [MaxLength(64)]
        [Column("geo_coordinates")]
        public string GeoCoordinates { get; set; } = string.Empty;

        /// <summary>
        /// Data status indicator ("raw", "validated", "anomaly", "rejected", etc.).
        /// </summary>
        [MaxLength(32)]
        [Column("status")]
        public string Status { get; set; } = "raw";

        /// <summary>
        /// Integrity hash of the raw measurement (e.g., SHA-256) for tamper detection.
        /// </summary>
        [MaxLength(128)]
        [Column("data_hash")]
        public string DataHash { get; set; } = string.Empty;

        /// <summary>
        /// Optional chain-of-custody hash (blockchain or external ledger reference).
        /// </summary>
        [MaxLength(128)]
        [Column("chain_hash")]
        public string ChainHash { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature of the sensor or user process that validated the data.
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Optional descriptive comment or annotation by operator or system.
        /// </summary>
        [MaxLength(255)]
        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// File path or URI to raw data file, CSV export, or sensor log blob.
        /// </summary>
        [MaxLength(512)]
        [Column("data_file")]
        public string DataFile { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of user who approved or validated this reading.
        /// </summary>
        [Column("approved_by_id")]
        public int? ApprovedById { get; set; }

        /// <summary>
        /// Navigation to the approving user record.
        /// </summary>
        [ForeignKey(nameof(ApprovedById))]
        public User ApprovedBy { get; set; } = default!;

        /// <summary>
        /// UTC timestamp of approval or validation event.
        /// </summary>
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Optional AI anomaly score for smart alarms and trend analysis.
        /// </summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Flag indicating whether this reading was flagged as an anomaly by AI.
        /// </summary>
        [Column("is_anomaly")]
        public bool? IsAnomaly { get; set; }

        /// <summary>
        /// Classification tag ("alarm", "event", "normal", etc.).
        /// </summary>
        [MaxLength(32)]
        [Column("tag")]
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// Free-text note for inspection, maintenance, or audit purposes.
        /// </summary>
        [MaxLength(1000)]
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Returns a concise string representation for logging and debugging.
        /// </summary>
        public override string ToString() =>
            $"SensorDataLog [ID={Id}]: {SensorType}={Value}{Unit} @ {Timestamp:u} on {DeviceName} (Status={Status})";
    }
}
