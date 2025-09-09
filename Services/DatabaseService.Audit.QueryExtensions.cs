// ==============================================================================
// File: Services/DatabaseService.Audit.QueryExtensions.cs
// Purpose: Read audit logs for an entity from system_event_log (fallback aware)
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    public static class DatabaseServiceAuditQueryExtensions
    {
        /// <summary>
        /// Returns audit entries for a given entity table and record id.
        /// Prefers system_event_log and maps to AuditEntryDto.
        /// </summary>
        public static async Task<List<AuditEntryDto>> GetAuditLogForEntityAsync(
            this DatabaseService db,
            string entity,
            int entityId,
            CancellationToken token = default)
        {
            entity ??= "system";

            var list = new List<AuditEntryDto>();

            try
            {
                const string sql = @"SELECT id, ts_utc, user_id, event_type, table_name, related_module, record_id,
                                            field_name, old_value, new_value, description, source_ip, device_info, session_id, severity
                                     FROM system_event_log
                                     WHERE table_name=@t AND record_id=@id
                                     ORDER BY ts_utc DESC, id DESC";

                var pars = new[]
                {
                    new MySqlParameter("@t", entity),
                    new MySqlParameter("@id", entityId)
                };

                var dt = await db.ExecuteSelectAsync(sql, pars, token).ConfigureAwait(false);
                foreach (DataRow r in dt.Rows)
                {
                    var dto = new AuditEntryDto
                    {
                        Id = r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : (int?)null,
                        Entity = r["table_name"]?.ToString(),
                        EntityId = r["record_id"]?.ToString(),
                        Action = r["event_type"]?.ToString(),
                        Timestamp = r.Table.Columns.Contains("ts_utc") && r["ts_utc"] != DBNull.Value ? Convert.ToDateTime(r["ts_utc"]) : DateTime.UtcNow,
                        UserId = r["user_id"] != DBNull.Value ? Convert.ToInt32(r["user_id"]) : (int?)null,
                        IpAddress = r["source_ip"]?.ToString(),
                        DeviceInfo = r["device_info"]?.ToString(),
                        SessionId = r["session_id"]?.ToString(),
                        FieldName = r["field_name"]?.ToString(),
                        OldValue = r["old_value"]?.ToString(),
                        NewValue = r["new_value"]?.ToString(),
                        Status = r["severity"]?.ToString(),
                        Note = r["description"]?.ToString()
                    };
                    list.Add(dto);
                }
                return list;
            }
            catch (MySqlException)
            {
                // Fallback: if system_event_log missing, return empty list.
            }

            return list;
        }
    }
}

