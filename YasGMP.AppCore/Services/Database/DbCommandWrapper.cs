using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Diagnostics;

namespace YasGMP.Services.Database
{
    /// <summary>
    /// Executes MySQL commands with telemetry hooks, parameter preparation, and slow-query tracking.
    /// </summary>
    internal static class DbCommandWrapper
    {
        /// <summary>
        /// Executes the execute non query async operation.
        /// </summary>
        public static async Task<int> ExecuteNonQueryAsync(
            MySqlCommand cmd,
            DiagnosticContext ctx,
            ITrace trace,
            string sqlOriginal,
            IEnumerable<MySqlParameter>? parameters,
            CancellationToken token)
        {
            PrepareParameters(cmd, parameters);
            var signature = DbTelemetry.SignatureOf(sqlOriginal);
            var sw = Stopwatch.StartNew();
            Exception? error = null;
            int rows = -1;
            try
            {
                rows = await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                return rows;
            }
            catch (Exception ex)
            {
                error = ex; throw;
            }
            finally
            {
                sw.Stop();
                var dur = (int)sw.Elapsed.TotalMilliseconds;
                DbSlowQueryRegistry.Record(dur, signature);
                DbTelemetry.RecordNPlusOne(ctx.CorrId, signature, trace);
                if (DbTelemetry.ShouldSample(ctx, dur) || dur >= ctx.SlowQueryMs)
                {
                    var data = new Dictionary<string, object?>
                    {
                        ["sql_signature"] = signature,
                        ["params"] = ToParamMap(cmd.Parameters),
                        ["duration_ms"] = dur,
                        ["rows"] = rows,
                        ["server_roundtrips"] = 1,
                        ["timeout"] = cmd.CommandTimeout,
                        ["connection_id"] = (cmd.Connection as MySqlConnection)?.ServerThread
                    };
                    var lvl = dur >= ctx.SlowQueryMs ? DiagLevel.Warn : DiagLevel.Debug;
                    trace.Log(lvl, "sql", error is null ? "exec_nonquery" : "exec_nonquery_error",
                        error is null ? "OK" : error.Message, error, data);
                }
            }
        }
        /// <summary>
        /// Executes the execute scalar async operation.
        /// </summary>

        public static async Task<object?> ExecuteScalarAsync(
            MySqlCommand cmd,
            DiagnosticContext ctx,
            ITrace trace,
            string sqlOriginal,
            IEnumerable<MySqlParameter>? parameters,
            CancellationToken token)
        {
            PrepareParameters(cmd, parameters);
            var signature = DbTelemetry.SignatureOf(sqlOriginal);
            var sw = Stopwatch.StartNew();
            Exception? error = null;
            object? result = null;
            try
            {
                result = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                error = ex; throw;
            }
            finally
            {
                sw.Stop();
                var dur = (int)sw.Elapsed.TotalMilliseconds;
                DbSlowQueryRegistry.Record(dur, signature);
                DbTelemetry.RecordNPlusOne(ctx.CorrId, signature, trace);
                if (DbTelemetry.ShouldSample(ctx, dur) || dur >= ctx.SlowQueryMs)
                {
                    var data = new Dictionary<string, object?>
                    {
                        ["sql_signature"] = signature,
                        ["params"] = ToParamMap(cmd.Parameters),
                        ["duration_ms"] = dur,
                        ["rows"] = 1,
                        ["server_roundtrips"] = 1,
                        ["timeout"] = cmd.CommandTimeout,
                        ["connection_id"] = (cmd.Connection as MySqlConnection)?.ServerThread
                    };
                    var lvl = dur >= ctx.SlowQueryMs ? DiagLevel.Warn : DiagLevel.Debug;
                    trace.Log(lvl, "sql", error is null ? "exec_scalar" : "exec_scalar_error",
                        error is null ? "OK" : error.Message, error, data);
                }
            }
        }
        /// <summary>
        /// Executes the execute select async operation.
        /// </summary>

        public static async Task<DataTable> ExecuteSelectAsync(
            MySqlCommand cmd,
            DiagnosticContext ctx,
            ITrace trace,
            string sqlOriginal,
            IEnumerable<MySqlParameter>? parameters,
            CancellationToken token)
        {
            PrepareParameters(cmd, parameters);
            var signature = DbTelemetry.SignatureOf(sqlOriginal);
            var sw = Stopwatch.StartNew();
            Exception? error = null;
            var dt = new DataTable();
            int rows = -1;
            try
            {
                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token).ConfigureAwait(false);
                dt.Load(reader);
                rows = dt.Rows.Count;
                return dt;
            }
            catch (Exception ex)
            {
                error = ex; throw;
            }
            finally
            {
                sw.Stop();
                var dur = (int)sw.Elapsed.TotalMilliseconds;
                DbSlowQueryRegistry.Record(dur, signature);
                DbTelemetry.RecordNPlusOne(ctx.CorrId, signature, trace);
                if (DbTelemetry.ShouldSample(ctx, dur) || dur >= ctx.SlowQueryMs)
                {
                    var data = new Dictionary<string, object?>
                    {
                        ["sql_signature"] = signature,
                        ["params"] = ToParamMap(cmd.Parameters),
                        ["duration_ms"] = dur,
                        ["rows"] = rows,
                        ["server_roundtrips"] = 1,
                        ["timeout"] = cmd.CommandTimeout,
                        ["connection_id"] = (cmd.Connection as MySqlConnection)?.ServerThread
                    };
                    var lvl = dur >= ctx.SlowQueryMs ? DiagLevel.Warn : DiagLevel.Debug;
                    trace.Log(lvl, "sql", error is null ? "exec_select" : "exec_select_error",
                        error is null ? "OK" : error.Message, error, data);
                }
            }
        }

        private static void PrepareParameters(MySqlCommand cmd, IEnumerable<MySqlParameter>? parameters)
        {
            if (parameters == null) return;
            foreach (var p in parameters) cmd.Parameters.Add(p);
        }

        private static IDictionary<string, object?> ToParamMap(MySqlParameterCollection pars)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (MySqlParameter p in pars)
            {
                dict[p.ParameterName] = "****"; // redacted values
            }
            return dict;
        }
    }
}

