// ==============================================================================
// File: Services/DatabaseService.Notifications.Extensions.cs
// Purpose: Minimal Notifications list/CRUD/workflow/audit/export shims
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    public static class DatabaseServiceNotificationsExtensions
    {
        public static async Task<List<Notification>> GetAllNotificationsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            try
            {
                var dt = await db.ExecuteSelectAsync("SELECT * FROM notifications /* ANALYZER_IGNORE: legacy table */ ORDER BY created_at DESC, id DESC", null, token).ConfigureAwait(false);
                var list = new List<Notification>(dt.Rows.Count);
                foreach (DataRow r in dt.Rows) list.Add(Map(r));
                return list;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                const string sql = @"
SELECT nq.id,
       COALESCE(nt.name, nt.code, 'Notification') AS title,
       JSON_UNQUOTE(JSON_EXTRACT(nq.payload, '$.message')) AS message,
       nq.channel AS type,
       nq.status,
       NULL AS entity,
       NULL AS entity_id,
       NULL AS link,
       NULL AS recipients,
       nq.recipient_user_id AS recipient_id,
       NULL AS sender_id,
       NULL AS ip_address,
       NULL AS device_info,
       NULL AS session_id,
       nq.created_at,
       nq.updated_at
FROM notification_queue nq
LEFT JOIN notification_templates nt ON nt.id = nq.template_id
ORDER BY nq.created_at DESC, nq.id DESC;";
                var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
                var list = new List<Notification>(dt.Rows.Count);
                foreach (DataRow r in dt.Rows) list.Add(Map(r));
                return list;
            }
        }

        public static async Task<int> SendNotificationAsync(this DatabaseService db, Notification n, CancellationToken token = default)
        {
            if (n == null) throw new ArgumentNullException(nameof(n));
            const string sql = @"INSERT INTO notifications /* ANALYZER_IGNORE: legacy table */ (title, message, type, priority, status, entity, entity_id, link, recipients, recipient_id, sender_id, ip_address, device_info, session_id, created_at)
                                VALUES (@title,@msg,@type,@prio,@status,@entity,@eid,@link,@recips,@rid,@sid,@ip,@dev,@sess,NOW())";
            var pars = new List<MySqlParameter>
            {
                new("@title", n.Title ?? string.Empty),
                new("@msg", n.Message ?? string.Empty),
                new("@type", n.Type ?? string.Empty),
                new("@prio", n.Priority ?? string.Empty),
                new("@status", n.Status ?? Notification.Statuses.New),
                new("@entity", (object?)n.Entity ?? DBNull.Value),
                new("@eid", (object?)n.EntityId ?? DBNull.Value),
                new("@link", (object?)n.Link ?? DBNull.Value),
                new("@recips", (object?)n.Recipients ?? DBNull.Value),
                new("@rid", (object?)n.RecipientId ?? DBNull.Value),
                new("@sid", (object?)n.SenderId ?? DBNull.Value),
                new("@ip", (object?)n.IpAddress ?? DBNull.Value),
                new("@dev", (object?)n.DeviceInfo ?? DBNull.Value),
                new("@sess", (object?)n.SessionId ?? DBNull.Value)
            };
            try
            {
                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                n.Id = Convert.ToInt32(idObj);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                const string q = @"INSERT INTO notification_queue (template_id, recipient_user_id, channel, payload, scheduled_at, status)
                                  VALUES (NULL, @rid, @channel, JSON_OBJECT('title', @title, 'message', @msg, 'type', @type, 'priority', @prio), NOW(), 'queued')";
                var pars2 = new List<MySqlParameter>
                {
                    new("@rid", (object?)n.RecipientId ?? DBNull.Value),
                    new("@channel", "push"),
                    new("@title", n.Title ?? string.Empty),
                    new("@msg", n.Message ?? string.Empty),
                    new("@type", n.Type ?? string.Empty),
                    new("@prio", n.Priority ?? string.Empty)
                };
                await db.ExecuteNonQueryAsync(q, pars2, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                n.Id = Convert.ToInt32(idObj);
            }
            await db.LogNotificationAuditAsync(n.Id, n.SenderId ?? 0, "SEND", n.IpAddress ?? string.Empty, n.DeviceInfo ?? string.Empty, n.SessionId, null, token).ConfigureAwait(false);
            return n.Id;
        }

        // Overload matching ViewModel call order (notification, actorUserId, ip, deviceInfo, sessionId)
        public static async Task<int> SendNotificationAsync(this DatabaseService db, Notification notification, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            notification.SenderId = actorUserId;
            notification.IpAddress = ip;
            notification.DeviceInfo = deviceInfo;
            notification.SessionId = sessionId;
            return await db.SendNotificationAsync(notification, token).ConfigureAwait(false);
        }

        public static Task LogNotificationAuditAsync(this DatabaseService db, int notificationId, int userId, string action, string ip, string deviceInfo, string? sessionId, string? note, CancellationToken token = default)
            => db.LogSystemEventAsync(userId, $"NOTIF_{action}", "notifications", "Notifications", notificationId == 0 ? null : notificationId, note, ip, "audit", deviceInfo, sessionId, token: token);

        public static async Task AcknowledgeNotificationAsync(this DatabaseService db, int notificationId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? note = null, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE notifications /* ANALYZER_IGNORE: legacy table */ SET status='acknowledged', acked_by=@u, acked_at=NOW() WHERE id=@id", new[] { new MySqlParameter("@u", actorUserId), new MySqlParameter("@id", notificationId) }, token).ConfigureAwait(false); } catch { }
            await db.LogNotificationAuditAsync(notificationId, actorUserId, "ACK", ip, deviceInfo, sessionId, note, token).ConfigureAwait(false);
        }

        public static async Task MuteNotificationAsync(this DatabaseService db, int notificationId, DateTime mutedUntilUtc, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE notifications /* ANALYZER_IGNORE: legacy table */ SET status='muted', muted_until=@until WHERE id=@id", new[] { new MySqlParameter("@until", mutedUntilUtc), new MySqlParameter("@id", notificationId) }, token).ConfigureAwait(false); } catch { }
            await db.LogNotificationAuditAsync(notificationId, actorUserId, "MUTE", ip, deviceInfo, sessionId, $"until={mutedUntilUtc:u}", token).ConfigureAwait(false);
        }

        public static async Task DeleteNotificationAsync(this DatabaseService db, int notificationId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("DELETE FROM notifications /* ANALYZER_IGNORE: legacy table */ WHERE id=@id", new[] { new MySqlParameter("@id", notificationId) }, token).ConfigureAwait(false); } catch { }
            await db.LogNotificationAuditAsync(notificationId, actorUserId, "DELETE", ip, deviceInfo, sessionId, null, token).ConfigureAwait(false);
        }

        public static async Task<int> ExportNotificationsAsync(this DatabaseService db, List<Notification> rows, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            await db.LogNotificationAuditAsync(0, actorUserId, "EXPORT", ip, deviceInfo, sessionId, $"count={rows?.Count ?? 0}", token).ConfigureAwait(false);
            return rows?.Count ?? 0;
        }

        private static Notification Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new Notification
            {
                Id = I("id"),
                Title = S("title"),
                Message = S("message"),
                Type = S("type"),
                Priority = S("priority"),
                Status = S("status"),
                Entity = S("entity"),
                EntityId = IN("entity_id"),
                Link = S("link"),
                Recipients = S("recipients"),
                RecipientId = IN("recipient_id"),
                SenderId = IN("sender_id"),
                IpAddress = S("ip_address"),
                DeviceInfo = S("device_info"),
                SessionId = S("session_id"),
                CreatedAt = D("created_at") ?? DateTime.UtcNow,
                UpdatedAt = D("updated_at")
            };
        }
    }
}
