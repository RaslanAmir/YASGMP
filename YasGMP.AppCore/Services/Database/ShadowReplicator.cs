using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using YasGMP.Diagnostics;

namespace YasGMP.Services.Database
{
    /// <summary>
    /// Optionally replays write commands against a shadow database to validate changes without impact.
    /// </summary>
    internal sealed class ShadowReplicator
    {
        private readonly string _primaryConn;
        private readonly string? _shadowConn;
        private readonly DiagnosticContext _ctx;
        private readonly ITrace _trace;
        /// <summary>
        /// Initializes a new instance of the ShadowReplicator class.
        /// </summary>

        public ShadowReplicator(string primaryConnectionString, string? shadowConnectionString, DiagnosticContext ctx, ITrace trace)
        {
            _primaryConn = primaryConnectionString;
            _shadowConn = shadowConnectionString;
            _ctx = ctx; _trace = trace;
        }
        /// <summary>
        /// Executes the enabled operation.
        /// </summary>

        public bool Enabled => !string.IsNullOrWhiteSpace(_shadowConn);
        /// <summary>
        /// Executes the try shadow execute non query async operation.
        /// </summary>

        public async Task<int?> TryShadowExecuteNonQueryAsync(string sql, IEnumerable<MySqlParameter>? parameters, int primaryRows, CancellationToken token)
        {
#if DEBUG
            if (!Enabled) return null;
            try
            {
                await using var conn = new MySqlConnection(_shadowConn);
                await conn.OpenAsync(token).ConfigureAwait(false);
                await using var cmd = new MySqlCommand(sql, conn);
                if (parameters != null)
                    foreach (var p in parameters)
                        cmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));
                var rows = await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);

                // Read-after-write verification by affected rows comparison (fast and generic)
                var ok = rows == primaryRows;
                _trace.Log(ok ? DiagLevel.Debug : DiagLevel.Warn, "shadow", "shadow_write",
                    ok ? "Rows match" : $"Rows mismatch primary={primaryRows}, shadow={rows}",
                    data: new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["duration_ms"] = 0,
                        ["primary_rows"] = primaryRows,
                        ["shadow_rows"] = rows
                    });

                // Optional deep verify for known tables (≤250 ms budget)
                await TryVerifyReadAfterWriteAsync(sql, parameters, token).ConfigureAwait(false);
                return rows;
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Warn, "shadow", "shadow_error", ex.Message, ex);
                return null;
            }
#else
            await Task.CompletedTask; return null;
#endif
        }

        private async Task TryVerifyReadAfterWriteAsync(string sql, IEnumerable<MySqlParameter>? parameters, CancellationToken token)
        {
            try
            {
                if (!ShouldVerify(out var table, out var pk)) return;
                if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(pk)) return;

                var verb = GetSqlVerb(sql);
                var idParamName = FindPkParamName(sql, pk);
                var idVal = FindParamValue(parameters, idParamName) ?? FindParamValue(parameters, "@id") ?? FindParamValue(parameters, "@" + pk);
                if (verb == "insert" && idVal is null)
                {
                    // Cannot reliably check insert without id; skip
                    return;
                }

                // allow small delay for commit/propagation
                await Task.Delay(150, token).ConfigureAwait(false);

                var primaryRow = await ReadRowAsync(_primaryConn, table!, pk!, idVal, token).ConfigureAwait(false);
                var shadowRow  = await ReadRowAsync(_shadowConn!, table!, pk!, idVal, token).ConfigureAwait(false);

                if (verb == "delete")
                {
                    bool ok = primaryRow == null && shadowRow == null;
                    _trace.Log(ok ? DiagLevel.Debug : DiagLevel.Warn, "shadow", ok ? "raw_verify_ok" : "raw_verify_diff",
                        ok ? "Delete visible in both" : "Delete mismatch",
                        data: new System.Collections.Generic.Dictionary<string, object?>
                        {
                            ["table"] = table,
                            ["pk"] = pk,
                            ["id"] = idVal,
                            ["primary_exists"] = primaryRow != null,
                            ["shadow_exists"] = shadowRow != null
                        });
                    return;
                }

                if (primaryRow == null || shadowRow == null)
                {
                    _trace.Log(DiagLevel.Warn, "shadow", "raw_verify_diff", "One side missing row",
                        data: new System.Collections.Generic.Dictionary<string, object?>
                        {
                            ["table"] = table,
                            ["pk"] = pk,
                            ["id"] = idVal,
                            ["primary_exists"] = primaryRow != null,
                            ["shadow_exists"] = shadowRow != null
                        });
                    return;
                }

                var diffs = ComputeDiff(primaryRow, shadowRow);
                bool okUpdate = diffs.Count == 0;
                _trace.Log(okUpdate ? DiagLevel.Debug : DiagLevel.Warn, "shadow", okUpdate ? "raw_verify_ok" : "raw_verify_diff",
                    okUpdate ? "Rows match" : "Rows differ",
                    data: new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["table"] = table,
                        ["pk"] = pk,
                        ["id"] = idVal,
                        ["diffs"] = diffs
                    });
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Debug, "shadow", "raw_verify_skip", ex.Message, ex);
            }
        }

        private bool ShouldVerify(out string? table, out string? pk)
        {
            table = null; pk = null;
            try
            {
                // Expect keys under: Diagnostics:DbShadow:VerifyTables:<table>=<pk>
                var prefix = "Diagnostics:DbShadow:VerifyTables:";
                foreach (var kv in _ctx.Config.AsEnumerable())
                {
                    if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
                    {
                        table = kv.Key.Substring(prefix.Length);
                        pk = kv.Value;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static string GetSqlVerb(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;
            var s = sql.TrimStart();
            var sp = s.IndexOf(' ');
            return (sp > 0 ? s.Substring(0, sp) : s).Trim().ToLowerInvariant();
        }

        private static string? FindPkParamName(string sql, string pk)
        {
            if (string.IsNullOrWhiteSpace(sql)) return null;
            var s = sql.ToLowerInvariant();
            var idx = s.IndexOf(pk.ToLowerInvariant() + " = @");
            if (idx >= 0)
            {
                var start = idx + pk.Length + 4;
                int end = start;
                while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_')) end++;
                return "@" + sql.Substring(start, end - start);
            }
            return null;
        }

        private static object? FindParamValue(IEnumerable<MySqlParameter>? pars, string? name)
        {
            if (pars == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var p in pars)
            {
                if (string.Equals(p.ParameterName, name, StringComparison.OrdinalIgnoreCase))
                    return p.Value;
            }
            return null;
        }

        private static async Task<Dictionary<string, object?>?> ReadRowAsync(string connStr, string table, string pk, object? id, CancellationToken token)
        {
            try
            {
                await using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync(token).ConfigureAwait(false);
                var sql = $"SELECT * FROM `{table}` WHERE `{pk}`=@id LIMIT 1";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id ?? DBNull.Value));
                await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (!reader.HasRows) return null;
                await reader.ReadAsync(token).ConfigureAwait(false);
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    dict[name] = val?.ToString();
                }
                return dict;
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, object?> ComputeDiff(Dictionary<string, object?> a, Dictionary<string, object?> b)
        {
            var diffs = new Dictionary<string, object?>();
            foreach (var k in a.Keys)
            {
                b.TryGetValue(k, out var bv);
                var av = a[k];
                if (!object.Equals(av, bv))
                    diffs[k] = new { primary = av, shadow = bv };
            }
            foreach (var k in b.Keys)
            {
                if (!a.ContainsKey(k))
                    diffs[k] = new { primary = (object?)null, shadow = b[k] };
            }
            return diffs;
        }
    }
}
