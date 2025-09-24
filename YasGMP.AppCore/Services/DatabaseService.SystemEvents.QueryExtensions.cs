// ==============================================================================
// File: Services/DatabaseService.SystemEvents.QueryExtensions.cs
// Purpose: Query system_event_log as SystemEvent POCOs with filters
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions surfacing system event log queries for diagnostics.
    /// </summary>
    public static class DatabaseServiceSystemEventsQueryExtensions
    {
        public static async Task<List<SystemEvent>> GetSystemEventsAsync(
            this DatabaseService db,
            int? userId,
            string? module,
            string? tableName,
            string? severity,
            DateTime? from,
            DateTime? to,
            bool? processed,
            int limit = 1000,
            int offset = 0,
            CancellationToken token = default)
        {
            string sql = "SELECT id, ts_utc, user_id, event_type, table_name, related_module, record_id, field_name, old_value, new_value, description, source_ip, device_info, session_id, severity, processed FROM system_event_log WHERE 1=1";
            var pars = new List<MySqlParameter>();
            if (userId.HasValue)   { sql += " AND user_id=@uid";   pars.Add(new MySqlParameter("@uid", userId)); }
            if (!string.IsNullOrWhiteSpace(module))    { sql += " AND related_module=@m"; pars.Add(new MySqlParameter("@m", module)); }
            if (!string.IsNullOrWhiteSpace(tableName)) { sql += " AND table_name=@t";    pars.Add(new MySqlParameter("@t", tableName)); }
            if (!string.IsNullOrWhiteSpace(severity))  { sql += " AND severity=@s";      pars.Add(new MySqlParameter("@s", severity)); }
            if (from.HasValue)      { sql += " AND ts_utc>=@f"; pars.Add(new MySqlParameter("@f", from.Value)); }
            if (to.HasValue)        { sql += " AND ts_utc<=@to"; pars.Add(new MySqlParameter("@to", to.Value)); }
            if (processed.HasValue) { sql += " AND processed=@p"; pars.Add(new MySqlParameter("@p", processed.Value)); }
            sql += " ORDER BY ts_utc DESC, id DESC LIMIT @lim OFFSET @off";
            pars.Add(new MySqlParameter("@lim", limit));
            pars.Add(new MySqlParameter("@off", offset));

            var dt = await db.ExecuteSelectAsync(sql, pars, token).ConfigureAwait(false);
            var list = new List<SystemEvent>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        private static SystemEvent Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int? I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : DateTime.UtcNow;
            bool B(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);

            return new SystemEvent
            {
                Id = Convert.ToInt32(r["id"]),
                EventTime = D("ts_utc"),
                UserId = I("user_id"),
                EventType = S("event_type"),
                TableName = S("table_name"),
                RelatedModule = S("related_module"),
                RecordId = I("record_id"),
                FieldName = S("field_name"),
                OldValue = S("old_value"),
                NewValue = S("new_value"),
                Description = S("description"),
                SourceIp = S("source_ip"),
                DeviceInfo = S("device_info"),
                SessionId = S("session_id"),
                Severity = S("severity"),
                Processed = B("processed")
            };
        }
    }
}

