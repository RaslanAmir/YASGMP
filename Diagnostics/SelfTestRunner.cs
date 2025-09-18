using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Services;

namespace YasGMP.Diagnostics
{
    public sealed class SelfTestRunner
    {
        private readonly DatabaseService _db;
        private readonly ITrace _trace;
        private readonly DiagnosticContext _ctx;

        public SelfTestRunner(DatabaseService db, ITrace trace, DiagnosticContext ctx)
        {
            _db = db; _trace = trace; _ctx = ctx;
        }

        public async Task RunAll(CancellationToken token = default)
        {
            try
            {
                await TestDbConnect(token).ConfigureAwait(false);
                await TestUtcDrift(token).ConfigureAwait(false);
                await InventoryTriggersAndAudit(token).ConfigureAwait(false);
                await CheckRbacIntegrity(token).ConfigureAwait(false);
                await ComputeSchemaHash(token).ConfigureAwait(false);
                await ExerciseChangeControlAssignmentHarness().ConfigureAwait(false);
                _trace.Log(DiagLevel.Info, "selftest", "completed", "Diagnostics self-tests finished.");
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Error, "selftest", "failed", ex.Message, ex);
            }
        }

        private async Task TestDbConnect(CancellationToken token)
        {
            try
            {
                var val = await _db.ExecuteScalarAsync("SELECT 1", null, token).ConfigureAwait(false);
                _trace.Log(DiagLevel.Info, "selftest", "db_connect", "OK", data: new Dictionary<string, object?> { ["result"] = val });
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Error, "selftest", "db_connect_error", ex.Message, ex);
            }
        }

        private async Task TestUtcDrift(CancellationToken token)
        {
            try
            {
                var dtLocal = DateTimeOffset.Now;
                var dtUtc   = DateTimeOffset.UtcNow;
                var diffSecondsLocalUtc = (int)Math.Abs((dtLocal - dtUtc).TotalSeconds - TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds);
                var sql = "SELECT TIMESTAMPDIFF(SECOND, UTC_TIMESTAMP(), NOW())";
                var drift = Convert.ToInt32(await _db.ExecuteScalarAsync(sql, null, token).ConfigureAwait(false) ?? 0);
                _trace.Log(DiagLevel.Info, "selftest", "utc_drift", $"db_vs_utc={drift}s; local_vs_utc_offset_err={diffSecondsLocalUtc}s",
                    data: new Dictionary<string, object?> { ["db_minus_utc_seconds"] = drift, ["local_offset_err_seconds"] = diffSecondsLocalUtc });
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Warn, "selftest", "utc_drift_failed", ex.Message, ex);
            }
        }

        private async Task InventoryTriggersAndAudit(CancellationToken token)
        {
            try
            {
                var triggers = await _db.ExecuteSelectAsync(
                    "SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE()",
                    null, token).ConfigureAwait(false);
                var auditTables = await _db.ExecuteSelectAsync(
                    "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME LIKE '%audit%'",
                    null, token).ConfigureAwait(false);
                _trace.Log(DiagLevel.Info, "selftest", "audit_inventory", $"triggers={triggers.Rows.Count}; audit_tables={auditTables.Rows.Count}");
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Warn, "selftest", "audit_inventory_failed", ex.Message, ex);
            }
        }

        private async Task CheckRbacIntegrity(CancellationToken token)
        {
            try
            {
                var count = Convert.ToInt32(await _db.ExecuteScalarAsync(
                    "SELECT COUNT(*) FROM permissions WHERE code='Diagnostics.View'",
                    null, token).ConfigureAwait(false) ?? 0);
                if (count == 0)
                    _trace.Log(DiagLevel.Warn, "selftest", "rbac_missing_permission", "Missing Diagnostics.View permission");
                else
                    _trace.Log(DiagLevel.Info, "selftest", "rbac_permission_ok", "Diagnostics.View present");
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Warn, "selftest", "rbac_check_failed", ex.Message, ex);
            }
        }

        private async Task ComputeSchemaHash(CancellationToken token)
        {
            try
            {
                var tables = await _db.ExecuteSelectAsync(
                    "SELECT TABLE_NAME, ENGINE, TABLE_ROWS FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME",
                    null, token).ConfigureAwait(false);
                var triggers = await _db.ExecuteSelectAsync(
                    "SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() ORDER BY TRIGGER_NAME",
                    null, token).ConfigureAwait(false);

                var sb = new StringBuilder();
                foreach (DataRow r in tables.Rows)
                    sb.Append(r[0]).Append('|').Append(r[1]).Append('|').Append(r[2]).Append('\n');
                foreach (DataRow r in triggers.Rows)
                    sb.Append(r[0]).Append('|').Append(r[1]).Append('|').Append(r[2]).Append('|').Append(r[3]).Append('\n');

                using var sha = SHA256.Create();
                var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
                _ctx.DbSchemaHash = hash;
                _trace.Log(DiagLevel.Info, "selftest", "schema_hash", hash);
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Warn, "selftest", "schema_hash_failed", ex.Message, ex);
            }
        }

        private async Task ExerciseChangeControlAssignmentHarness()
        {
            try
            {
                var harnessResult = await ChangeControlAssignmentHarness.RunAsync().ConfigureAwait(false);
                var eventSummary = harnessResult.LoggedEvents
                    .Select(e => $"{e.EventType}:{e.OldValue ?? "∅"}->{e.NewValue ?? "∅"}")
                    .ToArray();
                var data = new Dictionary<string, object?>
                {
                    ["statusMessages"] = harnessResult.StatusMessages,
                    ["eventSummary"] = eventSummary,
                    ["executedSqlCount"] = harnessResult.ExecutedSql.Count,
                    ["loggedAudit"] = harnessResult.LoggedAudit,
                    ["hasInitialAssignmentEvent"] = harnessResult.HasInitialAssignmentEvent,
                    ["hasReassignmentEvent"] = harnessResult.HasReassignmentEvent,
                    ["missingAuditEvents"] = harnessResult.MissingAuditEvents
                };

                if (harnessResult.MissingAuditEvents.Count > 0)
                {
                    var missing = string.Join(", ", harnessResult.MissingAuditEvents);
                    _trace.Log(
                        DiagLevel.Warn,
                        "selftest",
                        "cc_assign_harness_missing_audit",
                        $"Change control assignment harness executed with missing audit events: {missing}.",
                        data: data);
                }
                else
                {
                    _trace.Log(
                        DiagLevel.Info,
                        "selftest",
                        "cc_assign_harness",
                        "Change control assignment harness executed.",
                        data: data);
                }
            }
            catch (Exception ex)
            {
                _trace.Log(DiagLevel.Warn, "selftest", "cc_assign_harness_failed", ex.Message, ex);
            }
        }
    }
}

