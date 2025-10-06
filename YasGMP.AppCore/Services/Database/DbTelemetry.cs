using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YasGMP.Diagnostics;

namespace YasGMP.Services.Database
{
    /// <summary>
    /// Helper telemetry functions for database calls including sampling decisions and N+1 detection.
    /// </summary>
    internal static class DbTelemetry
    {
        private static readonly ConcurrentDictionary<string, int> NPlusOneCounter = new();

        public static bool ShouldSample(DiagnosticContext ctx, int durationMs)
        {
#if DEBUG
            return true; // 100%
#else
            // Always sample slow top-N and random 1%
            if (DbSlowQueryRegistry.IsInTopN(durationMs, ctx.TopNRelease)) return true;
            var rnd = Random.Shared.NextDouble() * 100.0;
            return rnd < Math.Max(0.0, ctx.RandomPercentRelease);
#endif
        }

        public static string SignatureOf(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;
            // remove parameters and collapse whitespace for signature
            var s = sql.Replace("\r", " ").Replace("\n", " ");
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s.Trim().ToLowerInvariant();
        }

        public static void RecordNPlusOne(string corrId, string signature, ITrace trace)
        {
            var key = corrId + "|" + signature;
            var count = NPlusOneCounter.AddOrUpdate(key, 1, (_, v) => v + 1);
            if (count == 3)
            {
                trace.Log(DiagLevel.Warn, "sql", "n_plus_one_suspect", "Same query repeated 3+ times", data: new Dictionary<string, object?>
                {
                    ["sql_signature"] = signature
                });
            }
        }
    }
}


