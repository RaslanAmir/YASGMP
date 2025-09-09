using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YasGMP.Diagnostics;

namespace YasGMP.Services.Database
{
    public static class DbSlowQueryRegistry
    {
        private static readonly ConcurrentQueue<(int dur, string sig, DateTime ts)> Q = new();
        private const int MaxKeep = 2000;

        public static void Record(int durationMs, string signature)
        {
            Q.Enqueue((durationMs, signature, DateTime.UtcNow));
            while (Q.Count > MaxKeep && Q.TryDequeue(out _)) { }
        }

        public static bool IsInTopN(int durationMs, int topN)
        {
            if (Q.IsEmpty) return false;
            var top = Q.OrderByDescending(x => x.dur).Take(topN).ToList();
            if (top.Count < topN) return durationMs >= (top.LastOrDefault().dur);
            return durationMs >= top.Last().dur;
        }

        public static object? TryGetSnapshot()
        {
            try
            {
                var list = Q.ToArray().OrderByDescending(x => x.dur).Take(200).ToList();
                return list.Select(x => new { duration_ms = x.dur, sql_signature = x.sig, ts_utc = x.ts }).ToList();
            }
            catch { return null; }
        }
    }
}

