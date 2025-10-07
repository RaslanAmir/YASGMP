using System;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// Aggregates the context required to persist a work-order signature together with its manifest metadata.
    /// </summary>
    public sealed record WorkOrderSignaturePersistRequest
    {
        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        public int WorkOrderId { get; init; }
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public int UserId { get; init; }
        /// <summary>
        /// Gets or sets the signature hash.
        /// </summary>
        public string SignatureHash { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the reason code.
        /// </summary>
        public string ReasonCode { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the reason description.
        /// </summary>
        public string? ReasonDescription { get; init; }
        /// <summary>
        /// Gets or sets the signature type.
        /// </summary>
        public string SignatureType { get; init; } = "potvrda";
        /// <summary>
        /// Gets or sets the record hash.
        /// </summary>
        public string RecordHash { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the record version.
        /// </summary>
        public int RecordVersion { get; init; }
        /// <summary>
        /// Gets or sets the signed at utc.
        /// </summary>
        public DateTime SignedAtUtc { get; init; }
        /// <summary>
        /// Gets or sets the server time zone.
        /// </summary>
        public string ServerTimeZone { get; init; } = TimeZoneInfo.Local.Id;
        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        public string? IpAddress { get; init; }
        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        public string? DeviceInfo { get; init; }
        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        public string? SessionId { get; init; }
        /// <summary>
        /// Gets or sets the revision no.
        /// </summary>
        public int RevisionNo { get; init; }
        /// <summary>
        /// Gets or sets the mfa evidence.
        /// </summary>
        public string? MfaEvidence { get; init; }
        /// <summary>
        /// Gets or sets the work order snapshot json.
        /// </summary>
        public string? WorkOrderSnapshotJson { get; init; }
        /// <summary>
        /// Gets or sets the signer username.
        /// </summary>
        public string SignerUsername { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the signer full name.
        /// </summary>
        public string? SignerFullName { get; init; }
        /// <summary>
        /// Gets or sets the reason display.
        /// </summary>
        public string ReasonDisplay { get; init; } = string.Empty;
    }
}

