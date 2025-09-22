using System;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// Aggregates the context required to persist a work-order signature together with its manifest metadata.
    /// </summary>
    public sealed record WorkOrderSignaturePersistRequest
    {
        public int WorkOrderId { get; init; }
        public int UserId { get; init; }
        public string SignatureHash { get; init; } = string.Empty;
        public string ReasonCode { get; init; } = string.Empty;
        public string? ReasonDescription { get; init; }
        public string SignatureType { get; init; } = "potvrda";
        public string RecordHash { get; init; } = string.Empty;
        public int RecordVersion { get; init; }
        public DateTime SignedAtUtc { get; init; }
        public string ServerTimeZone { get; init; } = TimeZoneInfo.Local.Id;
        public string? IpAddress { get; init; }
        public string? DeviceInfo { get; init; }
        public string? SessionId { get; init; }
        public int RevisionNo { get; init; }
        public string? MfaEvidence { get; init; }
        public string? WorkOrderSnapshotJson { get; init; }
        public string SignerUsername { get; init; } = string.Empty;
        public string? SignerFullName { get; init; }
        public string ReasonDisplay { get; init; } = string.Empty;
    }
}

