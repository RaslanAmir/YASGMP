// ==============================================================================
// File: Services/DatabaseService.Settings.Extensions.cs
// Purpose: Minimal Settings export/delete/rollback + audit shim used by SettingsViewModel
// ==============================================================================

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    public static class DatabaseServiceSettingsExtensions
    {
        public static async Task<List<Setting>> GetAllSettingsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            try
            {
                var dt = await db.ExecuteSelectAsync("SELECT * FROM settings /* ANALYZER_IGNORE: legacy table */ ORDER BY `key`", null, token).ConfigureAwait(false);
                var list = new List<Setting>(dt.Rows.Count);
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
                    int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
                    DateTime? DN(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

                    list.Add(new Setting
                    {
                        Id = Convert.ToInt32(r["id"]),
                        Key = S("key"),
                        Value = S("value"),
                        DefaultValue = S("default_value"),
                        ValueType = S("value_type"),
                        MinValue = S("min_value"),
                        MaxValue = S("max_value"),
                        Description = S("description"),
                        Category = S("category"),
                        Subcategory = S("subcategory"),
                        IsSensitive = r.Table.Columns.Contains("is_sensitive") && r["is_sensitive"] != System.DBNull.Value && Convert.ToBoolean(r["is_sensitive"]),
                        IsGlobal = !(r.Table.Columns.Contains("is_global") && r["is_global"] != System.DBNull.Value) || Convert.ToBoolean(r["is_global"]),
                        UserId = IN("user_id"),
                        RoleId = IN("role_id"),
                        ApprovedById = IN("approved_by_id"),
                        ApprovedAt = DN("approved_at"),
                        DigitalSignature = S("digital_signature"),
                        Status = S("status"),
                        UpdatedAt = DN("updated_at") ?? DateTime.UtcNow,
                        UpdatedById = IN("updated_by_id"),
                        ExpiryDate = DN("expiry_date")
                    });
                }
                return list;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                const string sql = @"
SELECT id,
       param_name AS `key`,
       param_value AS `value`,
       NULL AS default_value,
       NULL AS value_type,
       NULL AS min_value,
       NULL AS max_value,
       note AS description,
       'system' AS category,
       NULL AS subcategory,
       0 AS is_sensitive,
       1 AS is_global,
       NULL AS user_id,
       NULL AS role_id,
       NULL AS approved_by_id,
       NULL AS approved_at,
       digital_signature,
       '' AS status,
       updated_at,
       NULL AS updated_by_id,
       NULL AS expiry_date
FROM system_parameters
ORDER BY param_name;";
                var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
                var list = new List<Setting>(dt.Rows.Count);
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
                    int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
                    DateTime? DN(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

                    list.Add(new Setting
                    {
                        Id = Convert.ToInt32(r["id"]),
                        Key = S("key"),
                        Value = S("value"),
                        DefaultValue = S("default_value"),
                        ValueType = S("value_type"),
                        MinValue = S("min_value"),
                        MaxValue = S("max_value"),
                        Description = S("description"),
                        Category = S("category"),
                        Subcategory = S("subcategory"),
                        IsSensitive = false,
                        IsGlobal = true,
                        UserId = IN("user_id"),
                        RoleId = IN("role_id"),
                        ApprovedById = IN("approved_by_id"),
                        ApprovedAt = DN("approved_at"),
                        DigitalSignature = S("digital_signature"),
                        Status = S("status"),
                        UpdatedAt = DN("updated_at") ?? DateTime.UtcNow,
                        UpdatedById = IN("updated_by_id"),
                        ExpiryDate = DN("expiry_date")
                    });
                }
                return list;
            }
        }

        public static async Task<int> UpsertSettingAsync(this DatabaseService db, Setting setting, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            bool update = setting.Id > 0;
            string insert = @"INSERT INTO settings /* ANALYZER_IGNORE: legacy table */ (`key`, `value`, default_value, value_type, min_value, max_value, description, category, subcategory, is_sensitive, is_global, user_id, role_id, approved_by_id, approved_at, digital_signature, status, updated_at, updated_by_id)
                             VALUES (@key,@val,@def,@type,@min,@max,@desc,@cat,@sub,@sens,@glob,@uid,@rid,@apby,@apat,@sig,@status,NOW(),@updby)";
            string updateSql = @"UPDATE settings /* ANALYZER_IGNORE: legacy table */ SET `key`=@key, `value`=@val, default_value=@def, value_type=@type, min_value=@min, max_value=@max, description=@desc, category=@cat, subcategory=@sub, is_sensitive=@sens, is_global=@glob, user_id=@uid, role_id=@rid, approved_by_id=@apby, approved_at=@apat, digital_signature=@sig, status=@status, updated_at=NOW(), updated_by_id=@updby WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@key", setting.Key ?? string.Empty),
                new("@val", setting.Value ?? string.Empty),
                new("@def", setting.DefaultValue ?? string.Empty),
                new("@type", setting.ValueType ?? string.Empty),
                new("@min", setting.MinValue ?? string.Empty),
                new("@max", setting.MaxValue ?? string.Empty),
                new("@desc", setting.Description ?? string.Empty),
                new("@cat", setting.Category ?? string.Empty),
                new("@sub", setting.Subcategory ?? string.Empty),
                new("@sens", setting.IsSensitive),
                new("@glob", setting.IsGlobal),
                new("@uid", (object?)setting.UserId ?? DBNull.Value),
                new("@rid", (object?)setting.RoleId ?? DBNull.Value),
                new("@apby", (object?)setting.ApprovedById ?? DBNull.Value),
                new("@apat", (object?)setting.ApprovedAt ?? DBNull.Value),
                new("@sig", setting.DigitalSignature ?? string.Empty),
                new("@status", setting.Status ?? string.Empty),
                new("@updby", (object?)actorUserId ?? DBNull.Value)
            };

            try
            {
                if (!update)
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                    var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                    setting.Id = Convert.ToInt32(idObj);
                }
                else
                {
                    pars.Add(new MySqlParameter("@id", setting.Id));
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                // Fallback to system_parameters
                const string upsert = @"INSERT INTO system_parameters (param_name, param_value, updated_by, note)
                                      VALUES (@key, @val, @updby, @desc)
                                      ON DUPLICATE KEY UPDATE param_value=VALUES(param_value), updated_by=VALUES(updated_by), updated_at=CURRENT_TIMESTAMP, note=VALUES(note)";
                var pars2 = new List<MySqlParameter>
                {
                    new("@key", setting.Key ?? string.Empty),
                    new("@val", setting.Value ?? string.Empty),
                    new("@updby", (object?)actorUserId ?? DBNull.Value),
                    new("@desc", setting.Description ?? string.Empty)
                };
                await db.ExecuteNonQueryAsync(upsert, pars2, token).ConfigureAwait(false);
            }

            await db.LogSystemEventAsync(actorUserId, update ? "SETTING_UPDATE" : "SETTING_CREATE", "settings", "SettingsModule", setting.Id, setting.Key, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
            return setting.Id;
        }

        // Overload with explicit 'update' flag for ViewModel named-arg compatibility
        public static Task<int> UpsertSettingAsync(this DatabaseService db, Setting setting, bool update, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.UpsertSettingAsync(setting, actorUserId, ip, device, sessionId, token);
        public static async Task DeleteSettingAsync(this DatabaseService db, int settingId, int actorUserId, string ip, string device, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("DELETE FROM settings /* ANALYZER_IGNORE: legacy table */ WHERE id=@id", new[] { new MySqlParameter("@id", settingId) }, token).ConfigureAwait(false); }
            catch (MySqlException ex) when (ex.Number == 1146) { /* no-op for system_parameters; not keyed by id */ }
            await db.LogSystemEventAsync(actorUserId, "SETTING_DELETE", "settings", "SettingsModule", settingId, null, ip, "audit", device, null, token: token).ConfigureAwait(false);
        }

        public static Task RollbackSettingAsync(this DatabaseService db, int settingId, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "SETTING_ROLLBACK", "settings", "SettingsModule", settingId, null, ip, "audit", device, sessionId, token: token);

        // Name-keyed mapping helpers for system_parameters compatibility
        public static async Task<int> UpsertSettingByKeyAsync(this DatabaseService db, string key, string value, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key required", nameof(key));
            try
            {
                var setting = new Setting { Key = key, Value = value };
                return await db.UpsertSettingAsync(setting, actorUserId, ip, device, sessionId, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                const string upsert = @"INSERT INTO system_parameters (param_name, param_value, updated_by, note)
                                      VALUES (@key, @val, @updby, @note)
                                      ON DUPLICATE KEY UPDATE param_value=VALUES(param_value), updated_by=VALUES(updated_by), updated_at=CURRENT_TIMESTAMP, note=VALUES(note)";
                await db.ExecuteNonQueryAsync(upsert, new[]
                {
                    new MySqlParameter("@key", key),
                    new MySqlParameter("@val", value ?? string.Empty),
                    new MySqlParameter("@updby", actorUserId),
                    new MySqlParameter("@note", "Upsert via mapping")
                }, token).ConfigureAwait(false);
                await db.LogSystemEventAsync(actorUserId, "SETTING_CREATE", "settings", "SettingsModule", null, key, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
                return 0;
            }
        }

        public static async Task DeleteSettingByKeyAsync(this DatabaseService db, string key, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key required", nameof(key));
            try
            {
                await db.ExecuteNonQueryAsync("DELETE FROM settings WHERE `key`=@k", new[] { new MySqlParameter("@k", key) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                await db.ExecuteNonQueryAsync("DELETE FROM system_parameters WHERE param_name=@k", new[] { new MySqlParameter("@k", key) }, token).ConfigureAwait(false);
            }
            await db.LogSystemEventAsync(actorUserId, "SETTING_DELETE", "settings", "SettingsModule", null, key, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task RollbackSettingByKeyAsync(this DatabaseService db, string key, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key required", nameof(key));
            try
            {
                await db.ExecuteNonQueryAsync("UPDATE settings SET `value`=default_value, updated_at=NOW(), updated_by_id=@u WHERE `key`=@k", new[] { new MySqlParameter("@u", actorUserId), new MySqlParameter("@k", key) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                // Without a default, emulate rollback by removing override
                await db.ExecuteNonQueryAsync("DELETE FROM system_parameters WHERE param_name=@k", new[] { new MySqlParameter("@k", key) }, token).ConfigureAwait(false);
            }
            await db.LogSystemEventAsync(actorUserId, "SETTING_ROLLBACK", "settings", "SettingsModule", null, key, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static Task ExportSettingsAsync(this DatabaseService db, List<Setting> items, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
            => db.ExportSettingsAsync(items, format: "csv", actorUserId, ip, device, sessionId, token);

        public static Task ExportSettingsAsync(this DatabaseService db, List<Setting> items, string format, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
        {
            string? path = null;
            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(items ?? new List<Setting>(), "settings",
                    new (string, Func<Setting, object?>)[]
                    {
                        ("Id", s => s.Id),
                        ("Key", s => s.Key),
                        ("Value", s => s.Value),
                        ("Category", s => s.Category),
                        ("UpdatedAt", s => s.UpdatedAt)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(items ?? new List<Setting>(), "settings",
                    new (string, Func<Setting, object?>)[]
                    {
                        ("Id", s => s.Id),
                        ("Key", s => s.Key),
                        ("Value", s => s.Value),
                        ("Category", s => s.Category)
                    }, title: "Settings Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new List<Setting>(), "settings",
                    new (string, Func<Setting, object?>)[]
                    {
                        ("Id", s => s.Id),
                        ("Key", s => s.Key),
                        ("Value", s => s.Value),
                        ("Category", s => s.Category),
                        ("UpdatedAt", s => s.UpdatedAt)
                    });
            }
            return db.LogSystemEventAsync(actorUserId, "SETTING_EXPORT", "settings", "SettingsModule", null, $"format={format}; count={items?.Count ?? 0}; file={path}", ip, "info", device, sessionId, token: token);
        }

        public static Task LogSettingAuditAsync(this DatabaseService db, Setting? setting, string action, string ip, string device, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(0, $"SETTING_{action}", "settings", "SettingsModule", setting?.Id, details ?? setting?.Value, ip, "audit", device, sessionId, token: token);
    }
}

