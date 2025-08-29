using System;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Collision-free audit helpers for <see cref="DatabaseService"/>.
    /// These use unique method names to avoid extension overload clashes with other domains (e.g., Asset).
    /// </summary>
    public static class DatabaseServiceAuditHelpers
    {
        /// <summary>
        /// Writes a user-centric audit/system event entry without colliding with any Asset/other-domain
        /// <c>Log*AuditAsync</c> extension signatures.
        /// </summary>
        /// <param name="db">Database service instance.</param>
        /// <param name="user">User entity involved (nullable for bulk/system actions).</param>
        /// <param name="action">Action verb (CREATE, UPDATE, DELETE, EXPORT, etc.).</param>
        /// <param name="ip">Source IP.</param>
        /// <param name="deviceInfo">Device information string.</param>
        /// <param name="sessionId">Optional session id.</param>
        /// <param name="details">Optional details or signature hash.</param>
        /// <param name="token">Cancellation token.</param>
        public static Task WriteUserAuditAsync(
            this DatabaseService db,
            User? user,
            string action,
            string ip,
            string deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            action ??= "UPDATE";

            string description = user is null
                ? $"USER {action}: {details ?? "-"}"
                : $"USER {action}: {user.Username} ({user.FullName}). {details ?? string.Empty}".Trim();

            return db.LogSystemEventAsync(
                userId: user?.Id,
                eventType: $"USER_{action}",
                tableName: "users",
                module: "UserModule",
                recordId: user?.Id,
                description: description,
                ip: ip,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: sessionId,
                token: token);
        }

        /// <summary>
        /// Writes a validation-centric audit/system event entry (no name collisions).
        /// </summary>
        /// <param name="db">Database service instance.</param>
        /// <param name="validation">Validation entity involved (nullable for export/bulk).</param>
        /// <param name="action">Action verb (CREATE, UPDATE, DELETE, EXPORT, etc.).</param>
        /// <param name="ip">Source IP (nullable).</param>
        /// <param name="deviceInfo">Device information (nullable).</param>
        /// <param name="sessionId">Session identifier (nullable).</param>
        /// <param name="signatureHash">Optional signature hash for traceability.</param>
        /// <param name="token">Cancellation token.</param>
        public static Task WriteValidationAuditEntryAsync(
            this DatabaseService db,
            Validation? validation,
            string action,
            string? ip,
            string? deviceInfo,
            string? sessionId,
            string? signatureHash,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            action ??= "UPDATE";

            string subject = validation?.Code ?? validation?.Type ?? validation?.ValidationType ?? "?";
            string description = validation is null
                ? $"VALIDATION {action}. sig={signatureHash ?? "-"}"
                : $"VALIDATION {action}: {subject} (Id={validation.Id}). sig={signatureHash ?? "-"}";

            return db.LogSystemEventAsync(
                userId: null,
                eventType: $"VALIDATION_{action}",
                tableName: "validations",
                module: "ValidationModule",
                recordId: validation?.Id,
                description: description,
                ip: ip,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: sessionId,
                token: token);
        }
    }
}
