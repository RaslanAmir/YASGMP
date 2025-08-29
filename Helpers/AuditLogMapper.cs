// File: Helpers/AuditLogMapper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using YasGMP.Models;
using YasGMP.Models.DTO;

namespace YasGMP.Helpers
{
    /// <summary>
    /// <b>AuditLogMapper</b> – Static helper for robust mapping between <see cref="AuditEntryDto"/> (DTO) and <see cref="AuditLogEntry"/> (domain model).
    /// <para>
    /// ✅ Used for displaying, exporting, and forensically investigating audit log entries in GMP/Annex 11/21 CFR Part 11 compliant systems.<br/>
    /// ✅ Ensures safe, reliable transfer of all critical audit fields, including digital signatures, user, action, old/new values, and session metadata.<br/>
    /// ✅ Handles nulls, type conversions, and defensive fallback for maximum data integrity.
    /// </para>
    /// </summary>
    public static class AuditLogMapper
    {
        /// <summary>
        /// Maps a collection of <see cref="AuditEntryDto"/> objects to a strongly-typed list of <see cref="AuditLogEntry"/> objects.
        /// </summary>
        /// <param name="dtos">An enumerable collection of <see cref="AuditEntryDto"/> objects to map. May be <c>null</c>.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="AuditLogEntry"/> objects (empty list if <paramref name="dtos"/> is <c>null</c>).</returns>
        public static List<AuditLogEntry> MapDtosToAuditLogEntries(IEnumerable<AuditEntryDto> dtos)
        {
            if (dtos is null)
                return new List<AuditLogEntry>();

            return dtos
                .Where(d => d is not null)
                .Select(d => MapDtoToAuditLogEntry(d!))
                .ToList();
        }

        /// <summary>
        /// Maps a single <see cref="AuditEntryDto"/> object to a <see cref="AuditLogEntry"/> model, with defensive checks and fallback logic.
        /// </summary>
        /// <param name="dto">The <see cref="AuditEntryDto"/> instance to map. Must not be <c>null</c>.</param>
        /// <returns>A mapped, non-null <see cref="AuditLogEntry"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is <c>null</c>.</exception>
        public static AuditLogEntry MapDtoToAuditLogEntry(AuditEntryDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            // Handle optional/variant members gracefully
            string entityType = TryGetProperty(dto, "EntityType") ?? dto.Entity ?? string.Empty;
            string details = TryGetProperty(dto, "Details") ?? dto.Note ?? string.Empty;

            return new AuditLogEntry
            {
                /// <inheritdoc cref="AuditLogEntry.Id"/>
                Id = TryGetId(dto),

                /// <inheritdoc cref="AuditLogEntry.EntityType"/>
                EntityType = entityType,

                /// <inheritdoc cref="AuditLogEntry.EntityId"/>
                EntityId = ParseEntityId(dto.EntityId),

                /// <inheritdoc cref="AuditLogEntry.PerformedBy"/>
                PerformedBy = dto.Username ?? dto.UserFullName ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.Action"/>
                Action = dto.Action ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.OldValue"/>
                OldValue = dto.OldValue ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.NewValue"/>
                NewValue = dto.NewValue ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.ChangedAt"/>
                ChangedAt = TryGetTimestamp(dto),

                /// <inheritdoc cref="AuditLogEntry.DeviceInfo"/>
                DeviceInfo = dto.DeviceInfo ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.IpAddress"/>
                IpAddress = dto.IpAddress ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.SessionId"/>
                SessionId = dto.SessionId ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.DigitalSignature"/>
                DigitalSignature = dto.DigitalSignature ?? dto.SignatureHash ?? string.Empty,

                /// <inheritdoc cref="AuditLogEntry.Note"/>
                Note = details
            };
        }

        /// <summary>
        /// Safely parses the <c>EntityId</c> from a string to an <c>int</c>. Returns <c>0</c> on failure or null input.
        /// </summary>
        /// <param name="entityIdStr">EntityId as a string (may be <c>null</c>, empty, or non-numeric).</param>
        /// <returns>Parsed integer value or 0 if parsing fails.</returns>
        private static int ParseEntityId(string? entityIdStr)
        {
            if (!string.IsNullOrWhiteSpace(entityIdStr) && int.TryParse(entityIdStr, out int id))
                return id;
            return 0;
        }

        /// <summary>
        /// Attempts to retrieve a robust integer ID from an <see cref="AuditEntryDto"/>.
        /// Handles both nullable and non-nullable scenarios for compatibility.
        /// </summary>
        /// <param name="dto">The <see cref="AuditEntryDto"/> instance.</param>
        /// <returns>The resolved ID as <c>int</c> (or 0 if not available).</returns>
        private static int TryGetId(AuditEntryDto dto)
        {
            if (dto.Id.HasValue)
                return dto.Id.Value;

            if (!string.IsNullOrWhiteSpace(dto.EntityId) && int.TryParse(dto.EntityId, out int parsed))
                return parsed;

            return 0;
        }

        /// <summary>
        /// Robustly determines the timestamp for the audit entry (supports multiple DTO shapes and fallback properties).
        /// </summary>
        /// <param name="dto">The <see cref="AuditEntryDto"/> instance.</param>
        /// <returns>The best available timestamp, or <see cref="DateTime.MinValue"/> if not found.</returns>
        private static DateTime TryGetTimestamp(AuditEntryDto dto)
        {
            if (dto.Timestamp != default)
                return dto.Timestamp;

            var type = dto.GetType();
            if (type.GetProperty("CreatedAt")?.GetValue(dto) is DateTime createdAt && createdAt != default)
                return createdAt;

            if (type.GetProperty("ActionAt")?.GetValue(dto) is DateTime actionAt && actionAt != default)
                return actionAt;

            return DateTime.MinValue;
        }

        /// <summary>
        /// Tries to get a property value by name via reflection, returning its string representation or <c>null</c> if missing.
        /// </summary>
        /// <param name="dto">DTO object.</param>
        /// <param name="property">Property name to search for.</param>
        /// <returns>String value of property or <c>null</c> if not found or value is <c>null</c>.</returns>
        private static string? TryGetProperty(AuditEntryDto dto, string property)
        {
            var prop = dto.GetType().GetProperty(property);
            if (prop is null)
                return null;

            var val = prop.GetValue(dto);
            return val?.ToString();
        }
    }
}
