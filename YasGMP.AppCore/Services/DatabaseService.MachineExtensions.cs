using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    /// <summary>
    /// Strongly typed extension helpers for <see cref="DatabaseService"/> that route calls
    /// unambiguously to the <c>Machine</c>-typed overloads. Using these removes CS1503
    /// (“cannot convert from 'Machine' to 'Asset'”) at call sites once and for all.
    /// </summary>
    public static class DatabaseServiceMachineExtensions
    {
        /// <summary>
        /// Creates a new <see cref="Machine"/> and returns its primary key.
        /// Routes to the Machine core InsertOrUpdate method with update=false.
        /// </summary>
        /// <param name="db">The database service instance.</param>
        /// <param name="m">Machine to persist.</param>
        /// <param name="actorUserId">User id performing the operation.</param>
        /// <param name="ip">Source IP address.</param>
        /// <param name="deviceInfo">Client device information.</param>
        /// <param name="sessionId">Optional session identifier.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The newly created machine's primary key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> or <paramref name="m"/> is <see langword="null"/>.</exception>
        public static Task<int> SaveMachineAsync(
            this DatabaseService db,
            Machine m,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            SignatureMetadataDto? signatureMetadata = null,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (m  is null) throw new ArgumentNullException(nameof(m));

            return db.InsertOrUpdateMachineAsync(m, update: false, actorUserId, ip, deviceInfo, sessionId, signatureMetadata, token);
        }

        /// <summary>
        /// Updates an existing <see cref="Machine"/> and returns its primary key.
        /// Routes to the Machine core InsertOrUpdate method with update=true.
        /// </summary>
        /// <param name="db">The database service instance.</param>
        /// <param name="m">Machine to update.</param>
        /// <param name="actorUserId">User id performing the operation.</param>
        /// <param name="ip">Source IP address.</param>
        /// <param name="deviceInfo">Client device information.</param>
        /// <param name="sessionId">Optional session identifier.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The affected machine's primary key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> or <paramref name="m"/> is <see langword="null"/>.</exception>
        public static Task<int> UpdateMachineAsync(
            this DatabaseService db,
            Machine m,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            SignatureMetadataDto? signatureMetadata = null,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (m  is null) throw new ArgumentNullException(nameof(m));

            return db.InsertOrUpdateMachineAsync(m, update: true, actorUserId, ip, deviceInfo, sessionId, signatureMetadata, token);
        }

        /// <summary>
        /// Rolls back a machine by re-applying the provided <paramref name="snapshot"/> via update.
        /// </summary>
        /// <param name="db">The database service instance.</param>
        /// <param name="snapshot">Snapshot of the machine to re-apply.</param>
        /// <param name="actorUserId">User id performing the operation.</param>
        /// <param name="ip">Source IP address.</param>
        /// <param name="deviceInfo">Client device information.</param>
        /// <param name="sessionId">Optional session identifier.</param>
        /// <param name="signatureMetadata">Optional electronic signature metadata (hash/IP/device/session).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The affected machine's primary key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> or <paramref name="snapshot"/> is <see langword="null"/>.</exception>
        public static Task<int> RollbackMachineFromSnapshotAsync(
            this DatabaseService db,
            Machine snapshot,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            SignatureMetadataDto? signatureMetadata = null,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

            return db.RollbackMachineAsync(snapshot, actorUserId, ip, deviceInfo, sessionId, signatureMetadata, token);
        }

        /// <summary>
        /// Exports the provided machine rows and returns the file path recorded in the export log.
        /// Routes to the Machine export helper and returns the file path.
        /// </summary>
        /// <param name="db">The database service instance.</param>
        /// <param name="rows">Rows to export (an empty sequence is used if <see langword="null"/>).</param>
        /// <param name="ip">Source IP address.</param>
        /// <param name="deviceInfo">Client device information.</param>
        /// <param name="sessionId">Optional session identifier.</param>
        /// <param name="format">Export format (default <c>"zip"</c>).</param>
        /// <param name="actorUserId">User id written to the export log (optional).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Path of the exported file as recorded in the log.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is <see langword="null"/>.</exception>
        public static Task<string> ExportMachinesFromViewAsync(
            this DatabaseService db,
            IEnumerable<Machine> rows,
            string ip,
            string deviceInfo,
            string? sessionId = null,
            string format = "zip",
            int actorUserId = 0,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            rows ??= Array.Empty<Machine>();

            return db.ExportMachinesAsync(rows, ip, deviceInfo, sessionId, format, actorUserId, token);
        }

        // Overload matching MachineViewModel invocation order (rows, actorUserId, ip, device, session)
        public static Task<string> ExportMachinesFromViewAsync(
            this DatabaseService db,
            IEnumerable<Machine> rows,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
            => db.ExportMachinesFromViewAsync(rows, ip, deviceInfo, sessionId, format: "zip", actorUserId: actorUserId, token: token);

        public static Task LogMachineAuditAsync(
            this DatabaseService db,
            int machineId,
            int userId,
            string action,
            string? note,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
            => db.LogSystemEventAsync(userId, $"MACHINE_{action}", "machines", "MachineModule", machineId == 0 ? null : machineId, note, ip, "audit", deviceInfo, sessionId, token: token);
    }
}
