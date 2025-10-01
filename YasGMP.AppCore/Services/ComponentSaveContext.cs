using System;

namespace YasGMP.Services
{
    /// <summary>
    /// Context metadata supplied by UI adapters when persisting components.
    /// Captures the electronic signature hash plus client network/session info
    /// so the database write can retain the adapter-provided values without
    /// recomputing hashes inside <see cref="ComponentService"/>.
    /// </summary>
    public sealed record class ComponentSaveContext
    {
        /// <summary>Optional persisted signature hash supplied by the adapter.</summary>
        public string? SignatureHash { get; init; }

        /// <summary>Client IP address associated with the save.</summary>
        public string? IpAddress { get; init; }

        /// <summary>Client device descriptor (workstation, terminal, etc.).</summary>
        public string? DeviceInfo { get; init; }

        /// <summary>Client session identifier for traceability.</summary>
        public string? SessionId { get; init; }

        /// <summary>
        /// Creates a normalized context, trimming values and returning
        /// <c>null</c> for empty inputs so downstream persistence writes
        /// <see cref="DBNull.Value"/> instead of blank strings.
        /// </summary>
        public static ComponentSaveContext Create(
            string? signatureHash,
            string? ipAddress,
            string? deviceInfo,
            string? sessionId)
            => new()
            {
                SignatureHash = Normalize(signatureHash),
                IpAddress = Normalize(ipAddress),
                DeviceInfo = Normalize(deviceInfo),
                SessionId = Normalize(sessionId)
            };

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}

