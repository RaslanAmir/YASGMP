// File: YASGMP/Services/DatabaseService.ComponentOverloads.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector; // MySQL ADO.NET provider
using YasGMP.Models;
using YasGMP.Helpers;
using YasGMP.Common;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Partial extension of <see cref="DatabaseService"/> providing **compatibility overloads**
    /// for component CRUD. These forward to robust, schema-tolerant implementations here,
    /// removing any dependency on other overloads that may not exist in the current build.
    /// <para>
    /// • Intentionally uses positional parameters to avoid CS1739 issues with named args.<br/>
    /// • Logs extended-context actions via <see cref="LogSystemEventAsync"/> for traceability.<br/>
    /// • Uses reflection-safe getters defined on <see cref="DatabaseService"/> to stay tolerant to model drift.
    /// </para>
    /// </summary>
    public partial class DatabaseService
    {
        private static IAuthContext? TryResolveAuthContext() => ServiceLocator.GetService<IAuthContext>();

        private static string ResolveIp(string? ip)
        {
            if (!string.IsNullOrWhiteSpace(ip) && !string.Equals(ip, "ui", StringComparison.OrdinalIgnoreCase))
            {
                return ip!;
            }

            try
            {
                var auth = TryResolveAuthContext();
                if (!string.IsNullOrWhiteSpace(auth?.CurrentIpAddress))
                {
                    return auth!.CurrentIpAddress;
                }
            }
            catch
            {
            }

            var fallback = ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress();
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            return ip ?? string.Empty;
        }

        private static int ResolveActorUserId(int actorUserId)
        {
            if (actorUserId != 0)
            {
                return actorUserId;
            }

            try
            {
                return TryResolveAuthContext()?.CurrentUser?.Id ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string? ResolveDeviceInfo(string? deviceInfo)
        {
            if (!string.IsNullOrWhiteSpace(deviceInfo))
            {
                return deviceInfo;
            }

            try
            {
                return TryResolveAuthContext()?.CurrentDeviceInfo;
            }
            catch
            {
                return deviceInfo;
            }
        }

        private static string ResolveSessionId(string? sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId!;
            }

            try
            {
                return TryResolveAuthContext()?.CurrentSessionId ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // ======================= INSERT / UPDATE (Component) =======================

        /// <summary>
        /// Inserts a new component or updates an existing one using the domain <see cref="Component"/>.
        /// Schema-tolerant and aligned with table <c>machine_components</c>.
        /// </summary>
        /// <param name="component">Domain component.</param>
        /// <param name="update"><c>true</c> to update; <c>false</c> to insert.</param>
        /// <param name="actorUserId">Acting user id for audit trail.</param>
        /// <param name="ip">Source IP (defaults to "system").</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Primary key of the affected component.</returns>
        public async Task<int> InsertOrUpdateComponentAsync(
            Component component,
            bool update,
            int actorUserId,
            string ip = "system",
            string? deviceInfo = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            if (component is null) throw new ArgumentNullException(nameof(component));

            // Wire real user/IP when caller passed defaults
            actorUserId = ResolveActorUserId(actorUserId);
            ip = ResolveIp(ip);
            deviceInfo = ResolveDeviceInfo(deviceInfo);
            sessionId = ResolveSessionId(sessionId);

            // Map to low-level DTO to stay consistent with DB column names.
            var c = ComponentMapper.ToMachineComponent(component);
            var lastModified = TryGet<DateTime>(c, nameof(component.LastModified)) ?? DateTime.UtcNow;
            var signature = TryGetString(c, nameof(component.DigitalSignature));

            string sql = !update
                ? @"INSERT INTO machine_components
                       (machine_id, code, name, type, sop_doc, status, install_date, last_modified_by_id,
                        digital_signature, last_modified, source_ip, device_info, session_id)
                    VALUES
                       (@mid,@code,@name,@type,@sop,@status,@install,@modby,@sig,@last,@ip,@device,@session)"
                : @"UPDATE machine_components SET
                       machine_id=@mid, code=@code, name=@name, type=@type, sop_doc=@sop, status=@status,
                       install_date=@install, last_modified_by_id=@modby, digital_signature=@sig,
                       last_modified=@last, source_ip=@ip, device_info=@device, session_id=@session
                   WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@mid",     (object?)TryGet<int>(c, "MachineId") ?? DBNull.Value),
                new("@code",    (object?)TryGetString(c, "Code")     ?? DBNull.Value),
                new("@name",    (object?)TryGetString(c, "Name")     ?? DBNull.Value),
                new("@type",    (object?)TryGetString(c, "Type")     ?? DBNull.Value),
                new("@sop",     (object?)TryGetString(c, "SopDoc")   ?? DBNull.Value),
                new("@status",  (object?)TryGetString(c, "Status")   ?? DBNull.Value),
                new("@install", (object?)TryGet<DateTime>(c, "InstallDate") ?? DBNull.Value),
                new("@modby",   actorUserId),
                new("@sig",     (object?)signature ?? DBNull.Value),
                new("@last",    lastModified),
                new("@ip",      string.IsNullOrWhiteSpace(ip) ? (object)DBNull.Value : ip),
                new("@device",  string.IsNullOrWhiteSpace(deviceInfo) ? (object)DBNull.Value : deviceInfo),
                new("@session", string.IsNullOrWhiteSpace(sessionId) ? (object)DBNull.Value : sessionId)
            };
            if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(c, "Id") ?? 0));

            await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

            int id = update
                ? (TryGet<int>(c, "Id") ?? 0)
                : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: update ? "COMP_UPDATE" : "COMP_CREATE",
                tableName: "machine_components",
                module: "MachineModule",
                recordId: id,
                description: update ? "Component updated" : "Component created",
                ip: ip,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: string.IsNullOrWhiteSpace(sessionId) ? null : sessionId
            ).ConfigureAwait(false);

            return id;
        }

        /// <summary>
        /// Overload (domain <see cref="Component"/> + device context) that persists and logs with client context.
        /// </summary>
        public async Task<int> InsertOrUpdateComponentAsync(
            Component component,
            bool update,
            int actorUserId,
            string ip,
            string deviceInfo,
            CancellationToken token = default)
        {
            // Wire real user/IP when caller passed defaults
            actorUserId = ResolveActorUserId(actorUserId);
            ip = ResolveIp(ip);
            string? resolvedDevice = ResolveDeviceInfo(deviceInfo);
            int id = await InsertOrUpdateComponentAsync(
                component,
                update,
                actorUserId,
                ip,
                resolvedDevice,
                sessionId: null,
                token: token
            ).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: update ? "COMP_UPDATE_CTX" : "COMP_CREATE_CTX",
                tableName: "machine_components",
                module: "MachineModule",
                recordId: id,
                description: $"client-device={deviceInfo ?? string.Empty}",
                ip: ip,
                severity: "info",
                deviceInfo: resolvedDevice,
                sessionId: null
            ).ConfigureAwait(false);

            return id;
        }

        /// <summary>
        /// Overload (domain <see cref="Component"/> + device + session) that persists and logs with session context.
        /// </summary>
        public async Task<int> InsertOrUpdateComponentAsync(
            Component component,
            bool update,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            string? resolvedDevice = ResolveDeviceInfo(deviceInfo);
            sessionId = ResolveSessionId(sessionId);
            int id = await InsertOrUpdateComponentAsync(
                component,
                update,
                actorUserId,
                ip,
                resolvedDevice,
                sessionId,
                token
            ).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: update ? "COMP_UPDATE_CTX" : "COMP_CREATE_CTX",
                tableName: "machine_components",
                module: "MachineModule",
                recordId: id,
                description: $"client-device={deviceInfo ?? string.Empty}",
                ip: ip,
                severity: "info",
                deviceInfo: resolvedDevice,
                sessionId: sessionId
            ).ConfigureAwait(false);

            return id;
        }

        /// <summary>
        /// Overload that accepts low-level <see cref="MachineComponent"/> and logs device context.
        /// (implemented via conversion to domain model to avoid relying on other overloads)
        /// </summary>
        public Task<int> InsertOrUpdateComponentAsync(
            MachineComponent component,
            bool update,
            int actorUserId,
            string ip,
            string deviceInfo,
            CancellationToken token = default)
        {
            var domain = ComponentMapper.ToComponent(component);
            return InsertOrUpdateComponentAsync(domain, update, actorUserId, ip, deviceInfo, sessionId: null, token: token);
        }

        /// <summary>
        /// Overload that accepts low-level <see cref="MachineComponent"/> and logs device + session context.
        /// (implemented via conversion to domain model to avoid relying on other overloads)
        /// </summary>
        public Task<int> InsertOrUpdateComponentAsync(
            MachineComponent component,
            bool update,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            var domain = ComponentMapper.ToComponent(component);
            return InsertOrUpdateComponentAsync(domain, update, actorUserId, ip, deviceInfo, sessionId, token);
        }

        // ======================= DELETE (Component) ================================

        /// <summary>
        /// Delete overload with device context (parity with machine delete).
        /// Performs the delete inline to avoid relying on other overloads.
        /// </summary>
        public async Task DeleteComponentAsync(
            int id,
            int actorUserId,
            string ip,
            string deviceInfo,
            CancellationToken token = default)
        {
            // Wire real user/IP when caller passed defaults
            actorUserId = ResolveActorUserId(actorUserId);
            ip = ResolveIp(ip);
            await ExecuteNonQueryAsync(
                "DELETE FROM machine_components WHERE id=@id",
                new[] { new MySqlParameter("@id", id) },
                token
            ).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: "COMP_DELETE_CTX",
                tableName: "machine_components",
                module: "MachineModule",
                recordId: id,
                description: "Component deleted (extended context).",
                ip: ip,
                severity: "info",
                deviceInfo: deviceInfo,
                sessionId: null
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete overload with device + session context.
        /// Performs the delete inline to avoid relying on other overloads.
        /// </summary>
        public async Task DeleteComponentAsync(
            int id,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            // Wire real user/IP when caller passed defaults
            actorUserId = ResolveActorUserId(actorUserId);
            ip = ResolveIp(ip);
            await ExecuteNonQueryAsync(
                "DELETE FROM machine_components WHERE id=@id",
                new[] { new MySqlParameter("@id", id) },
                token
            ).ConfigureAwait(false);

            sessionId = ResolveSessionId(sessionId);

            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: "COMP_DELETE_CTX",
                tableName: "machine_components",
                module: "MachineModule",
                recordId: id,
                description: "Component deleted (extended context).",
                ip: ip,
                severity: "info",
                deviceInfo: deviceInfo,
                sessionId: sessionId
            ).ConfigureAwait(false);
        }

        // ======================= SAVE (helper) =====================================

        /// <summary>
        /// Convenience wrapper that chooses insert vs update from <c>component.Id</c>.
        /// </summary>
        /// <param name="component">Domain component.</param>
        /// <param name="actorUserId">Acting user id.</param>
        /// <param name="ip">Source IP.</param>
        /// <param name="deviceInfo">Client device descriptor.</param>
        /// <param name="sessionId">Optional session id.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Primary key of the affected component.</returns>
        public Task<int> SaveComponentAsync(
            Component component,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            if (component is null) throw new ArgumentNullException(nameof(component));
            bool update = component.Id > 0;
            return InsertOrUpdateComponentAsync(component, update, actorUserId, ip, deviceInfo, sessionId, token);
        }
    }
}

