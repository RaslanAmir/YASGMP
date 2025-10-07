using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace YasGMP.Diagnostics
{
    internal sealed class EfSqlInterceptor : DbCommandInterceptor
    {
        private readonly DiagnosticContext _ctx;
        private readonly ITrace _trace;
        /// <summary>
        /// Initializes a new instance of the EfSqlInterceptor class.
        /// </summary>

        public EfSqlInterceptor(DiagnosticContext ctx, ITrace trace)
        {
            _ctx = ctx; _trace = trace;
        }
        /// <summary>
        /// Executes the reader executing async operation.
        /// </summary>

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            return await WrapAsync(command, eventData, () => base.ReaderExecutingAsync(command, eventData, result, cancellationToken));
        }
        /// <summary>
        /// Executes the non query executing async operation.
        /// </summary>

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            return await WrapAsync(command, eventData, () => base.NonQueryExecutingAsync(command, eventData, result, cancellationToken));
        }
        /// <summary>
        /// Executes the scalar executing async operation.
        /// </summary>

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            return await WrapAsync(command, eventData, () => base.ScalarExecutingAsync(command, eventData, result, cancellationToken));
        }

        private async ValueTask<T> WrapAsync<T>(DbCommand command, CommandEventData data, Func<ValueTask<T>> next)
        {
            var signature = Services.Database.DbTelemetry.SignatureOf(command.CommandText ?? string.Empty);
            var sw = Stopwatch.StartNew();
            Exception? error = null;
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex; throw;
            }
            finally
            {
                sw.Stop();
                var dur = (int)sw.Elapsed.TotalMilliseconds;
                Services.Database.DbSlowQueryRegistry.Record(dur, signature);
                Services.Database.DbTelemetry.RecordNPlusOne(_ctx.CorrId, signature, _trace);
                if (Services.Database.DbTelemetry.ShouldSample(_ctx, dur) || dur >= _ctx.SlowQueryMs)
                {
                    var dataMap = new Dictionary<string, object?>
                    {
                        ["sql_signature"] = signature,
                        ["params"] = BuildParamMap(command.Parameters),
                        ["duration_ms"] = dur,
                        ["timeout"] = command.CommandTimeout,
                    };
                    var lvl = dur >= _ctx.SlowQueryMs ? DiagLevel.Warn : DiagLevel.Debug;
                    _trace.Log(lvl, "ef", error is null ? "exec" : "exec_error", data.CommandSource.ToString(), error, dataMap);
                }
            }
        }

        private static IDictionary<string, object?> BuildParamMap(DbParameterCollection pars)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DbParameter p in pars)
                dict[p.ParameterName] = "****";
            return dict;
        }
    }
}

