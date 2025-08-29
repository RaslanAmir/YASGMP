// ==============================================================================
//  YasGMP – DatabaseService.cs   (Part 1 of 6)
// ------------------------------------------------------------------------------
//  *** DO NOT EDIT SPLIT HEADERS – MERGE THE 6 CHUNKS SEQUENTIALLY ***
//
//  Ultra-robust, GMP / Annex 11 / 21 CFR Part 11–compliant data-access service
//  aligned with the 2025 FINAL SQL schema you provided. Includes:
//  • Resilient MySQL access with retry
//  • System event/audit logging to system_event_log
//  • Full CRUD for External Contractors & Contractor Interventions (no stubs)
//  • CAPA audit via capa_status_history + capa_action_log (no capa_audit_log)
//  • Defensive DataRow parsing (schema tolerant)
//  • XML documentation for IntelliSense everywhere
//
//  © 2025 YasGMP.  All rights reserved.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Models.Enums;
using System.Linq;
using System.Reflection;

namespace YasGMP.Services

// ======== Compile-time POCOs (minimal) – move to YasGMP.Models later =========
{
public class ApiAuditLog
{
    public int Id { get; set; }
    public int? ApiKeyId { get; set; }
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestDetails { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiUsageStat
{
    public string? Endpoint { get; set; }
    public int Count { get; set; }
    public double AverageMs { get; set; }
}

public class DeleteLog
{
    public int Id { get; set; }
    public int DeletedBy { get; set; }
    public string? TableName { get; set; }
    public int RecordId { get; set; }
    public string? DeleteType { get; set; } // "soft" | "hard"
    public string? Reason { get; set; }
    public bool Recoverable { get; set; }
    public string? BackupFile { get; set; }
    public string? SourceIp { get; set; }
    public string? Note { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SopDocumentLog
{
    public int Id { get; set; }
    public int SopDocumentId { get; set; }
    public int? PerformedBy { get; set; }
    public string? Action { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Note { get; set; }
}


    public class ApiKey
    {
        public int Id { get; set; }
        public string? KeyValue { get; set; }
        public string? Description { get; set; }
        public int? OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    public class ApiUsage
    {
        public int Id { get; set; }
        public int? ApiKeyId { get; set; }
        public int? UserId { get; set; }
        public DateTime CallTime { get; set; }
        public string? Endpoint { get; set; }
        public string? Method { get; set; }
        public string? Params { get; set; }
        public int ResponseCode { get; set; }
        public int DurationMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SourceIp { get; set; }
    }

    public class ExportPrintEntry
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public DateTime ExportTime { get; set; }
        public string? Format { get; set; }
        public string? TableName { get; set; }
        public string? FilterUsed { get; set; }
        public string? FilePath { get; set; }
        public string? SourceIp { get; set; }
        public string? Note { get; set; }
    }

    public class ReportSchedule
    {
        public int Id { get; set; }
        public string? ReportName { get; set; }
        public string? ScheduleType { get; set; }
        public string? Format { get; set; }
        public string? Recipients { get; set; }
        public DateTime? LastGenerated { get; set; }
        public DateTime? NextDue { get; set; }
        public string? Status { get; set; }
        public int? GeneratedBy { get; set; }
    }

    public class SystemParameter
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Note { get; set; }
    }

    public class SchemaMigrationEntry
    {
        public int Id { get; set; }
        public DateTime MigrationTime { get; set; }
        public int? MigratedBy { get; set; }
        public string? SchemaVersion { get; set; }
        public string? MigrationScript { get; set; }
        public string? Description { get; set; }
        public string? SourceIp { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Stubs to satisfy other references
    public class IotSensorData
    {
        public int Id { get; set; }
        public string? DeviceId { get; set; }
        public int? ComponentId { get; set; }
        public string? DataType { get; set; }
        public decimal Value { get; set; }
        public string? Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
        public bool AnomalyDetected { get; set; }
        public string? Note { get; set; }
    }

    public class IotAnomaly
    {
        public int Id { get; set; }
        public int? SensorDataId { get; set; }
        public DateTime DetectedAt { get; set; }
        public string? DetectedBy { get; set; }
        public string? Description { get; set; }
        public string? Severity { get; set; }
        public bool Resolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public class Dashboard
    {
        public int Id { get; set; }
        public string? DashboardName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? ConfigJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class SystemEvent
    {
        public int Id { get; set; }
        public DateTime EventTime { get; set; }
        public int? UserId { get; set; }
        public string? EventType { get; set; }
        public string? TableName { get; set; }
        public string? RelatedModule { get; set; }
        public int? RecordId { get; set; }
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Description { get; set; }
        public string? SourceIp { get; set; }
        public string? DeviceInfo { get; set; }
        public string? SessionId { get; set; }
        public string? Severity { get; set; }
        public bool Processed { get; set; }
    }







    public sealed class DatabaseService : IDisposable
    {
#region === 00 · CONSTANTS · FIELDS · CTOR ===================================

        private readonly string _connectionString;
        private const int MaxRetryCount = 3;

        /// <summary>
        /// Creates a <see cref="DatabaseService"/> bound to a MySQL / MariaDB
        /// connection string.
        /// </summary>
        /// <param name="connectionString">ADO.NET connection string.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="connectionString"/> is null.</exception>
        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        #endregion

#region === 00b · MODEL COMPAT HELPERS (safe reflection + conversions) ===

/// <summary>
/// Resolves a public instance property by name on <paramref name="type"/> with the following rules:
/// 1) Exact-case match first (no ambiguity).
/// 2) If not found, collect case-insensitive candidates.
/// 3) If multiple candidates, prefer ones **without** <see cref="System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute"/>.
/// 4) If still multiple, prefer a candidate whose <see cref="System.ComponentModel.DataAnnotations.Schema.ColumnAttribute"/>
///    explicitly names <paramref name="propName"/> (case-insensitive).
/// 5) As a deterministic fallback, return the lexicographically-first candidate.
/// This avoids <see cref="AmbiguousMatchException"/> when both <c>Username</c> and alias <c>UserName</c> exist.
/// </summary>
/// <param name="type">The declaring type.</param>
/// <param name="propName">Property name requested by callers (usually from DB column names or model code).</param>
private static PropertyInfo? ResolveProperty(Type type, string propName)
{
    if (type == null || string.IsNullOrWhiteSpace(propName)) return null;

    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
    var props = type.GetProperties(flags);

    // 1) Exact-case first
    PropertyInfo? exact = null;
    var ciCandidates = new List<PropertyInfo>();
    for (int i = 0; i < props.Length; i++)
    {
        var p = props[i];
        if (string.Equals(p.Name, propName, StringComparison.Ordinal))
        {
            exact = p;
            break;
        }
        if (string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
        {
            ciCandidates.Add(p);
        }
    }
    if (exact != null) return exact;

    // 2) No case-insensitive candidates
    if (ciCandidates.Count == 0) return null;
    if (ciCandidates.Count == 1) return ciCandidates[0];

    // 3) Prefer NOT NotMapped
    var filtered = new List<PropertyInfo>(ciCandidates.Count);
    for (int i = 0; i < ciCandidates.Count; i++)
    {
        if (ciCandidates[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() == null)
            filtered.Add(ciCandidates[i]);
    }
    if (filtered.Count == 1) return filtered[0];
    if (filtered.Count > 1) ciCandidates = filtered;

    // 4) Prefer [Column(Name=propName)]
    for (int i = 0; i < ciCandidates.Count; i++)
    {
        var col = ciCandidates[i].GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
        if (col != null && !string.IsNullOrWhiteSpace(col.Name) &&
            string.Equals(col.Name, propName, StringComparison.OrdinalIgnoreCase))
            return ciCandidates[i];
    }

    // 5) Deterministic fallback (lexicographic by Name)
    ciCandidates.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
    return ciCandidates[0];
}

/// <summary>
/// Safely gets the value of a property on <paramref name="obj"/> following <see cref="ResolveProperty"/> rules.
/// Returns <c>null</c> when the object or property is missing.
/// </summary>
private static object? TryGet(object? obj, string propName)
{
    if (obj is null) return null;
    var p = ResolveProperty(obj.GetType(), propName);
    return p?.GetValue(obj);
}

/// <summary>
/// Strongly-typed getter for value types (nullable). Includes robust conversions for
/// <see cref="DateTime"/> and numeric/boolean coercions.
/// </summary>
private static T? TryGet<T>(object? obj, string propName) where T : struct
{
    var v = TryGet(obj, propName);
    if (v is null || v is DBNull) return null;

    try
    {
        if (v is T t) return t;
        var destType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        // DateTime from string/object
        if (destType == typeof(DateTime))
        {
            if (v is DateTime dt) return (T)(object)dt;
            if (DateTime.TryParse(v.ToString(), out var parsed))
                return (T)(object)parsed;
        }

        // Boolean from "1"/"0"/"true"/"false"/numbers
        if (destType == typeof(bool))
        {
            if (v is bool b) return (T)(object)b;
            var s = v.ToString()?.Trim();
            if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase))
                return (T)(object)true;
            if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "no", StringComparison.OrdinalIgnoreCase))
                return (T)(object)false;
            if (double.TryParse(s, out var d))
                return (T)(object)(Math.Abs(d) > double.Epsilon);
        }

        return (T?)Convert.ChangeType(v, destType);
    }
    catch
    {
        return null;
    }
}

/// <summary>
/// String getter that returns <c>null</c> for <c>null</c>/<see cref="DBNull"/>.
/// </summary>
private static string? TryGetString(object? obj, string propName)
{
    var v = TryGet(obj, propName);
    return (v is null || v is DBNull) ? null : v.ToString();
}

/// <summary>
/// Sets the property <paramref name="propName"/> on <paramref name="target"/> if it exists,
/// using the same disambiguation as <see cref="ResolveProperty"/> and robust conversions for
/// <see cref="DateTime"/> and <see cref="bool"/>. Nulls are assigned to reference types or
/// nullable value types; mismatches are safely ignored.
/// </summary>
private static void SetIfExists(object? target, string propName, object? value)
{
    if (target is null) return;

    var p = ResolveProperty(target.GetType(), propName);
    if (p is null || !p.CanWrite) return;

    try
    {
        if (value is null || value is DBNull)
        {
            if (!p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null)
                p.SetValue(target, null);
            return;
        }

        var destType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

        // DateTime assignment
        if (destType == typeof(DateTime))
        {
            if (value is DateTime dt)
            {
                p.SetValue(target, dt);
                return;
            }
            if (DateTime.TryParse(value.ToString(), out var parsedDt))
            {
                p.SetValue(target, parsedDt);
                return;
            }
        }

        // Boolean assignment
        if (destType == typeof(bool))
        {
            if (value is bool b)
            {
                p.SetValue(target, b);
                return;
            }
            var s = value.ToString()?.Trim();
            if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase))
            {
                p.SetValue(target, true);
                return;
            }
            if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "no", StringComparison.OrdinalIgnoreCase))
            {
                p.SetValue(target, false);
                return;
            }
        }

        var converted = value is IConvertible ? Convert.ChangeType(value, destType) : value;
        p.SetValue(target, converted);
    }
    catch
    {
        // swallow conversion issues by design
    }
}

#endregion

#region === 01 · GENERIC DB HELPERS & RETRY ==================================

// === Local structured file trace ===
// Writes JSONL rows under %LocalAppData%/YasGMP/logs/YYYY-MM-DD_<pid>.log
// No external deps; safe in service layer (non-MAUI).
private static async Task __WriteTraceAsync(
    string phase,           // begin | end | error | retry
    string api,             // ExecuteReader | ExecuteSelect | ExecuteNonQuery | ExecuteScalar | ExecuteTx
    string sql,
    MySqlParameter[]? pars,
    long? elapsedMs = null,
    int? affected = null,
    string? error = null)
{
    try
    {
        var root = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YasGMP", "logs");

        System.IO.Directory.CreateDirectory(root);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var file = System.IO.Path.Combine(root, $"{date}_{Environment.ProcessId}.log");

        // Light param projection (no secrets inference here)
        object? P(MySqlParameter p) => new { name = p.ParameterName, type = p.MySqlDbType.ToString(), val = p.Value };

        var payload = new
        {
            ts = DateTimeOffset.UtcNow.ToString("o"),
            level = phase == "error" ? "error" : (phase == "retry" ? "warn" : "trace"),
            src = "DatabaseService",
            api,
            phase,
            elapsedMs,
            affected,
            sql,
            pars = pars is null ? null : pars.Select(P).ToArray(),
            error
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        await System.IO.File.AppendAllTextAsync(file, json + Environment.NewLine).ConfigureAwait(false);
    }
    catch
    {
        // Never throw from the tracer.
    }
}

/// <summary>
/// Executes a SQL command and returns a <see cref="MySqlDataReader"/>.
/// Connection is closed automatically when the reader is disposed (CloseConnection).
/// </summary>
public async Task<MySqlDataReader> ExecuteReaderAsync(
    string sql,
    MySqlParameter[]? pars = null,
    CancellationToken token = default)
{
    await __WriteTraceAsync("begin", "ExecuteReader", sql, pars).ConfigureAwait(false);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var result = await ExecuteWithRetryAsync(async () =>
        {
            var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            try
            {
                using var cmd = new MySqlCommand(sql, conn);
                if (pars is not null && pars.Length > 0) cmd.Parameters.AddRange(pars);

                // Close the connection when the reader is disposed by the caller.
                return (MySqlDataReader)await cmd
                    .ExecuteReaderAsync(CommandBehavior.CloseConnection, token)
                    .ConfigureAwait(false);
            }
            catch
            {
                await conn.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);

        sw.Stop();
        await __WriteTraceAsync("end", "ExecuteReader", sql, pars, sw.ElapsedMilliseconds, affected: null).ConfigureAwait(false);
        return result;
    }
    catch (Exception ex)
    {
        sw.Stop();
        await __WriteTraceAsync("error", "ExecuteReader", sql, pars, sw.ElapsedMilliseconds, error: ex.Message).ConfigureAwait(false);

        // Canonical audit (Region 07)
        await LogSystemEventAsync(
            userId: null,
            eventType: "DbError",
            tableName: "-",
            module: "DatabaseService",
            recordId: null,
            description: ex.Message,
            ip: "system",
            severity: "error",
            deviceInfo: "server",
            sessionId: ""
        ).ConfigureAwait(false);

        throw;
    }
}

/// <summary>
/// Executes a <c>SELECT</c> and returns a filled <see cref="DataTable"/>.
/// </summary>
public async Task<DataTable> ExecuteSelectAsync(
    string sql,
    MySqlParameter[]? pars = null,
    CancellationToken token = default)
{
    await __WriteTraceAsync("begin", "ExecuteSelect", sql, pars).ConfigureAwait(false);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var dt = await ExecuteWithRetryAsync(async () =>
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            using var cmd = new MySqlCommand(sql, conn);
            if (pars is not null && pars.Length > 0) cmd.Parameters.AddRange(pars);

            using var da = new MySqlDataAdapter(cmd);
            var table = new DataTable();
            da.Fill(table);
            return table;
        }).ConfigureAwait(false);

        sw.Stop();
        await __WriteTraceAsync("end", "ExecuteSelect", sql, pars, sw.ElapsedMilliseconds, affected: dt.Rows.Count).ConfigureAwait(false);
        return dt;
    }
    catch (Exception ex)
    {
        sw.Stop();
        await __WriteTraceAsync("error", "ExecuteSelect", sql, pars, sw.ElapsedMilliseconds, error: ex.Message).ConfigureAwait(false);

        await LogSystemEventAsync(null, "DbError", "-", "DatabaseService", null, ex.Message, "system", "error", "server", "").ConfigureAwait(false);
        throw;
    }
}

/// <summary>
/// Executes a non-query (<c>INSERT</c>/<c>UPDATE</c>/<c>DELETE</c>) and returns the number of affected rows.
/// </summary>
public async Task<int> ExecuteNonQueryAsync(
    string sql,
    MySqlParameter[]? pars = null,
    CancellationToken token = default)
{
    await __WriteTraceAsync("begin", "ExecuteNonQuery", sql, pars).ConfigureAwait(false);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var affected = await ExecuteWithRetryAsync(async () =>
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            using var cmd = new MySqlCommand(sql, conn);
            if (pars is not null && pars.Length > 0) cmd.Parameters.AddRange(pars);

            return await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }).ConfigureAwait(false);

        sw.Stop();
        await __WriteTraceAsync("end", "ExecuteNonQuery", sql, pars, sw.ElapsedMilliseconds, affected: affected).ConfigureAwait(false);
        return affected;
    }
    catch (Exception ex)
    {
        sw.Stop();
        await __WriteTraceAsync("error", "ExecuteNonQuery", sql, pars, sw.ElapsedMilliseconds, error: ex.Message).ConfigureAwait(false);

        await LogSystemEventAsync(null, "DbError", "-", "DatabaseService", null, ex.Message, "system", "error", "server", "").ConfigureAwait(false);
        throw;
    }
}

/// <summary>
/// Executes a scalar command and returns the first column of the first row (or <see langword="null"/>).
/// </summary>
public async Task<object?> ExecuteScalarAsync(
    string sql,
    MySqlParameter[]? pars = null,
    CancellationToken token = default)
{
    await __WriteTraceAsync("begin", "ExecuteScalar", sql, pars).ConfigureAwait(false);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var value = await ExecuteWithRetryAsync(async () =>
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(token).ConfigureAwait(false);

            using var cmd = new MySqlCommand(sql, conn);
            if (pars is not null && pars.Length > 0) cmd.Parameters.AddRange(pars);

            return await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
        }).ConfigureAwait(false);

        sw.Stop();
        await __WriteTraceAsync("end", "ExecuteScalar", sql, pars, sw.ElapsedMilliseconds, affected: (value is null ? 0 : 1)).ConfigureAwait(false);
        return value;
    }
    catch (Exception ex)
    {
        sw.Stop();
        await __WriteTraceAsync("error", "ExecuteScalar", sql, pars, sw.ElapsedMilliseconds, error: ex.Message).ConfigureAwait(false);

        await LogSystemEventAsync(null, "DbError", "-", "DatabaseService", null, ex.Message, "system", "error", "server", "").ConfigureAwait(false);
        throw;
    }
}

/// <summary>
/// Executes multiple SQL statements inside a single transaction. Rolls back on any failure.
/// </summary>
public async Task ExecuteTransactionAsync(
    IEnumerable<(string Sql, MySqlParameter[]? Pars)> commands,
    CancellationToken token = default)
{
    await __WriteTraceAsync("begin", "ExecuteTx", "(batch)", null).ConfigureAwait(false);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    await ExecuteWithRetryAsync(async () =>
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(token).ConfigureAwait(false);

        await using var tx = await conn.BeginTransactionAsync(token).ConfigureAwait(false);
        try
        {
            int affectedTotal = 0;

            foreach (var (sql, pars) in commands)
            {
                using var cmd = new MySqlCommand(sql, conn, tx as MySqlTransaction);
                if (pars is not null && pars.Length > 0) cmd.Parameters.AddRange(pars);
                var a = await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                affectedTotal += a;

                // per-statement trace
                await __WriteTraceAsync("end", "ExecuteTx.Statement", sql, pars, null, a).ConfigureAwait(false);
            }

            await tx.CommitAsync(token).ConfigureAwait(false);
            sw.Stop();
            await __WriteTraceAsync("end", "ExecuteTx", "(batch)", null, sw.ElapsedMilliseconds, affectedTotal).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(token).ConfigureAwait(false);
            sw.Stop();

            await __WriteTraceAsync("error", "ExecuteTx", "(batch)", null, sw.ElapsedMilliseconds, error: ex.Message).ConfigureAwait(false);

            // single canonical logger (Region 07)
            await LogSystemEventAsync(
                userId: null,
                eventType: "TransactionFailed",
                tableName: "-",
                module: "DatabaseService",
                recordId: null,
                description: $"TX rollback: {ex.Message}",
                ip: "system",
                severity: "critical",
                deviceInfo: "server",
                sessionId: ""
            ).ConfigureAwait(false);

            throw;
        }

        // dummy value for ExecuteWithRetryAsync signature
        return 0;
    }).ConfigureAwait(false);
}

/// <summary>
/// Convenience overload for transactional work that needs to run arbitrary code
/// against an open <see cref="MySqlConnection"/> and <see cref="MySqlTransaction"/>.
/// </summary>
public async Task ExecuteTransactionAsync(
    Func<MySqlConnection, MySqlTransaction, Task> work,
    CancellationToken token = default)
{
    if (work is null) throw new ArgumentNullException(nameof(work));

    await __WriteTraceAsync("begin", "ExecuteTx.Delegate", "(work)", null).ConfigureAwait(false);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    await ExecuteWithRetryAsync(async () =>
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(token).ConfigureAwait(false);

        await using var tx = await conn.BeginTransactionAsync(token).ConfigureAwait(false);
        try
        {
            await work(conn, tx as MySqlTransaction).ConfigureAwait(false);
            await tx.CommitAsync(token).ConfigureAwait(false);

            sw.Stop();
            await __WriteTraceAsync("end", "ExecuteTx.Delegate", "(work)", null, sw.ElapsedMilliseconds, affected: null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(token).ConfigureAwait(false);
            sw.Stop();

            await __WriteTraceAsync("error", "ExecuteTx.Delegate", "(work)", null, sw.ElapsedMilliseconds, error: ex.Message).ConfigureAwait(false);

            // single canonical logger (Region 07)
            await LogSystemEventAsync(
                userId: null,
                eventType: "TransactionFailed",
                tableName: "-",
                module: "DatabaseService",
                recordId: null,
                description: $"TX rollback: {ex.Message}",
                ip: "system",
                severity: "critical",
                deviceInfo: "server",
                sessionId: ""
            ).ConfigureAwait(false);

            throw;
        }

        return 0;
    }).ConfigureAwait(false);
}

/// <summary>
/// Retry wrapper for transient <see cref="MySqlException"/> failures.
/// Logs each failure and backs off linearly (max 3 attempts).
/// </summary>
private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    if (operation is null) throw new ArgumentNullException(nameof(operation));

    var attempt = 0;
    while (true)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (MySqlException ex)
        {
            attempt++;

            await __WriteTraceAsync("retry", "ExecuteWithRetry", "(n/a)", null, null, null,
                error: $"Attempt {attempt}: {ex.Message} (Code {ex.ErrorCode})").ConfigureAwait(false);

            // single canonical logger (Region 07)
            await LogSystemEventAsync(
                userId: null,
                eventType: "DbError",
                tableName: "-",
                module: "DatabaseService",
                recordId: null,
                description: ex.Message,
                ip: "system",
                severity: "warning",
                deviceInfo: "server",
                sessionId: ""
            ).ConfigureAwait(false);

            if (attempt >= 3) throw;
            await Task.Delay(200 * attempt, CancellationToken.None).ConfigureAwait(false);
        }
    }
}

#endregion

#region === 02 · ASSET MODULE =================================================

public async Task<List<Asset>> GetAllAssetsFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM assets", null, token).ConfigureAwait(false);
    var list = new List<Asset>();
    foreach (DataRow row in dt.Rows) list.Add(ParseAsset(row));
    return list;
}

public async Task<int> AddAssetAsync(
    Asset asset,
    string signatureHash,
    string ip,
    string deviceInfo,
    string sessionId,
    int actorUserId = 1,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO assets
(name, code, description, location, status, manufacturer, model, purchase_date,
 warranty_expiry, value, notes, last_modified, last_modified_by_id)
VALUES (@name, @code, @desc, @loc, @status, @manu, @model, @pdate, @wexp, @value,
        @notes, NOW(), @modby);";

    var pars = new[]
    {
        new MySqlParameter("@name",   TryGetString(asset,"Name")           ?? string.Empty),
        new MySqlParameter("@code",   TryGetString(asset,"Code")           ?? string.Empty),
        new MySqlParameter("@desc",   TryGetString(asset,"Description")    ?? string.Empty),
        new MySqlParameter("@loc",    TryGetString(asset,"Location")       ?? string.Empty),
        new MySqlParameter("@status", TryGetString(asset,"Status")         ?? string.Empty),
        new MySqlParameter("@manu",   TryGetString(asset,"Manufacturer")   ?? string.Empty),
        new MySqlParameter("@model",  TryGetString(asset,"Model")          ?? string.Empty),
        new MySqlParameter("@pdate",  (object?)(object?)(object?)TryGet<DateTime>(asset,"PurchaseDate") ?? DBNull.Value),
        new MySqlParameter("@wexp",   (object?)(object?)(object?)TryGet<DateTime>(asset,"WarrantyExpiry") ?? DBNull.Value),
        new MySqlParameter("@value",  (object?)(object?)(object?)TryGet<decimal>(asset,"Value") ?? DBNull.Value),
        new MySqlParameter("@notes",  TryGetString(asset,"Notes")          ?? string.Empty),
        new MySqlParameter("@modby",  actorUserId),
    };

    await ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
    int newId = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // unified legacy shim so existing VMs compile
    await LogAssetAuditAsync(newId, "CREATE", actorUserId, ip, deviceInfo, sessionId, signatureHash).ConfigureAwait(false);
    return newId;
}

public async Task UpdateAssetAsync(
    Asset asset,
    string signatureHash,
    string ip,
    string deviceInfo,
    string sessionId,
    int actorUserId = 1,
    CancellationToken token = default)
{
    const string sql = @"
UPDATE assets SET
    name                = @name,
    code                = @code,
    description         = @desc,
    location            = @loc,
    status              = @status,
    manufacturer        = @manu,
    model               = @model,
    purchase_date       = @pdate,
    warranty_expiry     = @wexp,
    value               = @value,
    notes               = @notes,
    last_modified       = NOW(),
    last_modified_by_id = @modby
WHERE id = @id;";

    var pars = new[]
    {
        new MySqlParameter("@name",   TryGetString(asset,"Name")           ?? string.Empty),
        new MySqlParameter("@code",   TryGetString(asset,"Code")           ?? string.Empty),
        new MySqlParameter("@desc",   TryGetString(asset,"Description")    ?? string.Empty),
        new MySqlParameter("@loc",    TryGetString(asset,"Location")       ?? string.Empty),
        new MySqlParameter("@status", TryGetString(asset,"Status")         ?? string.Empty),
        new MySqlParameter("@manu",   TryGetString(asset,"Manufacturer")   ?? string.Empty),
        new MySqlParameter("@model",  TryGetString(asset,"Model")          ?? string.Empty),
        new MySqlParameter("@pdate",  (object?)(object?)(object?)TryGet<DateTime>(asset,"PurchaseDate") ?? DBNull.Value),
        new MySqlParameter("@wexp",   (object?)(object?)(object?)TryGet<DateTime>(asset,"WarrantyExpiry") ?? DBNull.Value),
        new MySqlParameter("@value",  (object?)(object?)(object?)TryGet<decimal>(asset,"Value") ?? DBNull.Value),
        new MySqlParameter("@notes",  TryGetString(asset,"Notes")          ?? string.Empty),
        new MySqlParameter("@modby",  actorUserId),
        new MySqlParameter("@id",     Convert.ToInt32(TryGet<int>(asset,"Id") ?? 0))
    };

    await ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);

    // unified legacy shim so existing VMs compile
    await LogAssetAuditAsync(Convert.ToInt32(TryGet<int>(asset,"Id") ?? 0),
        "UPDATE", actorUserId, ip, deviceInfo, sessionId, signatureHash).ConfigureAwait(false);
}

/// <summary>
/// Tolerant audit DTO materializer (no hard property references)
/// </summary>
public async Task<List<AuditEntryDto>> GetAuditLogForEntityAsync(
    string tableName,
    int entityId,
    CancellationToken token = default)
{
    const string sql = @"
SELECT
    event_time      AS ts,
    user_id         AS uid,
    event_type      AS action,
    related_module  AS module,
    description     AS descr,
    source_ip       AS ip,
    device_info     AS device,
    session_id      AS session
FROM system_event_log
WHERE table_name = @t AND record_id = @id
ORDER BY event_time DESC;";

    var dt = await ExecuteSelectAsync(sql, new[]
    {
        new MySqlParameter("@t",  tableName ?? string.Empty),
        new MySqlParameter("@id", entityId)
    }, token).ConfigureAwait(false);

    var list = new List<AuditEntryDto>();
    foreach (DataRow r in dt.Rows)
    {
        var dto = Activator.CreateInstance<AuditEntryDto>();

        if (r.Table.Columns.Contains("ts")      && r["ts"]      != DBNull.Value) SetIfExists(dto, "Timestamp",   Convert.ToDateTime(r["ts"]));
        if (r.Table.Columns.Contains("uid")     && r["uid"]     != DBNull.Value) SetIfExists(dto, "UserId",      Convert.ToInt32(r["uid"]));
        if (r.Table.Columns.Contains("action")  && r["action"]  != DBNull.Value) SetIfExists(dto, "Action",      r["action"]!.ToString());
        if (r.Table.Columns.Contains("module")  && r["module"]  != DBNull.Value) SetIfExists(dto, "Module",      r["module"]!.ToString());
        if (r.Table.Columns.Contains("descr")   && r["descr"]   != DBNull.Value) SetIfExists(dto, "Description", r["descr"]!.ToString());
        if (r.Table.Columns.Contains("ip")      && r["ip"]      != DBNull.Value) SetIfExists(dto, "SourceIp",    r["ip"]!.ToString());
        if (r.Table.Columns.Contains("device")  && r["device"]  != DBNull.Value) SetIfExists(dto, "DeviceInfo",  r["device"]!.ToString());
        if (r.Table.Columns.Contains("session") && r["session"] != DBNull.Value) SetIfExists(dto, "SessionId",   r["session"]!.ToString());

        list.Add(dto);
    }
    return list;
}

// schema-tolerant row -> Asset
private static Asset ParseAsset(DataRow row)
{
    var a = Activator.CreateInstance<Asset>();

    if (row.Table.Columns.Contains("id")                    && row["id"]                    != DBNull.Value) SetIfExists(a, "Id",               Convert.ToInt32(row["id"]));
    if (row.Table.Columns.Contains("name"))                                                                 SetIfExists(a, "Name",             row["name"]?.ToString());
    if (row.Table.Columns.Contains("code"))                                                                 SetIfExists(a, "Code",             row["code"]?.ToString());
    if (row.Table.Columns.Contains("description"))                                                          SetIfExists(a, "Description",      row["description"]?.ToString());
    if (row.Table.Columns.Contains("location"))                                                             SetIfExists(a, "Location",         row["location"]?.ToString());
    if (row.Table.Columns.Contains("status"))                                                               SetIfExists(a, "Status",           row["status"]?.ToString());
    if (row.Table.Columns.Contains("manufacturer"))                                                         SetIfExists(a, "Manufacturer",     row["manufacturer"]?.ToString());
    if (row.Table.Columns.Contains("model"))                                                                SetIfExists(a, "Model",            row["model"]?.ToString());
    if (row.Table.Columns.Contains("purchase_date")    && row["purchase_date"]    != DBNull.Value)          SetIfExists(a, "PurchaseDate",     Convert.ToDateTime(row["purchase_date"]));
    if (row.Table.Columns.Contains("warranty_expiry")  && row["warranty_expiry"]  != DBNull.Value)          SetIfExists(a, "WarrantyExpiry",   Convert.ToDateTime(row["warranty_expiry"]));
    if (row.Table.Columns.Contains("value")            && row["value"]            != DBNull.Value)          SetIfExists(a, "Value",            Convert.ToDecimal(row["value"]));
    if (row.Table.Columns.Contains("notes"))                                                                 SetIfExists(a, "Notes",            row["notes"]?.ToString());
    if (row.Table.Columns.Contains("last_modified")    && row["last_modified"]    != DBNull.Value)          SetIfExists(a, "LastModified",     Convert.ToDateTime(row["last_modified"]));
    if (row.Table.Columns.Contains("last_modified_by_id") && row["last_modified_by_id"] != DBNull.Value)    SetIfExists(a, "LastModifiedById", Convert.ToInt32(row["last_modified_by_id"]));

    return a;
}

#endregion


public Task DeleteAssetAsync(
    int id,
    string ip,
    string deviceInfo,
    string? sessionId,
    int actorUserId,
    CancellationToken token = default)
{
    // forward to canonical signature
    return DeleteAssetAsync(id, actorUserId, ip, deviceInfo, sessionId, token);
}

public Task RollbackAssetAsync(
    int id,
    string ip,
    string deviceInfo,
    string? sessionId,
    int actorUserId,
    CancellationToken token = default)
{
    // forward to canonical signature
    return RollbackAssetAsync(id, actorUserId, ip, deviceInfo, sessionId, null, token);
}

public Task<string> ExportAssetsAsync(
    IEnumerable<Asset> rows,
    string ip,
    string deviceInfo,
    string? sessionId,
    int actorUserId,
    CancellationToken token = default)
{
    // default to xlsx and forward to canonical signature
    return ExportAssetsAsync(rows, "xlsx", actorUserId, ip, deviceInfo, sessionId, token);
}

public async Task LogAssetAuditAsync(
    int assetId,
    string action,
    int actorUserId,
    string? ip = null,
    string? deviceInfo = null,
    string? sessionId = null,
    string? signatureHash = null,
    string? note = null,
    CancellationToken token = default)
{
    var desc = $"Asset#{assetId} {action}."
             + (string.IsNullOrWhiteSpace(signatureHash) ? "" : $" sig={signatureHash}")
             + (string.IsNullOrWhiteSpace(note) ? "" : $" · {note}");

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: action,
        tableName: "assets",
        module: "AssetModule",
        recordId: assetId,
        description: desc,
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId,
        token: token
    ).ConfigureAwait(false);
}

public async Task DeleteAssetAsync(
    int id,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM assets WHERE id=@id",
        new[] { new MySqlParameter("@id", id) },
        token
    ).ConfigureAwait(false);

    await LogAssetAuditAsync(
        assetId: id,
        action: "DELETE",
        actorUserId: actorUserId,
        ip: ip,
        deviceInfo: deviceInfo,
        sessionId: sessionId,
        note: "Asset deleted",
        token: token
    ).ConfigureAwait(false);
}

public async Task RollbackAssetAsync(
    int id,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? contextJson = null,
    CancellationToken token = default)
{
    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "ROLLBACK",
        tableName: "assets",
        module: "AssetModule",
        recordId: id,
        description: $"Asset rollback. Context: {contextJson ?? ""}",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId,
        token: token
    ).ConfigureAwait(false);
}

public async Task<string> ExportAssetsAsync(
    IEnumerable<Asset> rows,
    string format,
    int actorUserId = 1,
    string ip = "system",
    string deviceInfo = "server",
    string? sessionId = null,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "xlsx" : format.ToLowerInvariant();
    string filePath = $"/export/assets_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";
    int count = rows?.Count() ?? 0;

    // optional unified export/print log (if you have the method from Region 15)
    try
    {
        await SaveExportPrintLogAsync(
            userId: actorUserId,
            format: fmt,
            tableName: "assets",
            filterUsed: $"count={count}",
            filePath: filePath,
            sourceIp: ip,
            note: "Assets export",
            token: token
        ).ConfigureAwait(false);
    }
    catch { /* if SaveExportPrintLogAsync isn't present, ignore */ }

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "EXPORT",
        tableName: "assets",
        module: "AssetModule",
        recordId: null,
        description: $"Exported {count} assets to {filePath}.",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId,
        token: token
    ).ConfigureAwait(false);

    return filePath;
}

// convenience overload used by some screens
public Task<string> ExportAssetsAsync(IEnumerable<Asset> rows, CancellationToken token = default)
    => ExportAssetsAsync(rows, "xlsx", 1, "system", "server", null, token);

// some ViewModels call GetAttachmentsAsync(int assetId). We map that to the
// existing attachments API (Region 18) which expects table name + id.
public Task<List<Attachment>> GetAttachmentsAsync(int assetId, CancellationToken token = default)
    => GetAttachmentsAsync("assets", assetId, token);


#region === 03 · MACHINE / COMPONENT =======================================
/// <summary>
/// Returns every machine. The <paramref name="includeAudit"/> flag is kept for caller compatibility,
/// though this method currently returns only machine rows (audits are accessible via LogSystemEvent).
/// </summary>
public async Task<List<Machine>> GetAllMachinesAsync(bool includeAudit = false, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM machines ORDER BY name, id", null, token).ConfigureAwait(false);
    var list = new List<Machine>();
    foreach (DataRow r in dt.Rows) list.Add(ParseMachine(r));
    return list;
}

/// <summary>
/// Legacy overload maintained for compatibility with older callers that didn’t expose <c>includeAudit</c>.
/// </summary>
public Task<List<Machine>> GetAllMachinesAsync(CancellationToken token) =>
    GetAllMachinesAsync(includeAudit: false, token);

/// <summary>Returns a single machine by primary key.</summary>
public async Task<Machine?> GetMachineByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM machines WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseMachine(dt.Rows[0]);
}

/// <summary>Returns every component across all machines (helper used by calibration screens).</summary>
public async Task<List<MachineComponent>> GetAllComponentsAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM machine_components ORDER BY name, id", null, token).ConfigureAwait(false);
    var list = new List<MachineComponent>();
    foreach (DataRow r in dt.Rows) list.Add(ParseMachineComponent(r));
    return list;
}

/// <summary>Returns all components for a specific machine.</summary>
public async Task<List<MachineComponent>> GetComponentsForMachineAsync(int machineId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM machine_components WHERE machine_id=@mid ORDER BY name, id",
        new[] { new MySqlParameter("@mid", machineId) }, token).ConfigureAwait(false);

    var list = new List<MachineComponent>();
    foreach (DataRow r in dt.Rows) list.Add(ParseMachineComponent(r));
    return list;
}

/// <summary>Returns a component by primary key.</summary>
public async Task<MachineComponent?> GetComponentByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM machine_components WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseMachineComponent(dt.Rows[0]);
}

// ======================= MACHINE UPSERT / DELETE ============================

/// <summary>
/// Inserts a new machine or updates an existing one (schema-tolerant) and writes an audit event.
/// </summary>
/// <param name="m">Machine model (dynamic/POCO tolerated via reflection helpers).</param>
/// <param name="update"><c>true</c> to update; <c>false</c> to insert.</param>
/// <param name="actorUserId">Acting user id for audit trail.</param>
/// <param name="ip">Source IP (defaults to "system").</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Primary key of the affected machine.</returns>
public async Task<int> InsertOrUpdateMachineAsync(
    Machine m,
    bool update,
    int actorUserId,
    string ip = "system",
    CancellationToken token = default)
{
    if (m is null) throw new ArgumentNullException(nameof(m));

    string sig = string.IsNullOrWhiteSpace(m.DigitalSignature)
        ? GenerateDigitalSignatureForMachine(m)
        : m.DigitalSignature;

    string sql = !update
        ? @"INSERT INTO machines
            (code,name,description,model,manufacturer,location,install_date,procurement_date,warranty_until,status,
             urs_doc,decommission_date,decommission_reason,serial_number,acquisition_cost,rfid_tag,qr_code,
             iot_device_id,cloud_device_guid,is_critical,lifecycle_phase,note,
             last_modified,last_modified_by_id,digital_signature)
          VALUES
            (@code,@name,@desc,@model,@manu,@loc,@install,@proc,@warranty,@status,
             @urs,@decomdate,@decomreason,@serial,@cost,@rfid,@qrcode,
             @iot,@cloud,@critical,@lifecycle,@note,
             NOW(),@modby,@sig)"
        : @"UPDATE machines SET
             code=@code,name=@name,description=@desc,model=@model,manufacturer=@manu,location=@loc,
             install_date=@install,procurement_date=@proc,warranty_until=@warranty,status=@status,
             urs_doc=@urs,decommission_date=@decomdate,decommission_reason=@decomreason,serial_number=@serial,
             acquisition_cost=@cost,rfid_tag=@rfid,qr_code=@qrcode,
             iot_device_id=@iot,cloud_device_guid=@cloud,is_critical=@critical,lifecycle_phase=@lifecycle,note=@note,
             last_modified=NOW(),last_modified_by_id=@modby,digital_signature=@sig
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@code",    (object?)TryGetString(m, "Code") ?? DBNull.Value),
        new("@name",    (object?)TryGetString(m, "Name") ?? DBNull.Value),
        new("@desc",    (object?)TryGetString(m, "Description") ?? DBNull.Value),
        new("@model",   (object?)TryGetString(m, "Model") ?? DBNull.Value),
        new("@manu",    (object?)TryGetString(m, "Manufacturer") ?? DBNull.Value),
        new("@loc",     (object?)TryGetString(m, "Location") ?? DBNull.Value),
        new("@install", (object?)TryGet<DateTime>(m, "InstallDate") ?? DBNull.Value),
        new("@proc",    (object?)TryGet<DateTime>(m, "ProcurementDate") ?? DBNull.Value),
        new("@warranty",(object?)TryGet<DateTime>(m, "WarrantyUntil") ?? DBNull.Value),
        new("@status",  (object?)TryGetString(m, "Status") ?? DBNull.Value),
        new("@urs",     (object?)TryGetString(m, "UrsDoc") ?? DBNull.Value),
        new("@decomdate",(object?)TryGet<DateTime>(m, "DecommissionDate") ?? DBNull.Value),
        new("@decomreason",(object?)TryGetString(m, "DecommissionReason") ?? DBNull.Value),
        new("@serial",  (object?)TryGetString(m, "SerialNumber") ?? DBNull.Value),
        new("@cost",    (object?)TryGet<decimal>(m, "AcquisitionCost") ?? DBNull.Value),
        new("@rfid",    (object?)TryGetString(m, "RfidTag") ?? DBNull.Value),
        new("@qrcode",  (object?)TryGetString(m, "QrCode") ?? DBNull.Value),
        new("@iot",     (object?)TryGetString(m, "IoTDeviceId") ?? DBNull.Value),
        new("@cloud",   (object?)TryGetString(m, "CloudDeviceGuid") ?? DBNull.Value),
        new("@critical",(object?)TryGet<bool>(m, "IsCritical") ?? DBNull.Value),
        new("@lifecycle",(object?)TryGetString(m, "LifecyclePhase") ?? DBNull.Value),
        new("@note",    (object?)TryGetString(m, "Note") ?? DBNull.Value),
        new("@modby",   actorUserId),
        new("@sig",     sig)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(m, "Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(m, "Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: update ? "MCH_UPDATE" : "MCH_CREATE",
        tableName: "machines",
        module: "MachineModule",
        recordId: id,
        description: update ? "Machine updated" : "Machine created",
        ip: ip,
        severity: "audit",
        deviceInfo: null,
        sessionId: null
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Overload: insert/update with device context.
/// </summary>
public async Task<int> InsertOrUpdateMachineAsync(
    Machine m, bool update, int actorUserId, string ip, string deviceInfo, CancellationToken token)
{
    int id = await InsertOrUpdateMachineAsync(m, update, actorUserId, ip, token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, update ? "MCH_UPDATE_CTX" : "MCH_CREATE_CTX", "machines", "MachineModule",
        id, $"client-device={deviceInfo ?? ""}", ip, "info", deviceInfo, null).ConfigureAwait(false);
    return id;
}

/// <summary>
/// Overload: insert/update with device + session context.
/// </summary>
public async Task<int> InsertOrUpdateMachineAsync(
    Machine m, bool update, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
{
    int id = await InsertOrUpdateMachineAsync(m, update, actorUserId, ip, token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, update ? "MCH_UPDATE_CTX" : "MCH_CREATE_CTX", "machines", "MachineModule",
        id, $"client-device={deviceInfo ?? ""}", ip, "info", deviceInfo, sessionId).ConfigureAwait(false);
    return id;
}

/// <summary>
/// Convenience wrapper that decides insert vs update by presence of <c>Id</c>.
/// </summary>
public Task<int> SaveMachineAsync(
    Machine m,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId,
    CancellationToken token = default)
{
    bool update = (TryGet<int>(m, "Id") ?? 0) > 0;
    return InsertOrUpdateMachineAsync(m, update, actorUserId, ip, deviceInfo, sessionId, token);
}

/// <summary>Deletes a machine and audits the operation.</summary>
public async Task DeleteMachineAsync(int id, int actorUserId, string ip = "system", CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM machines WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "MCH_DELETE", "machines", "MachineModule",
        id, "Machine deleted", ip, "audit", null, null).ConfigureAwait(false);
}

/// <summary>Delete overload with device context.</summary>
public async Task DeleteMachineAsync(int id, int actorUserId, string ip, string deviceInfo, CancellationToken token)
{
    await DeleteMachineAsync(id, actorUserId, ip, token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "MCH_DELETE_CTX", "machines", "MachineModule",
        id, "Machine deleted (extended context).", ip, "info", deviceInfo, null).ConfigureAwait(false);
}

/// <summary>Delete overload with device + session context.</summary>
public async Task DeleteMachineAsync(int id, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
{
    await DeleteMachineAsync(id, actorUserId, ip, token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "MCH_DELETE_CTX", "machines", "MachineModule",
        id, "Machine deleted (extended context).", ip, "info", deviceInfo, sessionId).ConfigureAwait(false);
}

// ======================= MACHINE EXPORT / ROLLBACK / AUDIT ==================

/// <summary>
/// Creates an export entry for the provided machines and writes an audit event.
/// This method records the export in <c>export_print_log</c> and returns the file path.
/// </summary>
public async Task<string> ExportMachinesAsync(
    IEnumerable<Machine> rows,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string format = "zip",
    int actorUserId = 0,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
    string filePath = $"/export/machines_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'machines',@filter,@path,@ip,'Machines export')",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "MCH_EXPORT", "machines", "MachineModule",
        null, $"Exported {(rows?.Count() ?? 0)} machines to {filePath}.", ip, "audit", deviceInfo, sessionId).ConfigureAwait(false);

    return filePath;
}

/// <summary>
/// Convenience wrapper for exports invoked from list/grid views.
/// </summary>
public Task<string> ExportMachinesFromViewAsync(
    List<Machine> rows,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
    => ExportMachinesAsync(rows, ip, deviceInfo, sessionId, "zip", actorUserId, token);

/// <summary>Simple rollback that re-applies a snapshot via update.</summary>
public Task<int> RollbackMachineAsync(Machine snapshot, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
    => InsertOrUpdateMachineAsync(snapshot, update: true, actorUserId, ip, deviceInfo, sessionId, token);

/// <summary>
/// Wrapper that clearly communicates intent: restore machine state from a given snapshot.
/// </summary>
public Task<int> RollbackMachineFromSnapshotAsync(
    Machine snapshot,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId,
    CancellationToken token = default)
    => RollbackMachineAsync(snapshot, actorUserId, ip, deviceInfo, sessionId, token);

/// <summary>Convenience audit for machine actions.</summary>
public async Task LogMachineAuditAsync(
    int machineId,
    int? userId,
    string action,
    string? note,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    await LogSystemEventAsync(
        userId: userId,
        eventType: string.IsNullOrWhiteSpace(action) ? "MCH_ACTION" : action,
        tableName: "machines",
        module: "MachineModule",
        recordId: machineId == 0 ? null : machineId,
        description: note,
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

/// <summary>
/// Overload used by some callers that pass the whole Machine object.
/// </summary>
public Task LogMachineAuditAsync(Machine m, string action, int userId, string ip, string deviceInfo, string? sessionId, string? note, CancellationToken token = default)
    => LogMachineAuditAsync(TryGet<int>(m, "Id") ?? 0, userId, action, note, ip, deviceInfo, sessionId, token);

// ======================= COMPONENT UPSERT / DELETE ==========================

/// <summary>
/// Inserts a new component or updates an existing one (schema-tolerant) and writes audit.
/// </summary>
public async Task<int> InsertOrUpdateComponentAsync(
    MachineComponent c,
    bool update,
    int actorUserId,
    string ip = "system",
    CancellationToken token = default)
{
    if (c is null) throw new ArgumentNullException(nameof(c));

    string sql = !update
        ? @"INSERT INTO machine_components
            (machine_id, code, name, type, sop_doc, status, install_date, last_modified_by_id)
          VALUES
            (@mid,@code,@name,@type,@sop,@status,@install,@modby)"
        : @"UPDATE machine_components SET
             machine_id=@mid, code=@code, name=@name, type=@type, sop_doc=@sop, status=@status, install_date=@install,
             last_modified_by_id=@modby
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@mid",   (object?)TryGet<int>(c, "MachineId") ?? DBNull.Value),
        new("@code",  (object?)TryGetString(c, "Code") ?? DBNull.Value),
        new("@name",  (object?)TryGetString(c, "Name") ?? DBNull.Value),
        new("@type",  (object?)TryGetString(c, "Type") ?? DBNull.Value),
        new("@sop",   (object?)TryGetString(c, "SopDoc") ?? DBNull.Value),
        new("@status",(object?)TryGetString(c, "Status") ?? DBNull.Value),
        new("@install",(object?)TryGet<DateTime>(c, "InstallDate") ?? DBNull.Value),
        new("@modby", actorUserId)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(c, "Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(c, "Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "COMP_UPDATE" : "COMP_CREATE", "machine_components", "MachineModule",
        id, update ? "Component updated" : "Component created", ip, "audit", null, null).ConfigureAwait(false);

    return id;
}

/// <summary>Hard-deletes a component with audit.</summary>
public async Task DeleteComponentAsync(int id, int actorUserId, string ip = "system", CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM machine_components WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "COMP_DELETE", "machine_components", "MachineModule",
        id, "Component deleted", ip, "audit", null, null).ConfigureAwait(false);
}

// ======================= PARSERS (schema-tolerant) ==========================

/// <summary>DataRow → Machine (schema-tolerant mapping).</summary>
private static Machine ParseMachine(DataRow r)
{
    var m = Activator.CreateInstance<Machine>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                 SetIfExists(m, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("code"))                                          SetIfExists(m, "Code", r["code"]?.ToString());
    if (r.Table.Columns.Contains("name"))                                          SetIfExists(m, "Name", r["name"]?.ToString());
    if (r.Table.Columns.Contains("manufacturer"))                                  SetIfExists(m, "Manufacturer", r["manufacturer"]?.ToString());
    if (r.Table.Columns.Contains("location"))                                      SetIfExists(m, "Location", r["location"]?.ToString());
    if (r.Table.Columns.Contains("install_date")      && r["install_date"] != DBNull.Value)
                                                                                    SetIfExists(m, "InstallDate", Convert.ToDateTime(r["install_date"]));
    if (r.Table.Columns.Contains("procurement_date")  && r["procurement_date"] != DBNull.Value)
                                                                                    SetIfExists(m, "ProcurementDate", Convert.ToDateTime(r["procurement_date"]));
    if (r.Table.Columns.Contains("warranty_until")    && r["warranty_until"] != DBNull.Value)
                                                                                    SetIfExists(m, "WarrantyUntil", Convert.ToDateTime(r["warranty_until"]));
    if (r.Table.Columns.Contains("status"))                                       SetIfExists(m, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("urs_doc"))                                      SetIfExists(m, "UrsDoc", r["urs_doc"]?.ToString());
    if (r.Table.Columns.Contains("decommission_date") && r["decommission_date"] != DBNull.Value)
                                                                                    SetIfExists(m, "DecommissionDate", Convert.ToDateTime(r["decommission_date"]));
    if (r.Table.Columns.Contains("decommission_reason"))                           SetIfExists(m, "DecommissionReason", r["decommission_reason"]?.ToString());
    if (r.Table.Columns.Contains("description"))                                   SetIfExists(m, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("model"))                                         SetIfExists(m, "Model", r["model"]?.ToString());
    if (r.Table.Columns.Contains("serial_number"))                                 SetIfExists(m, "SerialNumber", r["serial_number"]?.ToString());
    if (r.Table.Columns.Contains("acquisition_cost") && r["acquisition_cost"] != DBNull.Value)
                                                                                    SetIfExists(m, "AcquisitionCost", Convert.ToDecimal(r["acquisition_cost"]));
    if (r.Table.Columns.Contains("rfid_tag"))                                      SetIfExists(m, "RfidTag", r["rfid_tag"]?.ToString());
    if (r.Table.Columns.Contains("qr_code"))                                       SetIfExists(m, "QrCode", r["qr_code"]?.ToString());
    if (r.Table.Columns.Contains("iot_device_id"))                                 SetIfExists(m, "IoTDeviceId", r["iot_device_id"]?.ToString()); // NOTE: IoT capital T
    if (r.Table.Columns.Contains("cloud_device_guid"))                             SetIfExists(m, "CloudDeviceGuid", r["cloud_device_guid"]?.ToString());
    if (r.Table.Columns.Contains("is_critical")        && r["is_critical"] != DBNull.Value)
                                                                                    SetIfExists(m, "IsCritical", Convert.ToBoolean(r["is_critical"]));
    if (r.Table.Columns.Contains("lifecycle_phase"))                                SetIfExists(m, "LifecyclePhase", r["lifecycle_phase"]?.ToString());
    if (r.Table.Columns.Contains("note"))                                           SetIfExists(m, "Note", r["note"]?.ToString());
    if (r.Table.Columns.Contains("last_modified")     && r["last_modified"] != DBNull.Value)
                                                                                    SetIfExists(m, "LastModified", Convert.ToDateTime(r["last_modified"]));
    if (r.Table.Columns.Contains("last_modified_by_id") && r["last_modified_by_id"] != DBNull.Value)
                                                                                    SetIfExists(m, "LastModifiedById", Convert.ToInt32(r["last_modified_by_id"]));
    if (r.Table.Columns.Contains("digital_signature"))                              SetIfExists(m, "DigitalSignature", r["digital_signature"]?.ToString());

    return m;
}

/// <summary>DataRow → MachineComponent (schema-tolerant mapping).</summary>
private static MachineComponent ParseMachineComponent(DataRow r)
{
    var c = Activator.CreateInstance<MachineComponent>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                 SetIfExists(c, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("machine_id") && r["machine_id"] != DBNull.Value) SetIfExists(c, "MachineId", Convert.ToInt32(r["machine_id"]));
    if (r.Table.Columns.Contains("code"))                                          SetIfExists(c, "Code", r["code"]?.ToString());
    if (r.Table.Columns.Contains("name"))                                          SetIfExists(c, "Name", r["name"]?.ToString());
    if (r.Table.Columns.Contains("type"))                                          SetIfExists(c, "Type", r["type"]?.ToString());
    if (r.Table.Columns.Contains("sop_doc"))                                       SetIfExists(c, "SopDoc", r["sop_doc"]?.ToString());
    if (r.Table.Columns.Contains("status"))                                        SetIfExists(c, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("install_date") && r["install_date"] != DBNull.Value)
                                                                                    SetIfExists(c, "InstallDate", Convert.ToDateTime(r["install_date"]));
    return c;
}

// ======================= SIGNATURE ==========================================

/// <summary>Builds a deterministic digital signature for a machine using SHA-256.</summary>
private static string GenerateDigitalSignatureForMachine(object m)
{
    string code   = TryGetString(m, "Code") ?? "";
    string name   = TryGetString(m, "Name") ?? "";
    string model  = TryGetString(m, "Model") ?? "";
    string manu   = TryGetString(m, "Manufacturer") ?? "";
    string serial = TryGetString(m, "SerialNumber") ?? "";
    string status = TryGetString(m, "Status") ?? "";

    string data = $"{code}|{name}|{model}|{manu}|{serial}|{status}";
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    return Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data)));
}

#endregion
#region === 04 · CALIBRATIONS ===============================================

// Get all calibrations (latest first)
public async Task<List<Calibration>> GetAllCalibrationsAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM calibrations ORDER BY calibration_date DESC, id DESC",
        null, token).ConfigureAwait(false);

    var list = new List<Calibration>();
    foreach (DataRow r in dt.Rows) list.Add(ParseCalibration(r));
    return list;
}

public async Task<Calibration?> GetCalibrationByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM calibrations WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseCalibration(dt.Rows[0]);
}

public async Task<List<Calibration>> GetCalibrationsForComponentAsync(int componentId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM calibrations WHERE component_id=@cid ORDER BY calibration_date DESC",
        new[] { new MySqlParameter("@cid", componentId) }, token).ConfigureAwait(false);

    var list = new List<Calibration>();
    foreach (DataRow r in dt.Rows) list.Add(ParseCalibration(r));
    return list;
}

public async Task<List<Calibration>> GetCalibrationsBySupplierAsync(int supplierId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM calibrations WHERE supplier_id=@sid ORDER BY calibration_date DESC",
        new[] { new MySqlParameter("@sid", supplierId) }, token).ConfigureAwait(false);

    var list = new List<Calibration>();
    foreach (DataRow r in dt.Rows) list.Add(ParseCalibration(r));
    return list;
}

/// <summary>Calibrations due in the next N days (or overdue when N&lt;0).</summary>
public async Task<List<Calibration>> GetCalibrationsDueSoonAsync(int days, CancellationToken token = default)
{
    var to = DateTime.UtcNow.AddDays(days);
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM calibrations WHERE next_due IS NOT NULL AND next_due <= @to ORDER BY next_due",
        new[] { new MySqlParameter("@to", to) }, token).ConfigureAwait(false);

    var list = new List<Calibration>();
    foreach (DataRow r in dt.Rows) list.Add(ParseCalibration(r));
    return list;
}

// ======================= INSERT / UPDATE ====================================

/// <summary>Insert or update (schema tolerant) with audit. Auto-computes DigitalSignature when missing.</summary>
public async Task<int> InsertOrUpdateCalibrationAsync(
    Calibration cal,
    bool update,
    int actorUserId,
    string ip = "system",
    CancellationToken token = default)
{
    if (cal is null) throw new ArgumentNullException(nameof(cal));

    // Ensure digital signature
    string sig = string.IsNullOrWhiteSpace(cal.DigitalSignature)
        ? GenerateDigitalSignatureForCalibration(cal)
        : cal.DigitalSignature;

    string sql = !update
        ? @"INSERT INTO calibrations
           (component_id, supplier_id, calibration_date, next_due, cert_doc,
            result, comment, digital_signature, last_modified_by_id, source_ip)
           VALUES
           (@cid,@sid,@cdate,@next,@cert,@result,@comment,@sig,@modby,@ip)"
        : @"UPDATE calibrations SET
           component_id=@cid, supplier_id=@sid, calibration_date=@cdate, next_due=@next, cert_doc=@cert,
           result=@result, comment=@comment, digital_signature=@sig,
           last_modified=NOW(), last_modified_by_id=@modby, source_ip=@ip
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@cid",   (object?)TryGet<int>(cal, "ComponentId")     ?? DBNull.Value),
        new("@sid",   (object?)TryGet<int>(cal, "SupplierId")      ?? DBNull.Value),
        new("@cdate", (object?)TryGet<DateTime>(cal, "CalibrationDate") ?? DBNull.Value),
        new("@next",  (object?)TryGet<DateTime>(cal, "NextDue")    ?? DBNull.Value),
        new("@cert",  (object?)TryGetString(cal, "CertDoc")        ?? DBNull.Value),
        new("@result",(object?)TryGetString(cal, "Result")         ?? DBNull.Value),
        new("@comment",(object?)TryGetString(cal, "Comment")       ?? DBNull.Value),
        new("@sig",   sig ?? string.Empty),
        new("@modby", actorUserId),
        new("@ip",    ip ?? string.Empty)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(cal, "Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(cal, "Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: update ? "UPDATE" : "CREATE",
        tableName: "calibrations",
        module: "CalibrationModule",
        recordId: id,
        description: update ? "Calibration updated" : "Calibration created",
        ip: ip,
        severity: "audit",
        deviceInfo: null,
        sessionId: null
    ).ConfigureAwait(false);

    return id;
}

// Overload: with deviceInfo
public async Task<int> InsertOrUpdateCalibrationAsync(
    Calibration cal,
    bool update,
    int actorUserId,
    string ip,
    string deviceInfo,
    CancellationToken token)
{
    int id = await InsertOrUpdateCalibrationAsync(cal, update, actorUserId, ip, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: update ? "UPDATE_CTX" : "CREATE_CTX",
        tableName: "calibrations", module: "CalibrationModule", recordId: id,
        description: $"client-device={deviceInfo ?? ""}", ip: ip, severity: "info", deviceInfo: deviceInfo, sessionId: null
    ).ConfigureAwait(false);

    return id;
}

// Overload: with deviceInfo + sessionId
public async Task<int> InsertOrUpdateCalibrationAsync(
    Calibration cal,
    bool update,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId,
    CancellationToken token = default)
{
    int id = await InsertOrUpdateCalibrationAsync(cal, update, actorUserId, ip, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: update ? "UPDATE_CTX" : "CREATE_CTX",
        tableName: "calibrations", module: "CalibrationModule", recordId: id,
        description: $"client-device={deviceInfo ?? ""}", ip: ip, severity: "info", deviceInfo: deviceInfo, sessionId: sessionId
    ).ConfigureAwait(false);

    return id;
}

// ======================= DELETE ============================================

public async Task DeleteCalibrationAsync(int id, int actorUserId, string ip = "system", CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM calibrations WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "DELETE", tableName: "calibrations", module: "CalibrationModule",
        recordId: id, description: "Calibration deleted", ip: ip, severity: "audit", deviceInfo: null, sessionId: null
    ).ConfigureAwait(false);
}

public async Task DeleteCalibrationAsync(int id, int actorUserId, string ip, string deviceInfo, CancellationToken token)
{
    await DeleteCalibrationAsync(id, actorUserId, ip, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "DELETE_CTX", tableName: "calibrations", module: "CalibrationModule",
        recordId: id, description: "Calibration deleted (extended context).", ip: ip, severity: "info", deviceInfo: deviceInfo, sessionId: null
    ).ConfigureAwait(false);
}

public async Task DeleteCalibrationAsync(int id, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
{
    await DeleteCalibrationAsync(id, actorUserId, ip, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "DELETE_CTX", tableName: "calibrations", module: "CalibrationModule",
        recordId: id, description: "Calibration deleted (extended context).", ip: ip, severity: "info", deviceInfo: deviceInfo, sessionId: sessionId
    ).ConfigureAwait(false);
}

// ======================= FILTERS / VIEWS ====================================

public async Task<List<Calibration>> GetCalibrationsByDateRangeAsync(DateTime from, DateTime to, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM calibrations WHERE calibration_date BETWEEN @from AND @to ORDER BY calibration_date",
        new[] { new MySqlParameter("@from", from), new MySqlParameter("@to", to) }, token).ConfigureAwait(false);

    var list = new List<Calibration>();
    foreach (DataRow r in dt.Rows) list.Add(ParseCalibration(r));
    return list;
}

/// <summary>
/// Returns a filtered view (backed by <c>vw_calibrations_filter</c>).  
/// NOTE: field names here match the view: use <c>component_id</c> for component filter.
/// </summary>
public async Task<DataTable> GetCalibrationsViewAsync(
    int? componentId = null, int? supplierId = null, DateTime? from = null, DateTime? to = null, CancellationToken token = default)
{
    var sb = new StringBuilder("SELECT * FROM vw_calibrations_filter WHERE 1=1");
    var pars = new List<MySqlParameter>();

    if (componentId.HasValue) { sb.Append(" AND component_id=@cid"); pars.Add(new MySqlParameter("@cid", componentId.Value)); }
    if (supplierId.HasValue)  { sb.Append(" AND supplier_id=@sid");   pars.Add(new MySqlParameter("@sid", supplierId.Value)); }
    if (from.HasValue)        { sb.Append(" AND calibration_date >= @from"); pars.Add(new MySqlParameter("@from", from.Value)); }
    if (to.HasValue)          { sb.Append(" AND calibration_date <= @to");   pars.Add(new MySqlParameter("@to",   to.Value)); }

    sb.Append(" ORDER BY calibration_date DESC, id DESC");
    return await ExecuteSelectAsync(sb.ToString(), pars.ToArray(), token).ConfigureAwait(false);
}

// ======================= PARSE ==============================================

private static Calibration ParseCalibration(DataRow r)
{
    var c = Activator.CreateInstance<Calibration>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                        SetIfExists(c, "Id",               Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("component_id") && r["component_id"] != DBNull.Value)    SetIfExists(c, "ComponentId",      Convert.ToInt32(r["component_id"]));
    if (r.Table.Columns.Contains("supplier_id")  && r["supplier_id"]  != DBNull.Value)    SetIfExists(c, "SupplierId",       Convert.ToInt32(r["supplier_id"]));
    if (r.Table.Columns.Contains("calibration_date") && r["calibration_date"] != DBNull.Value)
                                                                                           SetIfExists(c, "CalibrationDate",  Convert.ToDateTime(r["calibration_date"]));
    if (r.Table.Columns.Contains("next_due") && r["next_due"] != DBNull.Value)            SetIfExists(c, "NextDue",          Convert.ToDateTime(r["next_due"]));
    if (r.Table.Columns.Contains("cert_doc"))                                              SetIfExists(c, "CertDoc",          r["cert_doc"]?.ToString());
    if (r.Table.Columns.Contains("result"))                                                SetIfExists(c, "Result",           r["result"]?.ToString());
    if (r.Table.Columns.Contains("comment"))                                               SetIfExists(c, "Comment",          r["comment"]?.ToString());
    if (r.Table.Columns.Contains("digital_signature"))                                     SetIfExists(c, "DigitalSignature", r["digital_signature"]?.ToString());
    if (r.Table.Columns.Contains("last_modified") && r["last_modified"] != DBNull.Value)   SetIfExists(c, "LastModified",     Convert.ToDateTime(r["last_modified"]));
    if (r.Table.Columns.Contains("last_modified_by_id") && r["last_modified_by_id"] != DBNull.Value)
                                                                                           SetIfExists(c, "LastModifiedById", Convert.ToInt32(r["last_modified_by_id"]));
    if (r.Table.Columns.Contains("source_ip"))                                             SetIfExists(c, "SourceIp",         r["source_ip"]?.ToString());

    return c;
}

// ======================= SIGNATURE ==========================================

/// <summary>Generates a SHA-256 signature for calibration integrity (schema tolerant).</summary>
private static string GenerateDigitalSignatureForCalibration(object cal)
{
    var cid   = TryGet<int>(cal, "ComponentId")?.ToString() ?? "";
    var sid   = TryGet<int>(cal, "SupplierId")?.ToString()  ?? "";
    var cdate = TryGet<DateTime>(cal, "CalibrationDate")?.ToString("O") ?? "";
    var next  = TryGet<DateTime>(cal, "NextDue")?.ToString("O") ?? "";
    var cert  = TryGetString(cal, "CertDoc") ?? "";
    var res   = TryGetString(cal, "Result")  ?? "";
    var note  = TryGetString(cal, "Comment") ?? "";

    string data = $"{cid}|{sid}|{cdate}|{next}|{cert}|{res}|{note}";
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    return Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data)));
}

#endregion
#region === 05 · WORK ORDERS ================================================

/// <summary>Returns all work orders.</summary>
public async Task<List<WorkOrder>> GetAllWorkOrdersAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM work_orders", null, token).ConfigureAwait(false);
    var list = new List<WorkOrder>();
    foreach (DataRow r in dt.Rows) list.Add(ParseWorkOrder(r));
    return list;
}

/// <summary>
/// Convenience method kept for callers that expect a "full" projection. Currently returns the same
/// shape as <see cref="GetAllWorkOrdersAsync(CancellationToken)"/>; relationships can be populated by the caller.
/// </summary>
public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync(CancellationToken token = default)
    => GetAllWorkOrdersAsync(token);

/// <summary>Returns a work order by ID.</summary>
public async Task<WorkOrder?> GetWorkOrderByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM work_orders WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    return dt.Rows.Count == 0 ? null : ParseWorkOrder(dt.Rows[0]);
}

/// <summary>
/// Inserts a new work order or updates an existing one (schema/model-tolerant) and logs an audit event.
/// </summary>
public async Task<int> InsertOrUpdateWorkOrderAsync(
    WorkOrder wo,
    bool update,
    int actorUserId,
    string ip = "system",
    string device = "N/A",
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO work_orders
           (machine_id, component_id, type, created_by, assigned_to, date_open, date_close,
            description, result, status, digital_signature, priority, related_incident)
           VALUES
           (@mid,@cid,@type,@created,@assigned,@open,@close,@desc,@res,@status,@sig,@prio,@incident)"
        : @"UPDATE work_orders SET
           machine_id=@mid, component_id=@cid, type=@type, created_by=@created, assigned_to=@assigned,
           date_open=@open, date_close=@close, description=@desc, result=@res, status=@status,
           digital_signature=@sig, priority=@prio, related_incident=@incident
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@mid",      (object?)(TryGet<int>(wo,"MachineId") ?? TryGet<int>(wo,"Machine")) ?? DBNull.Value),
        new("@cid",      (object?)(TryGet<int>(wo,"ComponentId") ?? TryGet<int>(wo,"Component")) ?? DBNull.Value),
        new("@type",     (object?) (TryGetString(wo,"Type") ?? TryGetString(wo,"WorkType")) ?? DBNull.Value),
        new("@created",  (object?)(TryGet<int>(wo,"CreatedById") ?? TryGet<int>(wo,"CreatedBy")) ?? DBNull.Value),
        new("@assigned", (object?)(TryGet<int>(wo,"AssignedToId") ?? TryGet<int>(wo,"AssignedTo")) ?? DBNull.Value),
        new("@open",     (object?)(TryGet<DateTime>(wo,"DateOpen") ?? TryGet<DateTime>(wo,"OpenDate")) ?? DBNull.Value),
        new("@close",    (object?)(TryGet<DateTime>(wo,"DateClose") ?? TryGet<DateTime>(wo,"CloseDate")) ?? DBNull.Value),
        new("@desc",     (object?) (TryGetString(wo,"Description") ?? TryGetString(wo,"Desc") ?? TryGetString(wo,"Details")) ?? DBNull.Value),
        new("@res",      (object?) (TryGetString(wo,"Result") ?? TryGetString(wo,"Outcome")) ?? DBNull.Value),
        new("@status",   (object?) (TryGetString(wo,"Status") ?? TryGetString(wo,"State")) ?? DBNull.Value),
        new("@sig",      (object?) (TryGetString(wo,"DigitalSignature") ?? TryGetString(wo,"Signature")) ?? DBNull.Value),
        new("@prio",     (object?) (TryGetString(wo,"Priority") ?? TryGetString(wo,"PriorityText")) ?? DBNull.Value),
        new("@incident", (object?)(TryGet<int>(wo,"RelatedIncidentId") ?? TryGet<int>(wo,"RelatedIncident") ?? TryGet<int>(wo,"IncidentId")) ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(wo,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    int id = update
        ? (TryGet<int>(wo,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "work_orders", "WorkOrderModule",
        id, update ? "Work order updated" : "Work order created", ip, "audit", device, null).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Wrapper: creates a new work order (decides insert vs update based on Id).
/// </summary>
public Task<int> AddWorkOrderAsync(
    WorkOrder wo,
    int actorUserId,
    string ip,
    string device,
    string? sessionId = null,
    CancellationToken token = default)
{
    bool update = (TryGet<int>(wo, "Id") ?? 0) > 0;
    return InsertOrUpdateWorkOrderAsync(wo, update, actorUserId, ip, device, token);
}

/// <summary>Hard-deletes a work order with audit.</summary>
public async Task DeleteWorkOrderAsync(
    int id, int actorUserId, string ip = "system", string device = "N/A", string? sessionId = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM work_orders WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "DELETE", "work_orders", "WorkOrderModule",
        id, "Work order deleted", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>
/// Updates a work order status, writes <c>work_order_status_log</c> and a structured audit row.
/// </summary>
public async Task UpdateWorkOrderStatusAsync(
    int workOrderId, string newStatus, int actorUserId, string note = "",
    string ip = "system", string device = "N/A", CancellationToken token = default)
{
    // Get old status
    var dt = await ExecuteSelectAsync(
        "SELECT status FROM work_orders WHERE id=@id",
        new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
    string oldStatus = dt.Rows.Count == 0 ? string.Empty : (dt.Rows[0]["status"]?.ToString() ?? "");

    // Update WO table (auto-close on terminal states)
    await ExecuteNonQueryAsync(
        "UPDATE work_orders SET status=@status, date_close=CASE WHEN @status IN ('zavrsen','odbijen','otkazan') THEN NOW() ELSE date_close END WHERE id=@id",
        new[]
        {
            new MySqlParameter("@status", newStatus ?? string.Empty),
            new MySqlParameter("@id", workOrderId)
        }, token).ConfigureAwait(false);

    // Insert status log
    await ExecuteNonQueryAsync(@"
INSERT INTO work_order_status_log (work_order_id, old_status, new_status, changed_by, note)
VALUES (@wo,@old,@new,@by,@note)",
        new[]
        {
            new MySqlParameter("@wo",   workOrderId),
            new MySqlParameter("@old",  oldStatus),
            new MySqlParameter("@new",  newStatus ?? string.Empty),
            new MySqlParameter("@by",   actorUserId),
            new MySqlParameter("@note", note ?? string.Empty)
        }, token).ConfigureAwait(false);

    // Audit table
    await ExecuteNonQueryAsync(@"
INSERT INTO work_order_audit (work_order_id, user_id, action, old_value, new_value, source_ip, device_info, note)
VALUES (@wo,@uid,'UPDATE',@old,@new,@ip,@dev,@note)",
        new[]
        {
            new MySqlParameter("@wo",  workOrderId),
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@old", oldStatus ?? string.Empty),
            new MySqlParameter("@new", newStatus ?? string.Empty),
            new MySqlParameter("@ip",  ip ?? string.Empty),
            new MySqlParameter("@dev", device ?? string.Empty),
            new MySqlParameter("@note", note ?? string.Empty)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "UPDATE_STATUS", "work_orders", "WorkOrderModule",
        workOrderId, $"WO status {oldStatus} → {newStatus}", ip, "audit", device, null).ConfigureAwait(false);
}

/// <summary>
/// Approves a work order (status → <c>odobren</c>) with audit and status log.
/// </summary>
public Task ApproveWorkOrderAsync(
    int workOrderId, int actorUserId, string note = "", string ip = "system", string device = "N/A", CancellationToken token = default)
    => UpdateWorkOrderStatusAsync(workOrderId, "odobren", actorUserId, note, ip, device, token);

/// <summary>
/// Closes a work order (status → <c>zavrsen</c>) with audit and status log.
/// </summary>
public Task CloseWorkOrderAsync(
    int workOrderId, int actorUserId, string note = "", string ip = "system", string device = "N/A", CancellationToken token = default)
    => UpdateWorkOrderStatusAsync(workOrderId, "zavrsen", actorUserId, note, ip, device, token);

/// <summary>
/// Escalates a work order (status → <c>eskaliran</c>) with audit and status log.
/// </summary>
public Task EscalateWorkOrderAsync(
    int workOrderId, int actorUserId, string note = "", string ip = "system", string device = "N/A", CancellationToken token = default)
    => UpdateWorkOrderStatusAsync(workOrderId, "eskaliran", actorUserId, note, ip, device, token);

/// <summary>Adds a comment to a work order (auto-increments revision).</summary>
public async Task<int> AddWorkOrderCommentAsync(
    int workOrderId, int userId, string comment, CancellationToken token = default)
{
    // Compute next revision_no
    int nextRev = Convert.ToInt32(
        await ExecuteScalarAsync(
            "SELECT COALESCE(MAX(revision_no),0)+1 FROM work_order_comments WHERE work_order_id=@wo",
            new[] { new MySqlParameter("@wo", workOrderId) }, token).ConfigureAwait(false) ?? 1);

    await ExecuteNonQueryAsync(@"
INSERT INTO work_order_comments (work_order_id, user_id, comment, revision_no)
VALUES (@wo,@uid,@c,@rev)",
        new[]
        {
            new MySqlParameter("@wo",  workOrderId),
            new MySqlParameter("@uid", userId),
            new MySqlParameter("@c",   comment ?? string.Empty),
            new MySqlParameter("@rev", nextRev)
        }, token).ConfigureAwait(false);

    return nextRev;
}

/// <summary>Returns all comments for a work order, newest first.</summary>
public async Task<List<WorkOrderComment>> GetWorkOrderCommentsAsync(int workOrderId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM work_order_comments WHERE work_order_id=@wo ORDER BY created_at DESC",
        new[] { new MySqlParameter("@wo", workOrderId) }, token).ConfigureAwait(false);

    var list = new List<WorkOrderComment>();
    foreach (DataRow r in dt.Rows) list.Add(ParseWorkOrderComment(r));
    return list;
}

/// <summary>Adds an e-signature to a work order.</summary>
public async Task<int> AddWorkOrderSignatureAsync(
    int workOrderId, int userId, string signatureHash, string signatureType = "zakljucavanje",
    string? pinUsed = null, string? note = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
INSERT INTO work_order_signatures
(work_order_id, user_id, signature_hash, pin_used, signature_type, note)
VALUES (@wo,@uid,@sig,@pin,@stype,@note)",
        new[]
        {
            new MySqlParameter("@wo",   workOrderId),
            new MySqlParameter("@uid",  userId),
            new MySqlParameter("@sig",  signatureHash ?? string.Empty),
            new MySqlParameter("@pin",  (object?)pinUsed ?? DBNull.Value),
            new MySqlParameter("@stype", signatureType ?? "zakljucavanje"),
            new MySqlParameter("@note", (object?)note ?? DBNull.Value)
        }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

/// <summary>
/// Writes a work-order audit entry (helper).
/// </summary>
public async Task LogWorkOrderAuditAsync(
    int workOrderId, int userId, string action, string? note,
    string ip = "system", string device = "N/A", int? incidentId = null, int? capaId = null,
    string? oldValue = null, string? newValue = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
INSERT INTO work_order_audit
(work_order_id, user_id, action, old_value, new_value, source_ip, device_info, incident_id, capa_id, note)
VALUES (@wo,@uid,@action,@old,@new,@ip,@dev,@incident,@capa,@note)",
        new[]
        {
            new MySqlParameter("@wo", workOrderId),
            new MySqlParameter("@uid", userId),
            new MySqlParameter("@action", action ?? "UPDATE"),
            new MySqlParameter("@old", (object?)oldValue ?? DBNull.Value),
            new MySqlParameter("@new", (object?)newValue ?? DBNull.Value),
            new MySqlParameter("@ip",  ip ?? string.Empty),
            new MySqlParameter("@dev", device ?? string.Empty),
            new MySqlParameter("@incident", (object?)incidentId ?? DBNull.Value),
            new MySqlParameter("@capa",     (object?)capaId     ?? DBNull.Value),
            new MySqlParameter("@note", (object?)note ?? DBNull.Value)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// Creates an export entry for the provided work orders and writes an audit event. Returns file path.
/// </summary>
public async Task<string> ExportWorkOrdersAsync(
    IEnumerable<WorkOrder> rows,
    int actorUserId,
    string ip,
    string device,
    string? sessionId = null,
    string format = "zip",
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
    string filePath = $"/export/workorders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'work_orders',@filter,@path,@ip,'Work orders export')",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "WO_EXPORT", "work_orders", "WorkOrderModule",
        null, $"Exported {(rows?.Count() ?? 0)} work orders to {filePath}.", ip, "audit", device, sessionId).ConfigureAwait(false);

    return filePath;
}

/// <summary>
/// Restores a work order from a snapshot by performing an update.
/// </summary>
public Task<int> RollbackWorkOrderAsync(
    WorkOrder snapshot,
    int actorUserId,
    string ip,
    string device,
    string? sessionId = null,
    CancellationToken token = default)
    => InsertOrUpdateWorkOrderAsync(snapshot, update: true, actorUserId, ip, device, token);

// ======================= PARSERS (schema-tolerant) ==========================

/// <summary>DataRow → WorkOrder (schema-tolerant mapping).</summary>
private static WorkOrder ParseWorkOrder(DataRow r)
{
    var w = Activator.CreateInstance<WorkOrder>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                     SetIfExists(w, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("machine_id") && r["machine_id"] != DBNull.Value)     SetIfExists(w, "MachineId", Convert.ToInt32(r["machine_id"]));
    if (r.Table.Columns.Contains("component_id") && r["component_id"] != DBNull.Value) SetIfExists(w, "ComponentId", Convert.ToInt32(r["component_id"]));
    if (r.Table.Columns.Contains("type"))                                              SetIfExists(w, "Type", r["type"]?.ToString());
    if (r.Table.Columns.Contains("created_by") && r["created_by"] != DBNull.Value)     SetIfExists(w, "CreatedById", Convert.ToInt32(r["created_by"]));
    if (r.Table.Columns.Contains("assigned_to") && r["assigned_to"] != DBNull.Value)   SetIfExists(w, "AssignedToId", Convert.ToInt32(r["assigned_to"]));
    if (r.Table.Columns.Contains("date_open") && r["date_open"] != DBNull.Value)       SetIfExists(w, "DateOpen", Convert.ToDateTime(r["date_open"]));
    if (r.Table.Columns.Contains("date_close") && r["date_close"] != DBNull.Value)     SetIfExists(w, "DateClose", Convert.ToDateTime(r["date_close"]));
    if (r.Table.Columns.Contains("description"))                                       SetIfExists(w, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("result"))                                            SetIfExists(w, "Result", r["result"]?.ToString());
    if (r.Table.Columns.Contains("status"))                                            SetIfExists(w, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("digital_signature"))                                 SetIfExists(w, "DigitalSignature", r["digital_signature"]?.ToString());
    if (r.Table.Columns.Contains("priority"))                                          SetIfExists(w, "Priority", r["priority"]?.ToString());
    if (r.Table.Columns.Contains("related_incident") && r["related_incident"] != DBNull.Value)
                                                                                       SetIfExists(w, "RelatedIncidentId", Convert.ToInt32(r["related_incident"]));

    return w;
}

/// <summary>DataRow → WorkOrderComment (schema-tolerant mapping).</summary>
private static WorkOrderComment ParseWorkOrderComment(DataRow r)
{
    var c = Activator.CreateInstance<WorkOrderComment>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)               SetIfExists(c, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("work_order_id") && r["work_order_id"] != DBNull.Value)
                                                                                 SetIfExists(c, "WorkOrderId", Convert.ToInt32(r["work_order_id"]));
    if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)     SetIfExists(c, "UserId", Convert.ToInt32(r["user_id"]));
    if (r.Table.Columns.Contains("comment"))                                     SetIfExists(c, "Comment", r["comment"]?.ToString());   // if model uses Text/Body instead:
                                                                                 SetIfExists(c, "Text", r["comment"]?.ToString());
                                                                                 SetIfExists(c, "Body", r["comment"]?.ToString());
    if (r.Table.Columns.Contains("revision_no") && r["revision_no"] != DBNull.Value)
                                                                                 SetIfExists(c, "RevisionNo", Convert.ToInt32(r["revision_no"]));
    if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
                                                                                 SetIfExists(c, "CreatedAt", Convert.ToDateTime(r["created_at"]));
    // common alternates
    if (r.Table.Columns.Contains("created_on") && r["created_on"] != DBNull.Value)
                                                                                 SetIfExists(c, "CreatedOn", Convert.ToDateTime(r["created_on"]));
    return c;
}

#endregion
#region === 06 · INCIDENTS ==================================================

/// <summary>
/// Returns all incidents (rows from <c>incident_log</c>).
/// Schema tolerant: uses <see cref="ParseIncident"/> to map columns safely.
/// </summary>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="Incident"/>.</returns>
public async Task<List<Incident>> GetAllIncidentsAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM incident_log ORDER BY detected_at DESC",
        null,
        token
    ).ConfigureAwait(false);

    var list = new List<Incident>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIncident(r));
    return list;
}

/// <summary>
/// Returns a single incident by ID.
/// </summary>
/// <param name="id">Incident id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns><see cref="Incident"/> instance or <c>null</c> if not found.</returns>
public async Task<Incident?> GetIncidentByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM incident_log WHERE id=@id",
        new[] { new MySqlParameter("@id", id) },
        token
    ).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseIncident(dt.Rows[0]);
}

/// <summary>
/// Inserts or updates an incident (model/schema tolerant).
/// </summary>
/// <param name="inc">Incident model (POCO).</param>
/// <param name="update"><c>false</c> to insert; <c>true</c> to update.</param>
/// <param name="actorUserId">User performing the change.</param>
/// <param name="ip">Source IP for audit.</param>
/// <param name="device">Device info for audit.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>New or existing incident id.</returns>
public async Task<int> InsertOrUpdateIncidentAsync(
    Incident inc,
    bool update,
    int actorUserId,
    string ip = "system",
    string device = "N/A",
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO incident_log
           (detected_at, reported_by, severity, title, description, resolved, resolved_at, resolved_by,
            actions_taken, follow_up, note, source_ip)
           VALUES
           (@det,@rep,@sev,@title,@desc,@res,@res_at,@res_by,@act,@follow,@note,@ip)"
        : @"UPDATE incident_log SET
           detected_at=@det, reported_by=@rep, severity=@sev, title=@title, description=@desc, resolved=@res,
           resolved_at=@res_at, resolved_by=@res_by, actions_taken=@act, follow_up=@follow, note=@note, source_ip=@ip
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@det",     (object?)TryGet<DateTime>(inc, "DetectedAt") ?? DBNull.Value),
        new("@rep",     (object?)(TryGet<int>(inc, "ReportedById") ?? TryGet<int>(inc, "ReportedBy")) ?? DBNull.Value),
        new("@sev",     (object?)TryGetString(inc, "Severity")       ?? DBNull.Value),
        new("@title",   (object?)TryGetString(inc, "Title")          ?? DBNull.Value),
        new("@desc",    (object?)TryGetString(inc, "Description")    ?? DBNull.Value),
        new("@res",     (object?)TryGet<bool>(inc, "Resolved")       ?? DBNull.Value),
        new("@res_at",  (object?)TryGet<DateTime>(inc, "ResolvedAt") ?? DBNull.Value),
        new("@res_by",  (object?)(TryGet<int>(inc, "ResolvedById") ?? TryGet<int>(inc, "ResolvedBy")) ?? DBNull.Value),
        new("@act",     (object?)TryGetString(inc, "ActionsTaken")   ?? DBNull.Value),
        new("@follow",  (object?)TryGetString(inc, "FollowUp")       ?? DBNull.Value),
        new("@note",    (object?)TryGetString(inc, "Note")           ?? DBNull.Value),
        new("@ip",      ip ?? string.Empty)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(inc, "Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(inc, "Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: update ? "UPDATE" : "CREATE",
        tableName: "incident_log",
        module: "IncidentModule",
        recordId: id,
        description: update ? "Incident updated" : "Incident created",
        ip: ip,
        severity: "audit",
        deviceInfo: device,
        sessionId: null
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Hard-deletes an incident with full audit log.
/// </summary>
/// <param name="id">Incident id.</param>
/// <param name="actorUserId">User performing delete.</param>
/// <param name="ip">Source IP.</param>
/// <param name="device">Device info.</param>
/// <param name="token">Cancellation token.</param>
public async Task DeleteIncidentAsync(
    int id,
    int actorUserId,
    string ip = "system",
    string device = "N/A",
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM incident_log WHERE id=@id",
        new[] { new MySqlParameter("@id", id) },
        token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "DELETE",
        tableName: "incident_log",
        module: "IncidentModule",
        recordId: id,
        description: "Incident deleted",
        ip: ip,
        severity: "audit",
        deviceInfo: device,
        sessionId: null
    ).ConfigureAwait(false);
}

/// <summary>
/// Convenience overload: deletes an incident without audit actor metadata (kept for compatibility).
/// </summary>
/// <param name="id">Incident id.</param>
/// <param name="token">Cancellation token.</param>
public async Task DeleteIncidentAsync(int id, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM incident_log WHERE id=@id",
        new[] { new MySqlParameter("@id", id) },
        token
    ).ConfigureAwait(false);

    // No canonical system log here (no actor info). Prefer the actor overload where possible.
}

/// <summary>
/// Inserts an incident audit entry (strongly-typed).
/// Computes and sets <c>DigitalSignature</c> automatically if not provided.
/// </summary>
/// <param name="audit">Audit entry.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>New audit id.</returns>
public async Task<int> InsertIncidentAuditAsync(IncidentAudit audit, CancellationToken token = default)
{
    if (audit is null) throw new ArgumentNullException(nameof(audit));

    // Set digital signature if missing
    if (string.IsNullOrWhiteSpace(audit.DigitalSignature))
    {
        var sig = GenerateDigitalSignatureForIncidentAudit(audit);
        SetIfExists(audit, "DigitalSignature", sig);
    }

    var sql = @"
INSERT INTO incident_audit
(incident_id, user_id, action, old_value, new_value, action_at, note,
 source_ip, digital_signature, capa_id, work_order_id)
VALUES
(@iid,@uid,@act,@old,@new,@at,@note,@ip,@sig,@capa,@wo)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@iid",  (object?)TryGet<int>(audit, "IncidentId") ?? DBNull.Value),
        new MySqlParameter("@uid",  (object?)TryGet<int>(audit, "UserId")     ?? DBNull.Value),
        new MySqlParameter("@act",  (object?)TryGetString(audit, "Action")    ?? "UPDATE"),
        new MySqlParameter("@old",  (object?)TryGetString(audit, "OldValue")  ?? string.Empty),
        new MySqlParameter("@new",  (object?)TryGetString(audit, "NewValue")  ?? string.Empty),
        new MySqlParameter("@at",   (object?)TryGet<DateTime>(audit, "ActionAt") ?? DateTime.UtcNow),
        new MySqlParameter("@note", (object?)TryGetString(audit, "Note")      ?? string.Empty),
        new MySqlParameter("@ip",   (object?)TryGetString(audit, "SourceIp")  ?? string.Empty),
        new MySqlParameter("@sig",  (object?)TryGetString(audit, "DigitalSignature") ?? string.Empty),
        new MySqlParameter("@capa", (object?)TryGet<int>(audit, "CapaId")     ?? DBNull.Value),
        new MySqlParameter("@wo",   (object?)TryGet<int>(audit, "WorkOrderId")?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int newId = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return newId;
}

/// <summary>
/// Backwards-compatible wrapper that accepts <c>object</c>.
/// Prefer <see cref="InsertIncidentAuditAsync(IncidentAudit, CancellationToken)"/>.
/// </summary>
/// <param name="audit">Audit object (schema tolerant).</param>
/// <param name="token">Cancellation token.</param>
/// <returns>New audit id.</returns>
public async Task<int> InsertIncidentAuditAsync(object audit, CancellationToken token = default)
{
    // compute signature and set it back if property exists
    var sig = GenerateDigitalSignatureForIncidentAudit(audit);
    SetIfExists(audit, "DigitalSignature", sig);

    var iid     = TryGet<int>(audit, "IncidentId");
    var uid     = TryGet<int>(audit, "UserId");
    var action  = TryGetString(audit, "Action")    ?? "UPDATE";
    var oldVal  = TryGetString(audit, "OldValue")  ?? string.Empty;
    var newVal  = TryGetString(audit, "NewValue")  ?? string.Empty;
    var at      = TryGet<DateTime>(audit, "ActionAt") ?? DateTime.UtcNow;
    var note    = TryGetString(audit, "Note")      ?? string.Empty;
    var srcIp   = TryGetString(audit, "SourceIp")  ?? string.Empty;
    var capaId  = TryGet<int>(audit, "CapaId");
    var woId    = TryGet<int>(audit, "WorkOrderId");

    const string sql = @"
INSERT INTO incident_audit
(incident_id, user_id, action, old_value, new_value, action_at, note,
 source_ip, digital_signature, capa_id, work_order_id)
VALUES
(@iid,@uid,@act,@old,@new,@at,@note,@ip,@sig,@capa,@wo)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@iid",  (object?)iid   ?? DBNull.Value),
        new MySqlParameter("@uid",  (object?)uid   ?? DBNull.Value),
        new MySqlParameter("@act",  action),
        new MySqlParameter("@old",  oldVal),
        new MySqlParameter("@new",  newVal),
        new MySqlParameter("@at",   at),
        new MySqlParameter("@note", note),
        new MySqlParameter("@ip",   srcIp),
        new MySqlParameter("@sig",  sig),
        new MySqlParameter("@capa", (object?)capaId ?? DBNull.Value),
        new MySqlParameter("@wo",   (object?)woId   ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int newId = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return newId;
}

/// <summary>
/// Returns a single incident audit entry by its ID.
/// </summary>
/// <param name="id">Audit id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Audit entry or <c>null</c> if not found.</returns>
public async Task<IncidentAudit?> GetIncidentAuditByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM incident_audit WHERE id=@id",
        new[] { new MySqlParameter("@id", id) },
        token
    ).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseIncidentAudit(dt.Rows[0]);
}

/// <summary>
/// Returns all audit entries for a given incident (new richer method).
/// </summary>
/// <param name="incidentId">Incident id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="IncidentAudit"/>.</returns>
public async Task<List<IncidentAudit>> GetIncidentAuditsByIncidentIdAsync(int incidentId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM incident_audit WHERE incident_id=@iid ORDER BY action_at DESC",
        new[] { new MySqlParameter("@iid", incidentId) },
        token
    ).ConfigureAwait(false);

    var list = new List<IncidentAudit>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIncidentAudit(r));
    return list;
}

/// <summary>
/// Returns all audit entries performed by a given user (new richer method).
/// </summary>
/// <param name="userId">User id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="IncidentAudit"/>.</returns>
public async Task<List<IncidentAudit>> GetIncidentAuditsByUserIdAsync(int userId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM incident_audit WHERE user_id=@uid ORDER BY action_at DESC",
        new[] { new MySqlParameter("@uid", userId) },
        token
    ).ConfigureAwait(false);

    var list = new List<IncidentAudit>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIncidentAudit(r));
    return list;
}

/// <summary>
/// Backwards-compatible method kept for callers: returns audits for a given incident.
/// Internally calls <see cref="GetIncidentAuditsByIncidentIdAsync(int, CancellationToken)"/>.
/// </summary>
/// <param name="incidentId">Incident id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="IncidentAudit"/>.</returns>
public async Task<List<IncidentAudit>> GetIncidentAuditsAsync(int incidentId, CancellationToken token = default)
    => await GetIncidentAuditsByIncidentIdAsync(incidentId, token).ConfigureAwait(false);

/// <summary>
/// Parses a row into <see cref="Incident"/> (schema tolerant).
/// </summary>
/// <param name="r">Data row.</param>
/// <returns>Incident.</returns>
private static Incident ParseIncident(DataRow r)
{
    var inc = Activator.CreateInstance<Incident>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                       SetIfExists(inc, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("detected_at") && r["detected_at"] != DBNull.Value)     SetIfExists(inc, "DetectedAt", Convert.ToDateTime(r["detected_at"]));
    if (r.Table.Columns.Contains("reported_by") && r["reported_by"] != DBNull.Value)     SetIfExists(inc, "ReportedById", Convert.ToInt32(r["reported_by"]));
    if (r.Table.Columns.Contains("severity"))                                            SetIfExists(inc, "Severity", r["severity"]?.ToString());
    if (r.Table.Columns.Contains("title"))                                               SetIfExists(inc, "Title", r["title"]?.ToString());
    if (r.Table.Columns.Contains("description"))                                         SetIfExists(inc, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("resolved") && r["resolved"] != DBNull.Value)           SetIfExists(inc, "Resolved", Convert.ToBoolean(r["resolved"]));
    if (r.Table.Columns.Contains("resolved_at") && r["resolved_at"] != DBNull.Value)     SetIfExists(inc, "ResolvedAt", Convert.ToDateTime(r["resolved_at"]));
    if (r.Table.Columns.Contains("resolved_by") && r["resolved_by"] != DBNull.Value)     SetIfExists(inc, "ResolvedById", Convert.ToInt32(r["resolved_by"]));
    if (r.Table.Columns.Contains("actions_taken"))                                       SetIfExists(inc, "ActionsTaken", r["actions_taken"]?.ToString());
    if (r.Table.Columns.Contains("follow_up"))                                           SetIfExists(inc, "FollowUp", r["follow_up"]?.ToString());
    if (r.Table.Columns.Contains("note"))                                                SetIfExists(inc, "Note", r["note"]?.ToString());
    if (r.Table.Columns.Contains("source_ip"))                                           SetIfExists(inc, "SourceIp", r["source_ip"]?.ToString());

    return inc;
}

/// <summary>
/// Parses a row into <see cref="IncidentAudit"/> (schema tolerant).
/// </summary>
/// <param name="r">Data row.</param>
/// <returns>Incident audit entry.</returns>
private static IncidentAudit ParseIncidentAudit(DataRow r)
{
    var a = Activator.CreateInstance<IncidentAudit>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                   SetIfExists(a, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("incident_id") && r["incident_id"] != DBNull.Value) SetIfExists(a, "IncidentId", Convert.ToInt32(r["incident_id"]));
    if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)         SetIfExists(a, "UserId", Convert.ToInt32(r["user_id"]));
    if (r.Table.Columns.Contains("action"))                                          SetIfExists(a, "Action", r["action"]?.ToString());
    if (r.Table.Columns.Contains("old_value"))                                       SetIfExists(a, "OldValue", r["old_value"]?.ToString());
    if (r.Table.Columns.Contains("new_value"))                                       SetIfExists(a, "NewValue", r["new_value"]?.ToString());
    if (r.Table.Columns.Contains("action_at") && r["action_at"] != DBNull.Value)     SetIfExists(a, "ActionAt", Convert.ToDateTime(r["action_at"]));
    if (r.Table.Columns.Contains("note"))                                            SetIfExists(a, "Note", r["note"]?.ToString());
    if (r.Table.Columns.Contains("source_ip"))                                       SetIfExists(a, "SourceIp", r["source_ip"]?.ToString());
    if (r.Table.Columns.Contains("digital_signature"))                                SetIfExists(a, "DigitalSignature", r["digital_signature"]?.ToString());
    if (r.Table.Columns.Contains("capa_id") && r["capa_id"] != DBNull.Value)         SetIfExists(a, "CapaId", Convert.ToInt32(r["capa_id"]));
    if (r.Table.Columns.Contains("work_order_id") && r["work_order_id"] != DBNull.Value)
                                                                                      SetIfExists(a, "WorkOrderId", Convert.ToInt32(r["work_order_id"]));
    return a;
}

/// <summary>
/// Generates a SHA-256 signature for IncidentAudit integrity (schema tolerant).
/// </summary>
/// <param name="audit">Audit object or POCO.</param>
/// <returns>Base64-encoded SHA-256 signature.</returns>
private static string GenerateDigitalSignatureForIncidentAudit(object audit)
{
    var iid    = TryGet<int>(audit, "IncidentId")?.ToString() ?? "";
    var uid    = TryGet<int>(audit, "UserId")?.ToString()     ?? "";
    var act    = TryGetString(audit, "Action")                ?? "";
    var oldV   = TryGetString(audit, "OldValue")              ?? "";
    var newV   = TryGetString(audit, "NewValue")              ?? "";
    var at     = TryGet<DateTime>(audit, "ActionAt")?.ToString("O") ?? "";
    var note   = TryGetString(audit, "Note")                  ?? "";
    var ip     = TryGetString(audit, "SourceIp")              ?? "";
    var capa   = TryGet<int>(audit, "CapaId")?.ToString()     ?? "";
    var wo     = TryGet<int>(audit, "WorkOrderId")?.ToString()?? "";

    string data = $"{iid}|{uid}|{act}|{oldV}|{newV}|{at}|{note}|{ip}|{capa}|{wo}";
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    return Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data)));
}

#endregion
#region === 06A · INCIDENT REPORTS (Workflow & Audit) =======================

public async Task<List<IncidentReport>> GetAllIncidentReportsFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM incident_reports ORDER BY reported_at DESC", null, token
    ).ConfigureAwait(false);

    var list = new List<IncidentReport>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIncidentReport(r));
    return list;
}

public async Task<int> InitiateIncidentReportAsync(IncidentReport report, CancellationToken token = default)
{
    var title        = TryGetString(report, "Title")        ?? "New Incident";
    var incidentType = TryGetString(report, "IncidentType") ?? "accident";
    var status       = TryGetString(report, "Status")       ?? "reported";
    var reportedBy   = TryGetString(report, "ReportedBy")   ?? "";
    var reportedAt   = TryGet<DateTime>(report, "ReportedAt") ?? DateTime.UtcNow;
    var assignedTo   = TryGetString(report, "AssignedTo");
    var area         = TryGetString(report, "Area");
    var description  = TryGetString(report, "Description");
    var rootCause    = TryGetString(report, "RootCause");
    var impactScore  = TryGet<int>(report, "ImpactScore");
    var linkedCapa   = TryGet<bool>(report, "LinkedCAPA");
    var deviceInfo   = TryGetString(report, "DeviceInfo");
    var sessionId    = TryGetString(report, "SessionId");
    var ipAddress    = TryGetString(report, "IpAddress");

    const string sql = @"
INSERT INTO incident_reports
(title, incident_type, status, reported_by, reported_at, assigned_to, area,
 description, root_cause, impact_score, linked_capa, device_info, session_id, ip_address)
VALUES
(@title,@type,@status,@rby,@rat,@ass,@area,@desc,@root,@impact,@capa,@dev,@sid,@ip)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@title",  title),
        new MySqlParameter("@type",   incidentType),
        new MySqlParameter("@status", status),
        new MySqlParameter("@rby",    (object?)reportedBy ?? DBNull.Value),
        new MySqlParameter("@rat",    reportedAt),
        new MySqlParameter("@ass",    (object?)assignedTo  ?? DBNull.Value),
        new MySqlParameter("@area",   (object?)area        ?? DBNull.Value),
        new MySqlParameter("@desc",   (object?)description ?? DBNull.Value),
        new MySqlParameter("@root",   (object?)rootCause   ?? DBNull.Value),
        new MySqlParameter("@impact", (object?)impactScore ?? DBNull.Value),
        new MySqlParameter("@capa",   (object?)linkedCapa  ?? DBNull.Value),
        new MySqlParameter("@dev",    (object?)deviceInfo  ?? DBNull.Value),
        new MySqlParameter("@sid",    (object?)sessionId   ?? DBNull.Value),
        new MySqlParameter("@ip",     (object?)ipAddress   ?? DBNull.Value),
    }, token).ConfigureAwait(false);

    var newId = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: null,
        eventType: "IR_INITIATE",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: newId,
        description: $"Incident report created: {title}",
        ip: ipAddress ?? "system",
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);

    return newId;
}

public async Task AssignIncidentReportAsync(
    int incidentReportId,
    int assignedToUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? note = null,
    CancellationToken token = default)
{
    var pars = new List<MySqlParameter>
    {
        new("@id", incidentReportId),
        new("@status", "assigned"),
        new("@note", (object?)note ?? DBNull.Value),
    };

    string sql = "UPDATE incident_reports SET status=@status";
    sql += ", assigned_to_id=@uid"; pars.Add(new MySqlParameter("@uid", assignedToUserId));
    sql += " WHERE id=@id";

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: assignedToUserId,
        eventType: "IR_ASSIGN",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: incidentReportId,
        description: $"Assigned incident report to user #{assignedToUserId}. {note}",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

public async Task InvestigateIncidentReportAsync(
    IncidentReport report,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    var id          = TryGet<int>(report, "Id") ?? 0;
    var rootCause   = TryGetString(report, "RootCause");
    var description = TryGetString(report, "Description");
    var impact      = TryGet<int>(report, "ImpactScore");

    const string sql = @"
UPDATE incident_reports
SET status='investigated',
    root_cause=@root,
    description=@desc,
    impact_score=@impact
WHERE id=@id";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@id", id),
        new MySqlParameter("@root", (object?)rootCause ?? DBNull.Value),
        new MySqlParameter("@desc", (object?)description ?? DBNull.Value),
        new MySqlParameter("@impact", (object?)impact ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "IR_INVESTIGATE",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: id,
        description: "Incident report investigated",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

public async Task ApproveIncidentReportAsync(
    int incidentReportId,
    int approverUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE incident_reports SET status='approved' WHERE id=@id",
        new[] { new MySqlParameter("@id", incidentReportId) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: approverUserId,
        eventType: "IR_APPROVE",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: incidentReportId,
        description: "Incident report approved",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

public async Task EscalateIncidentReportAsync(
    int incidentReportId,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? reason = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE incident_reports SET status='escalated' WHERE id=@id",
        new[] { new MySqlParameter("@id", incidentReportId) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "IR_ESCALATE",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: incidentReportId,
        description: $"Incident report escalated. {reason}",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

public async Task CloseIncidentReportAsync(
    int incidentReportId,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? note = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE incident_reports SET status='closed' WHERE id=@id",
        new[] { new MySqlParameter("@id", incidentReportId) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "IR_CLOSE",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: incidentReportId,
        description: $"Incident report closed. {note}",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

public async Task<string> ExportIncidentReportsAsync(
    IEnumerable<IncidentReport> rows,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string format = "zip",
    int actorUserId = 0,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
    string filePath = $"/export/inc_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log
(user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'incident_reports',@filter,@path,@ip,'Incident reports export')",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "IR_EXPORT",
        tableName: "incident_reports",
        module: "IncidentReports",
        recordId: null,
        description: $"Exported {(rows?.Count() ?? 0)} incident reports to {filePath}.",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);

    return filePath;
}

private static IncidentReport ParseIncidentReport(DataRow r)
{
    var ir = Activator.CreateInstance<IncidentReport>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                     SetIfExists(ir, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("title"))                                             SetIfExists(ir, "Title", r["title"]?.ToString());
    if (r.Table.Columns.Contains("incident_type"))                                     SetIfExists(ir, "IncidentType", r["incident_type"]?.ToString());
    if (r.Table.Columns.Contains("status"))                                            SetIfExists(ir, "Status", r["status"]?.ToString());

    if (r.Table.Columns.Contains("reported_by"))                                       SetIfExists(ir, "ReportedBy", r["reported_by"]?.ToString());
    if (r.Table.Columns.Contains("reported_at") && r["reported_at"] != DBNull.Value)   SetIfExists(ir, "ReportedAt", Convert.ToDateTime(r["reported_at"]));

    if (r.Table.Columns.Contains("assigned_to"))                                       SetIfExists(ir, "AssignedTo", r["assigned_to"]?.ToString());
    if (r.Table.Columns.Contains("assigned_to_id") && r["assigned_to_id"] != DBNull.Value)
                                                                                       SetIfExists(ir, "AssignedToId", Convert.ToInt32(r["assigned_to_id"]));

    if (r.Table.Columns.Contains("area"))                                              SetIfExists(ir, "Area", r["area"]?.ToString());
    if (r.Table.Columns.Contains("description"))                                       SetIfExists(ir, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("root_cause"))                                        SetIfExists(ir, "RootCause", r["root_cause"]?.ToString());

    if (r.Table.Columns.Contains("impact_score") && r["impact_score"] != DBNull.Value) SetIfExists(ir, "ImpactScore", Convert.ToInt32(r["impact_score"]));
    if (r.Table.Columns.Contains("linked_capa") && r["linked_capa"] != DBNull.Value)   SetIfExists(ir, "LinkedCAPA", Convert.ToBoolean(r["linked_capa"]));

    if (r.Table.Columns.Contains("device_info"))                                       SetIfExists(ir, "DeviceInfo", r["device_info"]?.ToString());
    if (r.Table.Columns.Contains("session_id"))                                        SetIfExists(ir, "SessionId", r["session_id"]?.ToString());
    if (r.Table.Columns.Contains("ip_address"))                                        SetIfExists(ir, "IpAddress", r["ip_address"]?.ToString());

    return ir;
}

#endregion
#region === 07 · SYSTEM EVENT LOG (WRITE) =====================================

/// <summary>
/*  YasGMP · Canonical System Event Logger
 *  -------------------------------------------------------------------------
 *  Inserts an Annex 11 / 21 CFR Part 11 style entry into <c>system_event_log</c>.
 *  This is the central, canonical event logger.
 *
 *  Backward-compatibility
 *  • Accepts legacy named argument <c>eventCode:</c> as an alias for <paramref name="eventType"/>.
 *  • Accepts legacy named argument <c>table:</c> as an alias for <paramref name="tableName"/>.
 *  • A separate overload (below) supports historic positional calls where a
 *    <see cref="System.Threading.CancellationToken"/> appeared as the 11th argument.
 */
/// </summary>
/// <param name="userId">ID of the user associated with the event (nullable).</param>
/// <param name="eventType">
/// Event type such as <c>CREATE</c>, <c>UPDATE</c>, <c>DELETE</c>, <c>LOGIN</c>, etc.
/// Optional for legacy callers: if omitted and <paramref name="eventCode"/> is provided,
/// that value will be used as the event type.
/// </param>
/// <param name="tableName">Logical/DB table affected (nullable).</param>
/// <param name="module">Module name that triggered the event (nullable).</param>
/// <param name="recordId">Primary key of the affected record (nullable).</param>
/// <param name="description">Detailed description of the event (nullable).</param>
/// <param name="ip">Source IP address for the event origin (nullable).</param>
/// <param name="severity">
/// Event severity level. Typical values: <c>info</c>, <c>warn</c>, <c>error</c>, <c>critical</c>. Default is <c>info</c>.
/// </param>
/// <param name="deviceInfo">Device information string (nullable).</param>
/// <param name="sessionId">Session identifier (nullable).</param>
/// <param name="fieldName">Affected field name (nullable).</param>
/// <param name="oldValue">Previous field value (nullable).</param>
/// <param name="newValue">New field value (nullable).</param>
/// <param name="token">Cancellation token for the async operation.</param>
/// <param name="_allowSourceIpNamedArg">
/// Reserved disambiguation flag for very old call sites that attempted to use <c>sourceIp:</c>.
/// Not used at runtime; present to prevent named-argument compile errors.
/// </param>
/// <param name="eventCode">
/// <b>Legacy alias.</b> If provided (e.g., callers using <c>eventCode:</c>) and
/// <paramref name="eventType"/> is null/empty, this value is mapped to <paramref name="eventType"/>.
/// </param>
/// <param name="table">
/// <b>Legacy alias.</b> If provided (e.g., callers using <c>table:</c>) and
/// <paramref name="tableName"/> is null, this value is mapped to <paramref name="tableName"/>.
/// </param>
public async Task LogSystemEventAsync(
    int?          userId,
    string?       eventType   = null,
    string?       tableName   = null,
    string?       module      = null,
    int?          recordId    = null,
    string?       description = null,
    string?       ip          = null,
    string        severity    = "info",
    string?       deviceInfo  = null,
    string?       sessionId   = null,
    string?       fieldName   = null,
    string?       oldValue    = null,
    string?       newValue    = null,
    CancellationToken token   = default,
    bool           _allowSourceIpNamedArg = false,
    string?       eventCode   = null,   // legacy named-arg alias
    string?       table       = null    // legacy named-arg alias
)
{
    // Map legacy aliases when present
    if (string.IsNullOrWhiteSpace(eventType) && !string.IsNullOrWhiteSpace(eventCode))
        eventType = eventCode;
    tableName ??= table;

    const string sql = @"
INSERT INTO system_event_log
(user_id, event_type, table_name, related_module, record_id, field_name,
 old_value, new_value, description, source_ip, device_info, session_id, severity)
VALUES
(@uid,@etype,@tname,@module,@rid,@field,@old,@new,@desc,@ip,@dev,@sess,@sev);";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid",   (object?)userId    ?? DBNull.Value),
        new MySqlParameter("@etype", eventType          ?? string.Empty),
        new MySqlParameter("@tname", tableName          ?? string.Empty),
        new MySqlParameter("@module",module             ?? string.Empty),
        new MySqlParameter("@rid",   (object?)recordId  ?? DBNull.Value),
        new MySqlParameter("@field", (object?)fieldName ?? DBNull.Value),
        new MySqlParameter("@old",   (object?)oldValue  ?? DBNull.Value),
        new MySqlParameter("@new",   (object?)newValue  ?? DBNull.Value),
        new MySqlParameter("@desc",  description        ?? string.Empty),
        new MySqlParameter("@ip",    ip                 ?? string.Empty),
        new MySqlParameter("@dev",   deviceInfo         ?? string.Empty),
        new MySqlParameter("@sess",  sessionId          ?? string.Empty),
        new MySqlParameter("@sev",   severity           ?? "info")
    }, token).ConfigureAwait(false);
}

/// <summary>
/// <b>Compatibility overload (legacy positional)</b> for historic call sites where
/// a <see cref="System.Threading.CancellationToken"/> was passed as the 11th argument
/// (directly after <paramref name="sessionId"/>). This overload forwards to the
/// canonical implementation above.
/// </summary>
/// <remarks>
/// The parameter name is <c>legacyToken</c> (not <c>token</c>) on purpose to avoid
/// ambiguity when callers use named arguments like <c>token:</c>. Positional calls
/// still bind to this overload; named calls bind to the canonical method.
/// </remarks>
/// <param name="userId">ID of the user associated with the event (nullable).</param>
/// <param name="eventType">Event type such as <c>CREATE</c>, <c>UPDATE</c>, etc. (nullable for legacy mapping).</param>
/// <param name="tableName">Logical/DB table affected (nullable).</param>
/// <param name="module">Module name that triggered the event (nullable).</param>
/// <param name="recordId">Primary key of the affected record (nullable).</param>
/// <param name="description">Detailed description of the event (nullable).</param>
/// <param name="ip">Source IP address for the event origin (nullable).</param>
/// <param name="severity">Event severity level. Default <c>info</c>.</param>
/// <param name="deviceInfo">Device information string (nullable).</param>
/// <param name="sessionId">Session identifier (nullable).</param>
/// <param name="legacyToken">Cancellation token (legacy position #11).</param>
/// <param name="fieldName">Affected field name (nullable).</param>
/// <param name="oldValue">Previous field value (nullable).</param>
/// <param name="newValue">New field value (nullable).</param>
/// <param name="_allowSourceIpNamedArg">
/// Reserved disambiguation flag for very old call sites that attempted to use <c>sourceIp:</c>.
/// </param>
/// <param name="eventCode"><b>Legacy alias.</b> Used if <paramref name="eventType"/> is null/empty.</param>
/// <param name="table"><b>Legacy alias.</b> Used if <paramref name="tableName"/> is null.</param>
public Task LogSystemEventAsync(
    int?          userId,
    string?       eventType,
    string?       tableName,
    string?       module,
    int?          recordId,
    string?       description,
    string?       ip,
    string        severity,
    string?       deviceInfo,
    string?       sessionId,
    CancellationToken legacyToken,               // ← legacy position & different name to avoid CS0121
    string?       fieldName   = null,
    string?       oldValue    = null,
    string?       newValue    = null,
    bool          _allowSourceIpNamedArg = false,
    string?       eventCode   = null,
    string?       table       = null
)
{
    // Forward to the canonical implementation (which handles alias mapping).
    return LogSystemEventAsync(
        userId,
        eventType,
        tableName,
        module,
        recordId,
        description,
        ip,
        severity,
        deviceInfo,
        sessionId,
        fieldName,
        oldValue,
        newValue,
        legacyToken,
        _allowSourceIpNamedArg,
        eventCode,
        table);
}

#endregion
#region === 08 · RBAC =======================================================



/// <summary>
/// Returns <c>true</c> if the user has a direct, non-expired allow entry in <c>user_permissions</c>
/// for the given permission <paramref name="permissionCode"/>.
/// </summary>
public async Task<bool> HasDirectUserPermissionAsync(int userId, string permissionCode, CancellationToken token = default)
{
    const string sql = @"
SELECT 1
FROM user_permissions up
JOIN permissions p ON p.id = up.permission_id
WHERE up.user_id=@uid
  AND p.code=@code
  AND up.allowed=1
  AND (up.expires_at IS NULL OR up.expires_at > NOW())
LIMIT 1;";
    var dt = await ExecuteSelectAsync(sql, new[]
    {
        new MySqlParameter("@uid",  userId),
        new MySqlParameter("@code", permissionCode ?? string.Empty)
    }, token).ConfigureAwait(false);

    return dt.Rows.Count > 0;
}

/// <summary>
/// Returns the active role ids for a user (ignores expired assignments).
/// </summary>
public async Task<List<int>> GetUserRoleIdsAsync(int userId, CancellationToken token = default)
{
    const string sql = @"
SELECT role_id
FROM user_roles
WHERE user_id=@uid
  AND (expires_at IS NULL OR expires_at > NOW());";
    var dt = await ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

    var list = new List<int>();
    foreach (DataRow r in dt.Rows)
    {
        if (r["role_id"] != DBNull.Value) list.Add(Convert.ToInt32(r["role_id"]));
    }
    return list;
}

/// <summary>
/// Returns <c>true</c> if the given role grants the permission code.
/// </summary>
public async Task<bool> HasRolePermissionAsync(int roleId, string permissionCode, CancellationToken token = default)
{
    const string sql = @"
SELECT 1
FROM role_permissions rp
JOIN permissions p ON p.id = rp.permission_id
WHERE rp.role_id=@rid
  AND p.code=@code
LIMIT 1;";
    var dt = await ExecuteSelectAsync(sql, new[]
    {
        new MySqlParameter("@rid",  roleId),
        new MySqlParameter("@code", permissionCode ?? string.Empty)
    }, token).ConfigureAwait(false);

    return dt.Rows.Count > 0;
}

/// <summary>
/// Returns <c>true</c> if user has a live, non-revoked, non-expired delegatation for the permission code.
/// </summary>
public async Task<bool> HasDelegatedPermissionAsync(int userId, string permissionCode, CancellationToken token = default)
{
    const string sql = @"
SELECT 1
FROM delegated_permissions d
JOIN permissions p ON p.id = d.permission_id
WHERE d.to_user_id=@uid
  AND p.code=@code
  AND (d.revoked IS NULL OR d.revoked=0)
  AND d.expires_at > NOW()
LIMIT 1;";
    var dt = await ExecuteSelectAsync(sql, new[]
    {
        new MySqlParameter("@uid",  userId),
        new MySqlParameter("@code", permissionCode ?? string.Empty)
    }, token).ConfigureAwait(false);

    return dt.Rows.Count > 0;
}

/// <summary>
/// All direct, non-expired permission codes for the user.
/// </summary>
public async Task<List<string>> GetUserPermissionCodesAsync(int userId, CancellationToken token = default)
{
    const string sql = @"
SELECT p.code
FROM user_permissions up
JOIN permissions p ON p.id = up.permission_id
WHERE up.user_id=@uid
  AND up.allowed=1
  AND (up.expires_at IS NULL OR up.expires_at > NOW())
ORDER BY p.code;";
    var dt = await ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

    var list = new List<string>();
    foreach (DataRow r in dt.Rows)
    {
        var code = r["code"]?.ToString();
        if (!string.IsNullOrWhiteSpace(code)) list.Add(code);
    }
    return list;
}

/// <summary>
/// All permission codes granted to a role.
/// </summary>
public async Task<List<string>> GetRolePermissionCodesAsync(int roleId, CancellationToken token = default)
{
    const string sql = @"
SELECT p.code
FROM role_permissions rp
JOIN permissions p ON p.id=rp.permission_id
WHERE rp.role_id=@rid
ORDER BY p.code;";
    var dt = await ExecuteSelectAsync(sql, new[] { new MySqlParameter("@rid", roleId) }, token).ConfigureAwait(false);

    var list = new List<string>();
    foreach (DataRow r in dt.Rows)
    {
        var code = r["code"]?.ToString();
        if (!string.IsNullOrWhiteSpace(code)) list.Add(code);
    }
    return list;
}

/// <summary>
/// All live delegated permission codes for the user.
/// </summary>
public async Task<List<string>> GetDelegatedPermissionCodesAsync(int userId, CancellationToken token = default)
{
    const string sql = @"
SELECT p.code
FROM delegated_permissions d
JOIN permissions p ON p.id = d.permission_id
WHERE d.to_user_id=@uid
  AND (d.revoked IS NULL OR d.revoked=0)
  AND d.expires_at > NOW()
ORDER BY p.code;";
    var dt = await ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

    var list = new List<string>();
    foreach (DataRow r in dt.Rows)
    {
        var code = r["code"]?.ToString();
        if (!string.IsNullOrWhiteSpace(code)) list.Add(code);
    }
    return list;
}

/// <summary>
/// Adds (or refreshes) a user→role mapping. Will upsert and log the change.
/// </summary>
public async Task AddUserRoleAsync(
    int userId,
    int roleId,
    int actorUserId,
    DateTime? expiresAt = null,
    string? sourceIp = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    const string upsert = @"
INSERT INTO user_roles (user_id, role_id, granted_by, granted_at, expires_at)
VALUES (@uid,@rid,@actor,NOW(),@exp)
ON DUPLICATE KEY UPDATE
    granted_by = VALUES(granted_by),
    granted_at = VALUES(granted_at),
    expires_at = VALUES(expires_at);";

    await ExecuteNonQueryAsync(upsert, new[]
    {
        new MySqlParameter("@uid",   userId),
        new MySqlParameter("@rid",   roleId),
        new MySqlParameter("@actor", actorUserId),
        new MySqlParameter("@exp",   (object?)expiresAt ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    await LogPermissionChangeAsync(
        targetUserId: userId,
        changedBy: actorUserId,
        changeType: "role",
        roleId: roleId,
        permissionId: null,
        action: "grant",
        reason: string.Empty,
        expiresAt: expiresAt,
        sourceIp: sourceIp,
        sessionId: sessionId,
        token: token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "RBAC_GRANT_ROLE", "user_roles", "RBAC",
        recordId: null, description: $"Granted role #{roleId} to user #{userId}.",
        ip: sourceIp, severity: "security", sessionId: sessionId).ConfigureAwait(false);
}

/// <summary>
/// Removes a user→role mapping and logs the change.
/// </summary>
public async Task RemoveUserRoleAsync(
    int userId,
    int roleId,
    int actorUserId = 0,
    string? reason = null,
    string? sourceIp = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM user_roles WHERE user_id=@uid AND role_id=@rid",
        new[]
        {
            new MySqlParameter("@uid", userId),
            new MySqlParameter("@rid", roleId)
        }, token).ConfigureAwait(false);

    await LogPermissionChangeAsync(
        targetUserId: userId,
        changedBy: actorUserId,
        changeType: "role",
        roleId: roleId,
        permissionId: null,
        action: "revoke",
        reason: reason ?? string.Empty,
        expiresAt: null,
        sourceIp: sourceIp,
        sessionId: sessionId,
        token: token).ConfigureAwait(false);
}

/// <summary>
/// Resolves a permission id from code. Optionally creates it if missing.
/// </summary>
public async Task<int> GetPermissionIdByCodeAsync(string permissionCode, bool createIfMissing = false, string? displayName = null, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT id FROM permissions WHERE code=@code LIMIT 1",
        new[] { new MySqlParameter("@code", permissionCode ?? string.Empty) }, token).ConfigureAwait(false);

    if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["id"]);

    if (!createIfMissing)
        throw new KeyNotFoundException($"Permission code not found: '{permissionCode}'.");

    await ExecuteNonQueryAsync(
        "INSERT INTO permissions (code, name) VALUES (@code,@name)",
        new[]
        {
            new MySqlParameter("@code", permissionCode ?? string.Empty),
            new MySqlParameter("@name", (object?)(displayName ?? permissionCode ?? string.Empty) ?? DBNull.Value)
        }, token).ConfigureAwait(false);

    return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

/// <summary>
/// Adds or updates a direct user permission (override). Also writes <c>permission_change_log</c>.
/// </summary>
public async Task AddUserPermissionAsync(
    int userId,
    int permissionId,
    int grantedBy,
    DateTime? expiresAt,
    bool allowed,
    string? reason = null,
    string? sourceIp = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    const string upsert = @"
INSERT INTO user_permissions (user_id, permission_id, allowed, reason, granted_by, granted_at, expires_at)
VALUES (@uid,@pid,@allow,@reason,@by,NOW(),@exp)
ON DUPLICATE KEY UPDATE
    allowed    = VALUES(allowed),
    reason     = VALUES(reason),
    granted_by = VALUES(granted_by),
    granted_at = VALUES(granted_at),
    expires_at = VALUES(expires_at);";

    await ExecuteNonQueryAsync(upsert, new[]
    {
        new MySqlParameter("@uid",   userId),
        new MySqlParameter("@pid",   permissionId),
        new MySqlParameter("@allow", allowed),
        new MySqlParameter("@reason", reason ?? string.Empty),
        new MySqlParameter("@by",    grantedBy),
        new MySqlParameter("@exp",   (object?)expiresAt ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    await LogPermissionChangeAsync(
        targetUserId: userId,
        changedBy: grantedBy,
        changeType: "permission",
        roleId: null,
        permissionId: permissionId,
        action: allowed ? "grant" : "deny",
        reason: reason ?? string.Empty,
        expiresAt: expiresAt,
        sourceIp: sourceIp,
        sessionId: sessionId,
        token: token).ConfigureAwait(false);
}

/// <summary>
/// Removes a direct user permission and writes <c>permission_change_log</c>.
/// </summary>
public async Task RemoveUserPermissionAsync(
    int userId,
    int permissionId,
    int actorUserId = 0,
    string? reason = null,
    string? sourceIp = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM user_permissions WHERE user_id=@uid AND permission_id=@pid",
        new[]
        {
            new MySqlParameter("@uid", userId),
            new MySqlParameter("@pid", permissionId)
        }, token).ConfigureAwait(false);

    await LogPermissionChangeAsync(
        targetUserId: userId,
        changedBy: actorUserId,
        changeType: "permission",
        roleId: null,
        permissionId: permissionId,
        action: "revoke",
        reason: reason ?? string.Empty,
        expiresAt: null,
        sourceIp: sourceIp,
        sessionId: sessionId,
        token: token).ConfigureAwait(false);
}

/// <summary>
/// Delegates a permission from one user to another (non-revoked, with hard expiry).
/// </summary>
public async Task AddDelegatedPermissionAsync(
    int fromUserId,
    int toUserId,
    int permissionId,
    int grantedBy,
    DateTime expiresAt,
    string? reason = null,
    string? sourceIp = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO delegated_permissions (from_user_id, to_user_id, permission_id, expires_at, reason, granted_by)
VALUES (@from,@to,@pid,@exp,@reason,@by);";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@from",   fromUserId),
        new MySqlParameter("@to",     toUserId),
        new MySqlParameter("@pid",    permissionId),
        new MySqlParameter("@exp",    expiresAt),
        new MySqlParameter("@reason", reason ?? string.Empty),
        new MySqlParameter("@by",     grantedBy)
    }, token).ConfigureAwait(false);

    await LogPermissionChangeAsync(
        targetUserId: toUserId,
        changedBy: grantedBy,
        changeType: "delegation",
        roleId: null,
        permissionId: permissionId,
        action: "grant",
        reason: reason ?? string.Empty,
        expiresAt: expiresAt,
        sourceIp: sourceIp,
        sessionId: sessionId,
        token: token).ConfigureAwait(false);
}

/// <summary>
/// Revokes a live delegation (requires actor id). Keeps your existing implementation + logging.
/// </summary>
public async Task RevokeDelegatedPermissionAsync(
    int delegationId, int actorUserId, string? reason = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
UPDATE delegated_permissions
SET revoked=1, revoked_at=NOW()
WHERE id=@id",
        new[] { new MySqlParameter("@id", delegationId) }, token).ConfigureAwait(false);

    await ExecuteNonQueryAsync(@"
INSERT INTO permission_change_log
(user_id, changed_by, change_type, action, reason)
SELECT to_user_id, @actor, 'delegation', 'revoke', @reason
FROM delegated_permissions WHERE id=@id",
        new[]
        {
            new MySqlParameter("@id",    delegationId),
            new MySqlParameter("@actor", actorUserId),
            new MySqlParameter("@reason", reason ?? string.Empty)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// Overload to support callers that don't pass actor id (fixes CS7036 from RBACService).
/// </summary>
public Task RevokeDelegatedPermissionAsync(int delegationId, CancellationToken token = default)
    => RevokeDelegatedPermissionAsync(delegationId, actorUserId: 0, reason: null, token);

/// <summary>
/// Writes a permission change record into <c>permission_change_log</c>.
/// </summary>
public async Task LogPermissionChangeAsync(
    int targetUserId,
    int changedBy,
    string changeType,
    int? roleId,
    int? permissionId,
    string action,
    string? reason,
    DateTime? expiresAt,
    string? sourceIp = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO permission_change_log
(user_id, changed_by, change_type, role_id, permission_id, action, reason, expires_at, source_ip, session_id, changed_at)
VALUES
(@uid,@actor,@ctype,@rid,@pid,@act,@reason,@exp,@ip,@sess,NOW());";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid",   targetUserId),
        new MySqlParameter("@actor", changedBy),
        new MySqlParameter("@ctype", changeType ?? string.Empty),
        new MySqlParameter("@rid",   (object?)roleId ?? DBNull.Value),
        new MySqlParameter("@pid",   (object?)permissionId ?? DBNull.Value),
        new MySqlParameter("@act",   action ?? string.Empty),
        new MySqlParameter("@reason",(object?)(reason ?? string.Empty) ?? DBNull.Value),
        new MySqlParameter("@exp",   (object?)expiresAt ?? DBNull.Value),
        new MySqlParameter("@ip",    (object?)(sourceIp ?? string.Empty) ?? DBNull.Value),
        new MySqlParameter("@sess",  (object?)(sessionId ?? string.Empty) ?? DBNull.Value)
    }, token).ConfigureAwait(false);
}

/// <summary>
/// Returns all roles. Schema-tolerant mapping.
/// </summary>
public async Task<List<Role>> GetAllRolesAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM roles ORDER BY name, id", null, token).ConfigureAwait(false);
    var list = new List<Role>();
    foreach (DataRow r in dt.Rows)
    {
        var role = Activator.CreateInstance<Role>();
        if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                SetIfExists(role, "Id", Convert.ToInt32(r["id"]));
        if (r.Table.Columns.Contains("name"))                                         SetIfExists(role, "Name", r["name"]?.ToString());
        if (r.Table.Columns.Contains("code"))                                         SetIfExists(role, "Code", r["code"]?.ToString());
        if (r.Table.Columns.Contains("description"))                                  SetIfExists(role, "Description", r["description"]?.ToString());
        list.Add(role);
    }
    return list;
}

/// <summary>
/// Returns all permissions. Schema-tolerant mapping.
/// </summary>
public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM permissions ORDER BY code, id", null, token).ConfigureAwait(false);
    var list = new List<Permission>();
    foreach (DataRow r in dt.Rows)
    {
        var p = Activator.CreateInstance<Permission>();
        if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                SetIfExists(p, "Id", Convert.ToInt32(r["id"]));
        if (r.Table.Columns.Contains("code"))                                         SetIfExists(p, "Code", r["code"]?.ToString());
        if (r.Table.Columns.Contains("name"))                                         SetIfExists(p, "Name", r["name"]?.ToString());
        if (r.Table.Columns.Contains("description"))                                  SetIfExists(p, "Description", r["description"]?.ToString());
        list.Add(p);
    }
    return list;
}

/// <summary>
/// Adds a permission request (user → permission) and returns its id.
/// </summary>
public async Task<int> AddPermissionRequestAsync(int userId, int permissionId, string? reason, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO permission_requests (user_id, permission_id, reason, status, requested_at)
VALUES (@uid,@pid,@reason,'pending',NOW());";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid", userId),
        new MySqlParameter("@pid", permissionId),
        new MySqlParameter("@reason", reason ?? string.Empty)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

/// <summary>
/// Approves a pending permission request, grants a direct permission override, and logs the decision.
/// </summary>
public async Task ApprovePermissionRequestAsync(int requestId, int approvedBy, string? comment, DateTime? expiresAt = null, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM permission_requests WHERE id=@id",
        new[] { new MySqlParameter("@id", requestId) }, token).ConfigureAwait(false);
    if (dt.Rows.Count == 0) return;

    int userId       = Convert.ToInt32(dt.Rows[0]["user_id"]);
    int permissionId = Convert.ToInt32(dt.Rows[0]["permission_id"]);

    await ExecuteNonQueryAsync(@"
UPDATE permission_requests
SET status='approved', reviewed_by=@rev, reviewed_at=NOW(), review_comment=@cmt
WHERE id=@id",
        new[]
        {
            new MySqlParameter("@rev", approvedBy),
            new MySqlParameter("@cmt", comment ?? string.Empty),
            new MySqlParameter("@id",  requestId)
        }, token).ConfigureAwait(false);

    // Grant direct override
    await AddUserPermissionAsync(userId, permissionId, approvedBy, expiresAt, allowed: true, reason: $"Approved request #{requestId}: {comment}", token: token)
        .ConfigureAwait(false);
}

/// <summary>
/// Denies a pending permission request and logs the decision.
/// </summary>
public async Task DenyPermissionRequestAsync(int requestId, int deniedBy, string? comment, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM permission_requests WHERE id=@id",
        new[] { new MySqlParameter("@id", requestId) }, token).ConfigureAwait(false);
    if (dt.Rows.Count == 0) return;

    int userId       = Convert.ToInt32(dt.Rows[0]["user_id"]);
    int permissionId = Convert.ToInt32(dt.Rows[0]["permission_id"]);

    await ExecuteNonQueryAsync(@"
UPDATE permission_requests
SET status='denied', reviewed_by=@rev, reviewed_at=NOW(), review_comment=@cmt
WHERE id=@id",
        new[]
        {
            new MySqlParameter("@rev", deniedBy),
            new MySqlParameter("@cmt", comment ?? string.Empty),
            new MySqlParameter("@id",  requestId)
        }, token).ConfigureAwait(false);

    await LogPermissionChangeAsync(
        targetUserId: userId,
        changedBy: deniedBy,
        changeType: "request",
        roleId: null,
        permissionId: permissionId,
        action: "deny",
        reason: comment ?? string.Empty,
        expiresAt: null,
        token: token).ConfigureAwait(false);
}

/* ========================= Back-compat shims (kept) ========================= */

/// <summary>
/// Grants or revokes a single permission override for a user by upserting into <c>user_permissions</c>, and logs the change.
/// </summary>
public async Task SetUserPermissionOverrideAsync(
    int userId,
    int permissionId,
    bool allowed,
    int actorUserId,
    string? reason    = null,
    DateTime? expires = null,
    string? sourceIp  = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    const string upsert = @"
INSERT INTO user_permissions (user_id, permission_id, allowed, reason, granted_by, granted_at, expires_at)
VALUES (@uid,@pid,@allow,@reason,@actor,NOW(),@exp)
ON DUPLICATE KEY UPDATE
    allowed=VALUES(allowed),
    reason =VALUES(reason),
    granted_by=VALUES(granted_by),
    granted_at=VALUES(granted_at),
    expires_at=VALUES(expires_at);";

    await ExecuteNonQueryAsync(upsert, new[]
    {
        new MySqlParameter("@uid",   userId),
        new MySqlParameter("@pid",   permissionId),
        new MySqlParameter("@allow", allowed),
        new MySqlParameter("@reason",reason ?? string.Empty),
        new MySqlParameter("@actor", actorUserId),
        new MySqlParameter("@exp",   (object?)expires ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    await ExecuteNonQueryAsync(@"
INSERT INTO permission_change_log
(user_id, changed_by, change_type, permission_id, action, reason, source_ip, session_id)
VALUES
(@uid,@actor,'permission',@pid,@action,@reason,@ip,@sess)",
        new[]
        {
            new MySqlParameter("@uid",   userId),
            new MySqlParameter("@actor", actorUserId),
            new MySqlParameter("@pid",   permissionId),
            new MySqlParameter("@action", allowed ? "grant" : "deny"),
            new MySqlParameter("@reason", reason ?? string.Empty),
            new MySqlParameter("@ip",     sourceIp ?? string.Empty),
            new MySqlParameter("@sess",   sessionId ?? string.Empty)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "RBAC_OVERRIDE", "user_permissions", "RBAC",
        recordId: null,
        description: $"User {userId}: {(allowed ? "GRANT" : "DENY")} permission #{permissionId}",
        ip: sourceIp,
        severity: "security",
        sessionId: sessionId).ConfigureAwait(false);
}

/// <summary>Legacy name → wraps <see cref="AddPermissionRequestAsync"/>.</summary>
public Task<int> SubmitPermissionRequestAsync(int userId, int permissionId, string? reason, CancellationToken token = default)
    => AddPermissionRequestAsync(userId, permissionId, reason, token);

/// <summary>Legacy combined review → calls approve/deny paths.</summary>
public async Task ReviewPermissionRequestAsync(
    int requestId,
    int reviewerUserId,
    bool approve,
    string? reviewComment = null,
    DateTime? expires     = null,
    CancellationToken token = default)
{
    if (approve)
        await ApprovePermissionRequestAsync(requestId, reviewerUserId, reviewComment, expires, token).ConfigureAwait(false);
    else
        await DenyPermissionRequestAsync(requestId, reviewerUserId, reviewComment, token).ConfigureAwait(false);
}

/// <summary>Legacy delegation insert returning id (kept).</summary>
public async Task<int> DelegatePermissionAsync(
    int fromUserId,
    int toUserId,
    int permissionId,
    DateTime expiresAt,
    int grantedBy,
    string? reason = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO delegated_permissions
(from_user_id, to_user_id, permission_id, expires_at, reason, granted_by)
VALUES (@from,@to,@pid,@exp,@reason,@by)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@from", fromUserId),
        new MySqlParameter("@to",   toUserId),
        new MySqlParameter("@pid",  permissionId),
        new MySqlParameter("@exp",  expiresAt),
        new MySqlParameter("@reason", reason ?? string.Empty),
        new MySqlParameter("@by",   grantedBy)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await ExecuteNonQueryAsync(@"
INSERT INTO permission_change_log
(user_id, changed_by, change_type, permission_id, action, reason)
VALUES (@uid,@actor,'delegation',@pid,'delegate',@reason)",
        new[]
        {
            new MySqlParameter("@uid",   toUserId),
            new MySqlParameter("@actor", grantedBy),
            new MySqlParameter("@pid",   permissionId),
            new MySqlParameter("@reason", reason ?? string.Empty)
        }, token).ConfigureAwait(false);

    return id;
}

#endregion
#region === 09 · SESSIONS & E-SIGNATURES ===================================

/// <summary>
/// Creates a login session row in <c>session_log</c>.
/// </summary>
/// <param name="userId">Logged-in user ID.</param>
/// <param name="sessionToken">Opaque session token issued by the app.</param>
/// <param name="ip">Client IP address (optional).</param>
/// <param name="device">Client device info / user-agent (optional).</param>
/// <param name="mfaSuccess">Indicates whether MFA succeeded.</param>
/// <param name="reason">Optional reason/note.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Newly created <c>session_log.id</c>.</returns>
public async Task<int> InsertSessionLogAsync(
    int userId,
    string sessionToken,
    string? ip = null,
    string? device = null,
    bool mfaSuccess = true,
    string? reason = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO session_log
(user_id, session_token, ip_address, device_info, mfa_success, reason)
VALUES (@uid,@tok,@ip,@dev,@mfa,@reason)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid", userId),
        new MySqlParameter("@tok", sessionToken),
        new MySqlParameter("@ip",  ip ?? string.Empty),
        new MySqlParameter("@dev", device ?? string.Empty),
        new MySqlParameter("@mfa", mfaSuccess),
        new MySqlParameter("@reason", reason ?? string.Empty)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId,
        "LOGIN",
        "session_log",
        "Auth",
        id,
        "User session created",
        ip,
        "security",
        device,
        sessionToken
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Marks a session as terminated (logout or forced) in <c>session_log</c>.
/// </summary>
/// <param name="sessionToken">Session token to terminate.</param>
/// <param name="terminatedBy">User ID who terminated (optional).</param>
/// <param name="ip">IP used at termination (optional).</param>
/// <param name="device">Device info at termination (optional).</param>
/// <param name="token">Cancellation token.</param>
public async Task TerminateSessionAsync(
    string sessionToken,
    int? terminatedBy = null,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
UPDATE session_log
SET logout_time = NOW(), terminated = 1, terminated_by = @by
WHERE session_token = @tok AND logout_time IS NULL",
        new[]
        {
            new MySqlParameter("@by",  (object?)terminatedBy ?? DBNull.Value),
            new MySqlParameter("@tok", sessionToken)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        terminatedBy,
        "LOGOUT",
        "session_log",
        "Auth",
        null,
        "Session terminated",
        ip,
        "security",
        device,
        sessionToken
    ).ConfigureAwait(false);
}

/// <summary>
/// Writes a user login audit row into <c>user_login_audit</c>.
/// </summary>
/// <param name="userId">User ID.</param>
/// <param name="success">Whether login succeeded.</param>
/// <param name="sessionToken">Session token (if any).</param>
/// <param name="ip">Client IP address (optional).</param>
/// <param name="device">Device / user-agent (optional).</param>
/// <param name="reason">Optional reason string.</param>
/// <param name="token">Cancellation token.</param>
public async Task InsertUserLoginAuditAsync(
    int userId,
    bool success,
    string? sessionToken,
    string? ip = null,
    string? device = null,
    string? reason = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
INSERT INTO user_login_audit
(user_id, success, session_token, ip_address, device_info, reason, login_time)
VALUES (@uid,@ok,@tok,@ip,@dev,@reason,NOW())",
        new[]
        {
            new MySqlParameter("@uid", userId),
            new MySqlParameter("@ok",  success),
            new MySqlParameter("@tok", sessionToken ?? string.Empty),
            new MySqlParameter("@ip",  ip ?? string.Empty),
            new MySqlParameter("@dev", device ?? string.Empty),
            new MySqlParameter("@reason", reason ?? string.Empty)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// Adds an e-signature row to <c>digital_signatures</c>.
/// </summary>
/// <param name="tableName">Target table the signature applies to.</param>
/// <param name="recordId">Target table record id.</param>
/// <param name="userId">User who signed.</param>
/// <param name="signatureHash">Precomputed digital signature hash.</param>
/// <param name="method">Signature method (e.g., <c>pin</c>, <c>cert</c>).</param>
/// <param name="status">Signature status (default: <c>valid</c>).</param>
/// <param name="ip">Source IP (optional).</param>
/// <param name="device">Device info (optional).</param>
/// <param name="note">Optional short note.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Newly created <c>digital_signatures.id</c>.</returns>
public async Task<int> AddDigitalSignatureAsync(
    string tableName,
    int recordId,
    int userId,
    string signatureHash,
    string method = "pin",
    string status = "valid",
    string? ip = null,
    string? device = null,
    string? note = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO digital_signatures
(table_name, record_id, user_id, signature_hash, method, status, ip_address, device_info, note)
VALUES (@tbl,@rid,@uid,@hash,@method,@status,@ip,@dev,@note)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@tbl",   tableName),
        new MySqlParameter("@rid",   recordId),
        new MySqlParameter("@uid",   userId),
        new MySqlParameter("@hash",  signatureHash),
        new MySqlParameter("@method",method),
        new MySqlParameter("@status",status),
        new MySqlParameter("@ip",    ip ?? string.Empty),
        new MySqlParameter("@dev",   device ?? string.Empty),
        new MySqlParameter("@note",  note ?? string.Empty)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId,
        "SIGN",
        "digital_signatures",
        "Signature",
        id,
        $"Signed {tableName}#{recordId}",
        ip,
        "audit",
        device,
        null
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Checks whether a given session token is still active (exists, not terminated, no logout time).
/// </summary>
/// <param name="sessionToken">Session token to check.</param>
/// <param name="token">Cancellation token.</param>
/// <returns><c>true</c> if active; otherwise <c>false</c>.</returns>
public async Task<bool> IsSessionActiveAsync(string sessionToken, CancellationToken token = default)
{
    const string sql = @"
SELECT COUNT(*) FROM session_log
WHERE session_token = @tok AND terminated = 0 AND logout_time IS NULL";

    var cnt = await ExecuteScalarAsync(sql, new[] { new MySqlParameter("@tok", sessionToken) }, token)
        .ConfigureAwait(false);

    return Convert.ToInt32(cnt) > 0;
}

/// <summary>
/// Gets the user id who owns a given session token.
/// </summary>
/// <param name="sessionToken">Session token.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>User id if found; otherwise <c>null</c>.</returns>
public async Task<int?> GetSessionUserIdAsync(string sessionToken, CancellationToken token = default)
{
    const string sql = @"SELECT user_id FROM session_log WHERE session_token = @tok LIMIT 1";
    var obj = await ExecuteScalarAsync(sql, new[] { new MySqlParameter("@tok", sessionToken) }, token)
        .ConfigureAwait(false);

    if (obj == null || obj == DBNull.Value) return null;
    return Convert.ToInt32(obj);
}

/// <summary>
/// Invalidates all active sessions for a user (bulk logout).
/// </summary>
/// <param name="userId">User id.</param>
/// <param name="reason">Optional reason recorded in <c>user_login_audit</c>.</param>
/// <param name="ip">IP used by the action.</param>
/// <param name="device">Device info.</param>
/// <param name="token">Cancellation token.</param>
public async Task InvalidateAllUserSessionsAsync(
    int userId,
    string? reason = "Bulk invalidate",
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
UPDATE session_log
SET logout_time = NOW(), terminated = 1, terminated_by = @uid
WHERE user_id = @uid AND logout_time IS NULL",
        new[]
        {
            new MySqlParameter("@uid", userId)
        }, token).ConfigureAwait(false);

    // Optional audit line (non-blocking design choice to keep it symmetric)
    await ExecuteNonQueryAsync(@"
INSERT INTO user_login_audit (user_id, success, session_token, ip_address, device_info, reason, login_time)
VALUES (@uid, 0, '', @ip, @dev, @reason, NOW())",
        new[]
        {
            new MySqlParameter("@uid", userId),
            new MySqlParameter("@ip",  ip    ?? string.Empty),
            new MySqlParameter("@dev", device?? string.Empty),
            new MySqlParameter("@reason", reason ?? string.Empty)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// Verifies a digital signature by comparing an existing row in <c>digital_signatures</c>.
/// </summary>
/// <param name="tableName">Target table name.</param>
/// <param name="recordId">Target record id.</param>
/// <param name="userId">User id who signed.</param>
/// <param name="signatureHash">Expected signature hash.</param>
/// <param name="requireValidStatus">
/// If <c>true</c>, requires <c>status = 'valid'</c>; otherwise any status matches.
/// </param>
/// <param name="token">Cancellation token.</param>
/// <returns><c>true</c> if a matching signature row exists; otherwise <c>false</c>.</returns>
public async Task<bool> VerifyDigitalSignatureAsync(
    string tableName,
    int recordId,
    int userId,
    string signatureHash,
    bool requireValidStatus = true,
    CancellationToken token = default)
{
    string sql = @"
SELECT COUNT(*) FROM digital_signatures
WHERE table_name = @tbl AND record_id = @rid AND user_id = @uid AND signature_hash = @hash";

    if (requireValidStatus)
        sql += " AND status = 'valid'";

    var cnt = await ExecuteScalarAsync(sql, new[]
    {
        new MySqlParameter("@tbl",  tableName),
        new MySqlParameter("@rid",  recordId),
        new MySqlParameter("@uid",  userId),
        new MySqlParameter("@hash", signatureHash)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(cnt) > 0;
}

/// <summary>
/// Revokes a digital signature by setting <c>status = 'revoked'</c>.
/// </summary>
/// <param name="signatureId">Signature row id.</param>
/// <param name="actorUserId">User performing the revoke action.</param>
/// <param name="ip">IP recorded in system log.</param>
/// <param name="device">Device info recorded in system log.</param>
/// <param name="token">Cancellation token.</param>
public async Task RevokeDigitalSignatureAsync(
    int signatureId,
    int actorUserId,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE digital_signatures SET status = 'revoked' WHERE id = @id",
        new[] { new MySqlParameter("@id", signatureId) }, token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        actorUserId,
        "REVOKE_SIGN",
        "digital_signatures",
        "Signature",
        signatureId,
        "Digital signature revoked",
        ip,
        "audit",
        device,
        null
    ).ConfigureAwait(false);
}

/// <summary>
/// Returns how many signatures exist for a given table/record (optionally filtered to <c>valid</c>).
/// </summary>
/// <param name="tableName">Target table.</param>
/// <param name="recordId">Target record id.</param>
/// <param name="onlyValid">If <c>true</c>, counts only signatures with <c>status = 'valid'</c>.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Count of matching signature rows.</returns>
public async Task<int> GetSignatureCountAsync(
    string tableName,
    int recordId,
    bool onlyValid = true,
    CancellationToken token = default)
{
    string sql = @"
SELECT COUNT(*) FROM digital_signatures
WHERE table_name = @tbl AND record_id = @rid";

    if (onlyValid)
        sql += " AND status = 'valid'";

    var cnt = await ExecuteScalarAsync(sql, new[]
    {
        new MySqlParameter("@tbl", tableName),
        new MySqlParameter("@rid", recordId)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(cnt);
}

#endregion
#region === 10 · SUPPLIERS & CONTRACTORS ====================================

/// <summary>
/// Inserts or updates a supplier in the <c>suppliers</c> table (schema-tolerant: only uses safe/dynamic accessors).
/// </summary>
/// <param name="s">Supplier model (can be a lightweight/DTO; values are read via <c>TryGet*</c> helpers).</param>
/// <param name="update">When <c>true</c>, runs UPDATE; otherwise INSERT.</param>
/// <param name="actorUserId">User id performing the operation (for audit log).</param>
/// <param name="ip">Source IP for audit.</param>
/// <param name="device">Device info for audit.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Inserted/updated supplier id.</returns>
public async Task<int> InsertOrUpdateSupplierAsync(
    Supplier s,
    bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO suppliers
           (name, code, oib, contact, email, phone, address, type, status,
            contract_start, contract_end, cert_doc, valid_until, risk_score,
            last_audit, comment, digital_signature)
           VALUES
           (@name,@code,@oib,@contact,@email,@phone,@addr,@type,@status,
            @cstart,@cend,@cert,@valid,@risk,@laudit,@comment,@sig)"
        : @"UPDATE suppliers SET
           name=@name, code=@code, oib=@oib, contact=@contact, email=@email, phone=@phone, address=@addr,
           type=@type, status=@status, contract_start=@cstart, contract_end=@cend, cert_doc=@cert,
           valid_until=@valid, risk_score=@risk, last_audit=@laudit, comment=@comment, digital_signature=@sig
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@name",   TryGetString(s,"Name") ?? string.Empty),
        new("@code",   (object?)TryGetString(s,"Code") ?? DBNull.Value),
        new("@oib",    (object?)TryGetString(s,"Oib")  ?? DBNull.Value),
        new("@contact",(object?)TryGetString(s,"Contact") ?? DBNull.Value),
        new("@email",  (object?)TryGetString(s,"Email")   ?? DBNull.Value),
        new("@phone",  (object?)TryGetString(s,"Phone")   ?? DBNull.Value),
        new("@addr",   (object?)TryGetString(s,"Address") ?? DBNull.Value),

        // NOTE: legacy objects may expose either "Type" or "SupplierType"; TryGetString will handle both if your helper supports fallback.
        // If your TryGetString does not fallback, it returns null and we send DB NULL which is schema-safe.
        new("@type",   (object?)TryGetString(s,"Type")    ?? DBNull.Value),

        new("@status", (object?)TryGetString(s,"Status")  ?? DBNull.Value),
        new("@cstart", (object?)TryGet<DateTime>(s,"ContractStart") ?? DBNull.Value),
        new("@cend",   (object?)TryGet<DateTime>(s,"ContractEnd")   ?? DBNull.Value),
        new("@cert",   (object?)TryGetString(s,"CertDoc") ?? DBNull.Value),
        new("@valid",  (object?)TryGet<DateTime>(s,"ValidUntil")    ?? DBNull.Value),
        new("@risk",   (object?)TryGet<decimal>(s,"RiskScore")      ?? DBNull.Value),
        new("@laudit", (object?)TryGet<DateTime>(s,"LastAudit")     ?? DBNull.Value),
        new("@comment",(object?)TryGetString(s,"Comment") ?? DBNull.Value),
        new("@sig",    (object?)TryGetString(s,"DigitalSignature") ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(s,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(s,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "suppliers", "SupplierModule",
        id, update ? "Supplier updated" : "Supplier created", ip, "audit", device, null).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Backward-compatibility overload for call sites using the named argument <c>isUpdate:</c>.
/// The differing signature order avoids CS0111 while allowing <c>isUpdate:</c> to bind by name.
/// </summary>
public Task<int> InsertOrUpdateSupplierAsync(
    Supplier s,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default,
    bool isUpdate = false)
    => InsertOrUpdateSupplierAsync(s, update: isUpdate, actorUserId, ip, device, token);

/// <summary>Convenience shim: create supplier.</summary>
public Task<int> AddSupplierAsync(
    Supplier s,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
    => InsertOrUpdateSupplierAsync(s, update: false, actorUserId, ip, device, token);

/// <summary>Convenience shim: update supplier.</summary>
public Task<int> UpdateSupplierAsync(
    Supplier s,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
    => InsertOrUpdateSupplierAsync(s, update: true, actorUserId, ip, device, token);

/// <summary>Delete supplier by id.</summary>
public async Task DeleteSupplierAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM suppliers WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "DELETE", "suppliers", "SupplierModule",
        id, "Supplier deleted", ip, "audit", device, null).ConfigureAwait(false);
}

/// <summary>
/// Compatibility delete overloads used by older code paths (accepts a Supplier or omits user/device).
/// </summary>
public Task DeleteSupplierAsync(Supplier supplier, CancellationToken token = default)
    => DeleteSupplierAsync(TryGet<int>(supplier, "Id") ?? 0, 1, null, null, token);

public Task DeleteSupplierAsync(int id, CancellationToken token)
    => DeleteSupplierAsync(id, 1, null, null, token);

public Task DeleteSupplierAsync(Supplier supplier, string ip, string device, CancellationToken token = default)
    => DeleteSupplierAsync(TryGet<int>(supplier, "Id") ?? 0, 1, ip, device, token);

/// <summary>Rollback supplier – audit only (no versioning in DB).</summary>
public async Task RollbackSupplierAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    await LogSystemEventAsync(actorUserId, "ROLLBACK", "suppliers", "SupplierModule",
        id, "Supplier rollback requested", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>Log supplier audit entry (shim for ViewModels).</summary>
public Task LogSupplierAuditAsync(
    int supplierId,
    string action,
    int actorUserId = 1,
    string? description = null,
    string? ip = null,
    string? device = null,
    string? sessionId = null)
    => LogSystemEventAsync(actorUserId, action, "suppliers", "SupplierModule",
        supplierId, description ?? string.Empty, ip, "audit", device, sessionId);

/// <summary>Export suppliers – audit/log only, returns the file path.</summary>
public async Task<string> ExportSuppliersAsync(
    IEnumerable<Supplier> rows,
    string format = "csv",
    int actorUserId = 1,
    string ip = "system",
    string device = "server",
    string? sessionId = null,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format;
    string filePath = $"/export/suppliers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await LogSystemEventAsync(actorUserId, "EXPORT", "suppliers", "SupplierModule",
        null, $"Exported {(rows?.Count() ?? 0)} suppliers to {filePath}.", ip, "audit", device, sessionId).ConfigureAwait(false);

    return filePath;
}

/// <summary>Get supplier by id (schema tolerant).</summary>
public async Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM suppliers WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    if (dt.Rows.Count == 0) return null;
    return ParseSupplier(dt.Rows[0]);
}

/// <summary>Returns all suppliers ordered by name (schema tolerant).</summary>
public async Task<List<Supplier>> GetAllSuppliersAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM suppliers ORDER BY name",
        null, token).ConfigureAwait(false);

    var list = new List<Supplier>();
    foreach (DataRow r in dt.Rows) list.Add(ParseSupplier(r));
    return list;
}

/// <summary>Legacy shim for callers expecting *Full* variant.</summary>
public Task<List<Supplier>> GetAllSuppliersFullAsync(CancellationToken token = default)
    => GetAllSuppliersAsync(token);

/// <summary>Returns contractors (external_contractors) — schema tolerant.</summary>
public async Task<List<ExternalContractor>> GetAllExternalContractorsUltraAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM external_contractors", null, token).ConfigureAwait(false);
    var list = new List<ExternalContractor>();
    foreach (DataRow r in dt.Rows) list.Add(ParseExternalContractor(r));
    return list;
}

/// <summary>Unified "normal" name.</summary>
public Task<List<ExternalContractor>> GetAllExternalContractorsAsync(CancellationToken token = default)
    => GetAllExternalContractorsUltraAsync(token);

/// <summary>Alias for code using 'Servicer' naming.</summary>
public Task<List<ExternalContractor>> GetAllExternalServicersAsync(CancellationToken token = default)
    => GetAllExternalContractorsUltraAsync(token);

/// <summary>Create or update an external contractor (schema tolerant).</summary>
public async Task<int> InsertOrUpdateExternalContractorUltraAsync(
    ExternalContractor c,
    bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO external_contractors (name, service_type, contact, phone, email, doc_file)
            VALUES (@name,@stype,@contact,@phone,@email,@doc)"
        : @"UPDATE external_contractors
            SET name=@name, service_type=@stype, contact=@contact, phone=@phone, email=@email, doc_file=@doc
            WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@name",   TryGetString(c,"Name") ?? string.Empty),
        new("@stype",  (object?)TryGetString(c,"ServiceType") ?? DBNull.Value),
        new("@contact",(object?)TryGetString(c,"Contact") ?? DBNull.Value),
        new("@phone",  (object?)TryGetString(c,"Phone") ?? DBNull.Value),
        new("@email",  (object?)TryGetString(c,"Email") ?? DBNull.Value),
        new("@doc",    (object?)TryGetString(c,"DocFile") ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(c,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(c,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "external_contractors", "ContractorModule",
        id, update ? "Contractor updated" : "Contractor created", ip, "audit", device, null).ConfigureAwait(false);

    return id;
}

/// <summary>Shim: add contractor.</summary>
public Task<int> AddExternalContractorAsync(ExternalContractor c, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdateExternalContractorUltraAsync(c, false, actorUserId, ip, device, token);

/// <summary>Shim: update contractor.</summary>
public Task<int> UpdateExternalContractorAsync(ExternalContractor c, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdateExternalContractorUltraAsync(c, true, actorUserId, ip, device, token);

/// <summary>Servicer aliases (same table/semantics).</summary>
public Task<int> InsertOrUpdateExternalServicerAsync(ExternalContractor c, bool update, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdateExternalContractorUltraAsync(c, update, actorUserId, ip, device, token);
public Task<int> AddExternalServicerAsync(ExternalContractor c, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdateExternalContractorUltraAsync(c, false, actorUserId, ip, device, token);
public Task<int> UpdateExternalServicerAsync(ExternalContractor c, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdateExternalContractorUltraAsync(c, true, actorUserId, ip, device, token);

/// <summary>Delete contractor.</summary>
public async Task DeleteExternalContractorAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM external_contractors WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "DELETE", "external_contractors", "ContractorModule",
        id, "Contractor deleted", ip, "audit", device, null).ConfigureAwait(false);
}

/// <summary>Alias for 'Servicer'.</summary>
public Task DeleteExternalServicerAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => DeleteExternalContractorAsync(id, actorUserId, ip, device, token);

/// <summary>Rollback contractor – audit only.</summary>
public async Task RollbackExternalContractorAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
{
    await LogSystemEventAsync(actorUserId, "ROLLBACK", "external_contractors", "ContractorModule",
        id, "Contractor rollback requested", ip, "audit", device, sessionId).ConfigureAwait(false);
}
public Task RollbackExternalServicerAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
    => RollbackExternalContractorAsync(id, actorUserId, ip, device, sessionId, token);

/// <summary>Get contractor by id (schema tolerant).</summary>
public async Task<ExternalContractor?> GetExternalContractorByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM external_contractors WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    if (dt.Rows.Count == 0) return null;
    return ParseExternalContractor(dt.Rows[0]);
}

/// <summary>Export contractors – audit/log only, return file path.</summary>
public async Task<string> ExportExternalContractorsAsync(IEnumerable<ExternalContractor> rows, string format = "csv", int actorUserId = 1, string ip = "system", string device = "server", string? sessionId = null, CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format;
    string filePath = $"/export/external_contractors_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await LogSystemEventAsync(actorUserId, "EXPORT", "external_contractors", "ContractorModule",
        null, $"Exported {(rows?.Count() ?? 0)} contractors to {filePath}.", ip, "audit", device, sessionId).ConfigureAwait(false);

    return filePath;
}

/// <summary>Log contractor audit entry.</summary>
public Task LogContractorAuditAsync(int contractorId, string action, int actorUserId = 1, string? description = null, string? ip = null, string? device = null, string? sessionId = null)
    => LogSystemEventAsync(actorUserId, action, "external_contractors", "ContractorModule", contractorId, description ?? string.Empty, ip, "audit", device, sessionId);

/// <summary>
/// Inserts or updates a contractor intervention in <c>contractor_interventions</c>.
/// </summary>
public async Task<int> InsertOrUpdateContractorInterventionUltraAsync(
    ContractorIntervention i,
    bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO contractor_interventions
           (contractor_id, component_id, intervention_date, reason, result, gmp_compliance, doc_file)
           VALUES (@cid,@comp,@date,@reason,@result,@gmp,@doc)"
        : @"UPDATE contractor_interventions SET
           contractor_id=@cid, component_id=@comp, intervention_date=@date, reason=@reason,
           result=@result, gmp_compliance=@gmp, doc_file=@doc
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@cid",   (object?)TryGet<int>(i,"ContractorId")     ?? DBNull.Value),
        new("@comp",  (object?)TryGet<int>(i,"ComponentId")      ?? DBNull.Value),
        new("@date",  (object?)TryGet<DateTime>(i,"InterventionDate") ?? DBNull.Value),
        new("@reason",(object?)TryGetString(i,"Reason")          ?? DBNull.Value),
        new("@result",(object?)TryGetString(i,"Result")          ?? DBNull.Value),
        new("@gmp",   (object?)TryGet<bool>(i,"GmpCompliance")   ?? DBNull.Value),
        new("@doc",   (object?)TryGetString(i,"DocFile")         ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(i,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(i,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "contractor_interventions", "ContractorModule",
        id, update ? "Intervention updated" : "Intervention created", ip, "audit", device, null).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Compatibility method expected by legacy ViewModels: canonical name (…Intervention<strong>Async</strong>) and both update/isUpdate styles.
/// </summary>
public Task<int> InsertOrUpdateContractorInterventionAsync(
    ContractorIntervention i,
    bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
    => InsertOrUpdateContractorInterventionUltraAsync(i, update, actorUserId, ip, device, token);

/// <summary>Overload that accepts the named argument <c>isUpdate:</c> (placed last to avoid CS0111).</summary>
public Task<int> InsertOrUpdateContractorInterventionAsync(
    ContractorIntervention i,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default,
    bool isUpdate = false)
    => InsertOrUpdateContractorInterventionUltraAsync(i, isUpdate, actorUserId, ip, device, token);

/// <summary>Delete contractor intervention (full audit).</summary>
public async Task DeleteContractorInterventionAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM contractor_interventions WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "DELETE", "contractor_interventions", "ContractorModule",
        id, "Contractor intervention deleted", ip, "audit", device, null).ConfigureAwait(false);
}

/// <summary>Compatibility deletes used in some older pages.</summary>
public Task DeleteContractorInterventionAsync(int id, CancellationToken token = default)
    => DeleteContractorInterventionAsync(id, 1, null, null, token);

public Task DeleteContractorInterventionAsync(int id, string ip, string device, CancellationToken token = default)
    => DeleteContractorInterventionAsync(id, 1, ip, device, token);

/// <summary>Rollback contractor intervention – audit only (no versioning here).</summary>
public Task RollbackContractorInterventionAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    return LogSystemEventAsync(actorUserId, "ROLLBACK", "contractor_interventions", "ContractorModule",
        id, "Intervention rollback requested", ip, "audit", device, sessionId);
}

/// <summary>Audit helper for contractor interventions.</summary>
public Task LogContractorInterventionAuditAsync(
    int interventionId,
    string action,
    int actorUserId = 1,
    string? description = null,
    string? ip = null,
    string? device = null,
    string? sessionId = null)
    => LogSystemEventAsync(actorUserId, action, "contractor_interventions", "ContractorModule",
        interventionId, description ?? string.Empty, ip, "audit", device, sessionId);

/* ----------------------- Parsers (schema tolerant) ----------------------- */

private static Supplier ParseSupplier(DataRow r)
{
    var s = new Supplier();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)            SetIfExists(s, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("name"))                                      SetIfExists(s, "Name", r["name"]?.ToString());
    if (r.Table.Columns.Contains("code"))                                      SetIfExists(s, "Code", r["code"]?.ToString());
    if (r.Table.Columns.Contains("oib"))                                       SetIfExists(s, "Oib", r["oib"]?.ToString());
    if (r.Table.Columns.Contains("contact"))                                   SetIfExists(s, "Contact", r["contact"]?.ToString());
    if (r.Table.Columns.Contains("email"))                                     SetIfExists(s, "Email", r["email"]?.ToString());
    if (r.Table.Columns.Contains("phone"))                                     SetIfExists(s, "Phone", r["phone"]?.ToString());
    if (r.Table.Columns.Contains("address"))                                   SetIfExists(s, "Address", r["address"]?.ToString());
    if (r.Table.Columns.Contains("type"))                                      SetIfExists(s, "Type", r["type"]?.ToString());
    if (r.Table.Columns.Contains("status"))                                    SetIfExists(s, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("contract_start") && r["contract_start"] != DBNull.Value) SetIfExists(s, "ContractStart", Convert.ToDateTime(r["contract_start"]));
    if (r.Table.Columns.Contains("contract_end")   && r["contract_end"]   != DBNull.Value) SetIfExists(s, "ContractEnd",   Convert.ToDateTime(r["contract_end"]));
    if (r.Table.Columns.Contains("cert_doc"))                                   SetIfExists(s, "CertDoc", r["cert_doc"]?.ToString());
    if (r.Table.Columns.Contains("valid_until")    && r["valid_until"]    != DBNull.Value) SetIfExists(s, "ValidUntil", Convert.ToDateTime(r["valid_until"]));
    if (r.Table.Columns.Contains("risk_score")     && r["risk_score"]     != DBNull.Value) SetIfExists(s, "RiskScore",  Convert.ToDecimal(r["risk_score"]));
    if (r.Table.Columns.Contains("last_audit")     && r["last_audit"]     != DBNull.Value) SetIfExists(s, "LastAudit",  Convert.ToDateTime(r["last_audit"]));
    if (r.Table.Columns.Contains("comment"))                                     SetIfExists(s, "Comment", r["comment"]?.ToString());
    if (r.Table.Columns.Contains("digital_signature"))                          SetIfExists(s, "DigitalSignature", r["digital_signature"]?.ToString());

    return s;
}

private static ExternalContractor ParseExternalContractor(DataRow r)
{
    var c = new ExternalContractor();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)               SetIfExists(c, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("name"))                                        SetIfExists(c, "Name", r["name"]?.ToString());
    if (r.Table.Columns.Contains("service_type"))                                SetIfExists(c, "ServiceType", r["service_type"]?.ToString());
    if (r.Table.Columns.Contains("contact"))                                     SetIfExists(c, "Contact", r["contact"]?.ToString());
    if (r.Table.Columns.Contains("phone"))                                       SetIfExists(c, "Phone", r["phone"]?.ToString());
    if (r.Table.Columns.Contains("email"))                                       SetIfExists(c, "Email", r["email"]?.ToString());
    if (r.Table.Columns.Contains("doc_file"))                                    SetIfExists(c, "DocFile", r["doc_file"]?.ToString());

    return c;
}

#endregion
#region === 11 · PARTS & INVENTORY =========================================

/// <summary>Returns every part.</summary>
/// <param name="token">Cancellation token.</param>
public async Task<List<Part>> GetAllPartsAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM parts ORDER BY name", null, token).ConfigureAwait(false);
    var list = new List<Part>();
    foreach (DataRow r in dt.Rows) list.Add(ParsePart(r));
    return list;
}

/// <summary>Alias for callers expecting a *Full* variant.</summary>
public Task<List<Part>> GetAllPartsFullAsync(CancellationToken token = default)
    => GetAllPartsAsync(token);

/// <summary>Also provide spare-parts aliases (same table).</summary>
public Task<List<Part>> GetAllSparePartsAsync(CancellationToken token = default)
    => GetAllPartsAsync(token);
public Task<List<Part>> GetAllSparePartsFullAsync(CancellationToken token = default)
    => GetAllPartsAsync(token);

/// <summary>Returns a single part by primary key.</summary>
/// <param name="id">Part identifier.</param>
/// <param name="token">Cancellation token.</param>
public async Task<Part?> GetPartByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM parts WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    return dt.Rows.Count == 0 ? null : ParsePart(dt.Rows[0]);
}

/// <summary>Inserts or updates a part row (schema tolerant input).</summary>
public async Task<int> InsertOrUpdatePartAsync(
    Part part,
    bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    if (part is null) throw new ArgumentNullException(nameof(part));

    string sql = !update
        ? @"INSERT INTO parts (code, name, default_supplier_id, description, status)
            VALUES (@code,@name,@dsup,@desc,@status)"
        : @"UPDATE parts
            SET code=@code, name=@name, default_supplier_id=@dsup, description=@desc, status=@status
            WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@code",   TryGetString(part,"Code") ?? string.Empty),
        new("@name",   TryGetString(part,"Name") ?? string.Empty),
        new("@dsup",   (object?)TryGet<int>(part,"DefaultSupplierId") ?? DBNull.Value),
        new("@desc",   (object?)TryGetString(part,"Description") ?? DBNull.Value),
        new("@status", TryGetString(part,"Status") ?? "active")
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(part,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    int id = update
        ? (TryGet<int>(part,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "parts", "Inventory",
        id, update ? "Part updated" : "Part created", ip, "audit", device).ConfigureAwait(false);

    return id;
}

/// <summary>Convenience shim: add part.</summary>
public Task<int> AddPartAsync(Part part, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdatePartAsync(part, update: false, actorUserId, ip, device, token);

/// <summary>Convenience shim: update part.</summary>
public Task<int> UpdatePartAsync(Part part, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => InsertOrUpdatePartAsync(part, update: true, actorUserId, ip, device, token);

/// <summary>Delete a part.</summary>
public async Task DeletePartAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM parts WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "DELETE", "parts", "Inventory", id, "Part deleted", ip, "audit", device).ConfigureAwait(false);
}

/// <summary>Rollback part – audit only (no versioning in DB).</summary>
public async Task RollbackPartAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
{
    await LogSystemEventAsync(actorUserId, "ROLLBACK", "parts", "Inventory",
        id, "Part rollback requested", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>Log part audit entry (shim for ViewModels).</summary>
public Task LogPartAuditAsync(int partId, string action, int actorUserId = 1, string? description = null, string? ip = null, string? device = null, string? sessionId = null)
    => LogSystemEventAsync(actorUserId, action, "parts", "Inventory", partId, description ?? string.Empty, ip, "audit", device, sessionId);

/// <summary>Export parts – audit/log only, return file path.</summary>
public async Task<string> ExportPartsAsync(IEnumerable<Part> rows, string format = "csv", int actorUserId = 1, string ip = "system", string device = "server", string? sessionId = null, CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format;
    string filePath = $"/export/parts_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await LogSystemEventAsync(actorUserId, "EXPORT", "parts", "Inventory",
        null, $"Exported {(rows?.Count() ?? 0)} parts to {filePath}.", ip, "audit", device, sessionId).ConfigureAwait(false);

    return filePath;
}

/* ---------- Spare-part method aliases (some ViewModels use these names) ---------- */

public Task<int> AddSparePartAsync(Part part, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => AddPartAsync(part, actorUserId, ip, device, token);

public Task<int> UpdateSparePartAsync(Part part, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => UpdatePartAsync(part, actorUserId, ip, device, token);

public Task DeleteSparePartAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
    => DeletePartAsync(id, actorUserId, ip, device, token);

public Task RollbackSparePartAsync(int id, int actorUserId = 1, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
    => RollbackPartAsync(id, actorUserId, ip, device, sessionId, token);

public Task LogSparePartAuditAsync(int partId, string action, int actorUserId = 1, string? description = null, string? ip = null, string? device = null, string? sessionId = null)
    => LogPartAuditAsync(partId, action, actorUserId, description, ip, device, sessionId);

public Task<string> ExportSparePartsAsync(IEnumerable<Part> rows, string format = "csv", int actorUserId = 1, string ip = "system", string device = "server", string? sessionId = null, CancellationToken token = default)
    => ExportPartsAsync(rows, format, actorUserId, ip, device, sessionId, token);

/* ---------------------------- Pricing & Warehouses ---------------------------- */

/// <summary>Upserts a supplier price for a part.</summary>
public async Task UpsertPartSupplierPriceAsync(
    PartSupplierPrice price,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO part_supplier_prices (part_id, supplier_id, unit_price, currency, valid_until)
VALUES (@pid,@sid,@price,@cur,@until)
ON DUPLICATE KEY UPDATE
    unit_price=VALUES(unit_price),
    currency  =VALUES(currency),
    valid_until=VALUES(valid_until);";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@pid",   TryGet<int>(price,"PartId") ?? 0),
        new MySqlParameter("@sid",   TryGet<int>(price,"SupplierId") ?? 0),
        new MySqlParameter("@price", (object?)TryGet<decimal>(price,"UnitPrice") ?? DBNull.Value),
        new MySqlParameter("@cur",   TryGetString(price,"Currency") ?? "EUR"),
        new MySqlParameter("@until", (object?)TryGet<DateTime>(price,"ValidUntil") ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "UPSERT", "part_supplier_prices", "Inventory",
        TryGet<int>(price,"PartId") ?? 0,
        $"Price for part #{TryGet<int>(price,"PartId") ?? 0} and supplier #{TryGet<int>(price,"SupplierId") ?? 0} set to {TryGet<decimal>(price,"UnitPrice") ?? 0m} {TryGetString(price,"Currency") ?? "EUR"}",
        ip, "audit", device).ConfigureAwait(false);
}

/// <summary>Returns all prices for a given part.</summary>
public async Task<List<PartSupplierPrice>> GetPricesForPartAsync(int partId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM part_supplier_prices WHERE part_id=@pid",
        new[] { new MySqlParameter("@pid", partId) }, token).ConfigureAwait(false);

    var list = new List<PartSupplierPrice>();
    foreach (DataRow r in dt.Rows) list.Add(ParsePartSupplierPrice(r));
    return list;
}

/// <summary>Returns all warehouses.</summary>
public async Task<List<Warehouse>> GetAllWarehousesAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM warehouses ORDER BY name", null, token).ConfigureAwait(false);
    var list = new List<Warehouse>();
    foreach (DataRow r in dt.Rows) list.Add(ParseWarehouse(r));
    return list;
}

/// <summary>Alias for callers expecting *Full* variant.</summary>
public Task<List<Warehouse>> GetAllWarehousesFullAsync(CancellationToken token = default)
    => GetAllWarehousesAsync(token);

/// <summary>Inserts or updates a warehouse.</summary>
public async Task<int> InsertOrUpdateWarehouseAsync(
    Warehouse wh, bool update, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO warehouses (name, location, responsible_id)
            VALUES (@name,@loc,@resp)"
        : @"UPDATE warehouses
            SET name=@name, location=@loc, responsible_id=@resp
            WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@name", TryGetString(wh,"Name") ?? string.Empty),
        new("@loc",  (object?)TryGetString(wh,"Location") ?? DBNull.Value),
        new("@resp", (object?)TryGet<int>(wh,"ResponsibleId") ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(wh,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    int id = update
        ? (TryGet<int>(wh,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "warehouses", "Inventory", id,
        update ? "Warehouse updated" : "Warehouse created", ip, "audit", device).ConfigureAwait(false);

    return id;
}

/// <summary>Returns stock rows for a given part across all warehouses.</summary>
public async Task<List<StockLevel>> GetStockForPartAsync(int partId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM stock_levels WHERE part_id=@pid",
        new[] { new MySqlParameter("@pid", partId) }, token).ConfigureAwait(false);

    var list = new List<StockLevel>();
    foreach (DataRow r in dt.Rows) list.Add(ParseStockLevel(r));
    return list;
}

/// <summary>Upserts a stock level row for (part, warehouse).</summary>
public async Task SetStockLevelAsync(
    int partId, int warehouseId, int quantity, int? minThreshold, int? maxThreshold,
    int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO stock_levels (part_id, warehouse_id, quantity, min_threshold, max_threshold)
VALUES (@pid,@wid,@qty,@min,@max)
ON DUPLICATE KEY UPDATE
    quantity=VALUES(quantity),
    min_threshold=VALUES(min_threshold),
    max_threshold=VALUES(max_threshold);";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@pid", partId),
        new MySqlParameter("@wid", warehouseId),
        new MySqlParameter("@qty", quantity),
        new MySqlParameter("@min", (object?)minThreshold ?? DBNull.Value),
        new MySqlParameter("@max", (object?)maxThreshold ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "UPSERT", "stock_levels", "Inventory", null,
        $"Stock set: Part#{partId} @ WH#{warehouseId} = {quantity}", ip, "audit", device).ConfigureAwait(false);
}

/// <summary>
/// Adjusts stock in a single warehouse and records an <c>inventory_transactions</c> row.
/// </summary>
/// <param name="partId">Part identifier.</param>
/// <param name="warehouseId">Warehouse identifier.</param>
/// <param name="deltaQuantity">Signed change in quantity (positive for in, negative for out).</param>
/// <param name="type">in/out/transfer/adjust/damage/obsolete.</param>
/// <param name="performedBy">User id who performed the action.</param>
/// <param name="relatedDocument">Optional related document code/id.</param>
/// <param name="note">Optional note.</param>
/// <param name="token">Cancellation token.</param>
public async Task AdjustStockLevelAsync(
    int partId,
    int warehouseId,
    int deltaQuantity,
    string type,
    int performedBy,
    string? relatedDocument = null,
    string? note = null,
    CancellationToken token = default)
{
    // FIX (CS8620): The tuple’s second item is declared with a nullable array type to match
    // ExecuteTransactionAsync(IEnumerable<(string Sql, MySqlParameter[]? Pars)> ...).
    var commands = new List<(string Sql, MySqlParameter[]? Pars)>();

    // Upsert stock_levels
    commands.Add((
        @"
INSERT INTO stock_levels (part_id, warehouse_id, quantity)
VALUES (@pid,@wid,@qty)
ON DUPLICATE KEY UPDATE quantity = quantity + @qty;",
        new[]
        {
            new MySqlParameter("@pid", partId),
            new MySqlParameter("@wid", warehouseId),
            new MySqlParameter("@qty", deltaQuantity)
        }));

    // Insert inventory_transactions
    commands.Add((
        @"
INSERT INTO inventory_transactions
(part_id, warehouse_id, transaction_type, quantity, performed_by, related_document, note)
VALUES (@pid,@wid,@type,@qty,@uid,@doc,@note);",
        new[]
        {
            new MySqlParameter("@pid",  partId),
            new MySqlParameter("@wid",  warehouseId),
            new MySqlParameter("@type", type),
            new MySqlParameter("@qty",  Math.Abs(deltaQuantity)),
            new MySqlParameter("@uid",  performedBy),
            new MySqlParameter("@doc",  relatedDocument ?? (object)DBNull.Value),
            new MySqlParameter("@note", note ?? (object)DBNull.Value)
        }));

    await ExecuteTransactionAsync(commands, token).ConfigureAwait(false);
}

/// <summary>
/// Transfers stock between two warehouses (creates two transaction rows).
/// </summary>
/// <param name="partId">Part identifier.</param>
/// <param name="fromWarehouseId">Source warehouse id.</param>
/// <param name="toWarehouseId">Destination warehouse id.</param>
/// <param name="quantity">Quantity to transfer (must be &gt; 0).</param>
/// <param name="performedBy">User id who performed the action.</param>
/// <param name="relatedDocument">Optional related document code/id.</param>
/// <param name="note">Optional note.</param>
/// <param name="token">Cancellation token.</param>
public async Task MoveStockAsync(
    int partId,
    int fromWarehouseId,
    int toWarehouseId,
    int quantity,
    int performedBy,
    string? relatedDocument = null,
    string? note = null,
    CancellationToken token = default)
{
    if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

    // FIX (CS8620): match nullable array element in the tuple type.
    var cmds = new List<(string Sql, MySqlParameter[]? Pars)>();

    // Decrease source
    cmds.Add((
        @"
INSERT INTO stock_levels (part_id, warehouse_id, quantity)
VALUES (@pid,@wid,@qty)
ON DUPLICATE KEY UPDATE quantity = quantity + @qty;",
        new[]
        {
            new MySqlParameter("@pid", partId),
            new MySqlParameter("@wid", fromWarehouseId),
            new MySqlParameter("@qty", -quantity)
        }));

    // Increase destination
    cmds.Add((
        @"
INSERT INTO stock_levels (part_id, warehouse_id, quantity)
VALUES (@pid,@wid,@qty)
ON DUPLICATE KEY UPDATE quantity = quantity + @qty;",
        new[]
        {
            new MySqlParameter("@pid", partId),
            new MySqlParameter("@wid", toWarehouseId),
            new MySqlParameter("@qty", quantity)
        }));

    // Two inventory transactions
    cmds.Add((
        @"
INSERT INTO inventory_transactions
(part_id, warehouse_id, transaction_type, quantity, performed_by, related_document, note)
VALUES (@pid,@wid,'transfer',@qty,@uid,@doc,@note);",
        new[]
        {
            new MySqlParameter("@pid", partId),
            new MySqlParameter("@wid", fromWarehouseId),
            new MySqlParameter("@qty", quantity),
            new MySqlParameter("@uid", performedBy),
            new MySqlParameter("@doc", relatedDocument ?? (object)DBNull.Value),
            new MySqlParameter("@note", note ?? (object)DBNull.Value)
        }));
    cmds.Add((
        @"
INSERT INTO inventory_transactions
(part_id, warehouse_id, transaction_type, quantity, performed_by, related_document, note)
VALUES (@pid,@wid,'in',@qty,@uid,@doc,@note);",
        new[]
        {
            new MySqlParameter("@pid", partId),
            new MySqlParameter("@wid", toWarehouseId),
            new MySqlParameter("@qty", quantity),
            new MySqlParameter("@uid", performedBy),
            new MySqlParameter("@doc", relatedDocument ?? (object)DBNull.Value),
            new MySqlParameter("@note", note ?? (object)DBNull.Value)
        }));

    await ExecuteTransactionAsync(cmds, token).ConfigureAwait(false);
}

/// <summary>Adds or updates a work-order part mapping.</summary>
public async Task AssignPartToWorkOrderAsync(
    int workOrderId, int partId, int quantity, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO work_order_parts (work_order_id, part_id, quantity)
VALUES (@wo,@part,@qty)
ON DUPLICATE KEY UPDATE quantity = VALUES(quantity);";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@wo",   workOrderId),
        new MySqlParameter("@part", partId),
        new MySqlParameter("@qty",  quantity)
    }, token).ConfigureAwait(false);
}

/// <summary>Removes a part mapping from a work-order.</summary>
public async Task RemovePartFromWorkOrderAsync(
    int workOrderId, int partId, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM work_order_parts WHERE work_order_id=@wo AND part_id=@pid",
        new[]
        {
            new MySqlParameter("@wo",  workOrderId),
            new MySqlParameter("@pid", partId)
        }, token).ConfigureAwait(false);
}

/// <summary>Returns all parts assigned to a given work-order.</summary>
public async Task<List<WorkOrderPart>> GetPartsForWorkOrderAsync(
    int workOrderId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM work_order_parts WHERE work_order_id=@wo",
        new[] { new MySqlParameter("@wo", workOrderId) }, token).ConfigureAwait(false);

    var list = new List<WorkOrderPart>();
    foreach (DataRow r in dt.Rows)
    {
        var wop = Activator.CreateInstance<WorkOrderPart>();
        if (r.Table.Columns.Contains("work_order_id")) SetIfExists(wop, "WorkOrderId", Convert.ToInt32(r["work_order_id"]));
        if (r.Table.Columns.Contains("part_id"))       SetIfExists(wop, "PartId",      Convert.ToInt32(r["part_id"]));
        if (r.Table.Columns.Contains("quantity") && r["quantity"] != DBNull.Value)
                                                      SetIfExists(wop, "Quantity",     Convert.ToInt32(r["quantity"]));
        list.Add(wop);
    }
    return list;
}

/// <summary>Helper: insert an inventory transaction row (without stock mutation).</summary>
public async Task<int> LogInventoryTransactionAsync(InventoryTransaction tx, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO inventory_transactions
(part_id, warehouse_id, transaction_type, quantity, performed_by, related_document, note)
VALUES (@pid,@wid,@type,@qty,@uid,@doc,@note)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@pid",  TryGet<int>(tx,"PartId") ?? 0),
        new MySqlParameter("@wid",  TryGet<int>(tx,"WarehouseId") ?? 0),
        new MySqlParameter("@type", TryGetString(tx,"TransactionType") ?? "adjust"),
        new MySqlParameter("@qty",  TryGet<int>(tx,"Quantity") ?? 0),
        new MySqlParameter("@uid",  TryGet<int>(tx,"PerformedBy") ?? 0),
        new MySqlParameter("@doc",  (object?)TryGetString(tx,"RelatedDocument") ?? DBNull.Value),
        new MySqlParameter("@note", (object?)TryGetString(tx,"Note") ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

/* --------------------------------- Parsers --------------------------------- */

/// <summary>Row → Part (schema tolerant).</summary>
private static Part ParsePart(DataRow r)
{
    var p = Activator.CreateInstance<Part>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                 SetIfExists(p, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("code"))                                           SetIfExists(p, "Code", r["code"]?.ToString());
    if (r.Table.Columns.Contains("name"))                                           SetIfExists(p, "Name", r["name"]?.ToString());
    if (r.Table.Columns.Contains("default_supplier_id") && r["default_supplier_id"] != DBNull.Value)
                                                                                    SetIfExists(p, "DefaultSupplierId", Convert.ToInt32(r["default_supplier_id"]));
    if (r.Table.Columns.Contains("description"))                                    SetIfExists(p, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("status"))                                         SetIfExists(p, "Status", r["status"]?.ToString());
    return p;
}

/// <summary>Row → PartSupplierPrice (schema tolerant).</summary>
private static PartSupplierPrice ParsePartSupplierPrice(DataRow r)
{
    var p = Activator.CreateInstance<PartSupplierPrice>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)             SetIfExists(p, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("part_id") && r["part_id"] != DBNull.Value)   SetIfExists(p, "PartId", Convert.ToInt32(r["part_id"]));
    if (r.Table.Columns.Contains("supplier_id") && r["supplier_id"] != DBNull.Value)
                                                                                SetIfExists(p, "SupplierId", Convert.ToInt32(r["supplier_id"]));
    if (r.Table.Columns.Contains("unit_price") && r["unit_price"] != DBNull.Value)
                                                                                SetIfExists(p, "UnitPrice", Convert.ToDecimal(r["unit_price"]));
    if (r.Table.Columns.Contains("currency"))                                   SetIfExists(p, "Currency", r["currency"]?.ToString());
    if (r.Table.Columns.Contains("valid_until") && r["valid_until"] != DBNull.Value)
                                                                                SetIfExists(p, "ValidUntil", Convert.ToDateTime(r["valid_until"]));
    return p;
}

/// <summary>Row → Warehouse (schema tolerant).</summary>
private static Warehouse ParseWarehouse(DataRow r)
{
    var w = Activator.CreateInstance<Warehouse>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)             SetIfExists(w, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("name"))                                       SetIfExists(w, "Name", r["name"]?.ToString());
    if (r.Table.Columns.Contains("location"))                                   SetIfExists(w, "Location", r["location"]?.ToString());
    if (r.Table.Columns.Contains("responsible_id") && r["responsible_id"] != DBNull.Value)
                                                                                SetIfExists(w, "ResponsibleId", Convert.ToInt32(r["responsible_id"]));
    return w;
}

/// <summary>Row → StockLevel (schema tolerant).</summary>
private static StockLevel ParseStockLevel(DataRow r)
{
    var s = Activator.CreateInstance<StockLevel>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)             SetIfExists(s, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("part_id") && r["part_id"] != DBNull.Value)   SetIfExists(s, "PartId", Convert.ToInt32(r["part_id"]));
    if (r.Table.Columns.Contains("warehouse_id") && r["warehouse_id"] != DBNull.Value)
                                                                                SetIfExists(s, "WarehouseId", Convert.ToInt32(r["warehouse_id"]));
    if (r.Table.Columns.Contains("quantity") && r["quantity"] != DBNull.Value) SetIfExists(s, "Quantity", Convert.ToInt32(r["quantity"]));
    if (r.Table.Columns.Contains("min_threshold") && r["min_threshold"] != DBNull.Value)
                                                                                SetIfExists(s, "MinThreshold", Convert.ToInt32(r["min_threshold"]));
    if (r.Table.Columns.Contains("max_threshold") && r["max_threshold"] != DBNull.Value)
                                                                                SetIfExists(s, "MaxThreshold", Convert.ToInt32(r["max_threshold"]));
    return s;
}

#endregion
#region === 12 · CALIBRATION EXPORT LOG =====================================

/// <summary>
/// Records a calibration export using the stored procedure <c>sp_log_calibration_export</c>
/// and also writes to <c>export_print_log</c> for unified auditing.
/// </summary>
/// <param name="userId">Exporting user.</param>
/// <param name="format">excel / pdf (validated to match DB enum).</param>
/// <param name="componentId">Optional filter component id.</param>
/// <param name="dateFrom">Optional range from.</param>
/// <param name="dateTo">Optional range to.</param>
/// <param name="filePath">Server path of generated file.</param>
/// <param name="token">Cancellation token.</param>
public async Task LogCalibrationExportAsync(
    int userId,
    string format,
    int? componentId,
    DateTime? dateFrom,
    DateTime? dateTo,
    string filePath,
    CancellationToken token = default)
{
    // Validate/normalize inputs
    string fmt = (format ?? "excel").Trim().ToLowerInvariant();
    if (fmt != "excel" && fmt != "pdf") fmt = "excel";
    if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("filePath cannot be empty.", nameof(filePath));

    // Call stored procedure with retry + proper disposal
    await ExecuteWithRetryAsync(async () =>
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(token).ConfigureAwait(false);

        using var cmd = new MySqlCommand("sp_log_calibration_export", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@p_user_id",   userId);
        cmd.Parameters.AddWithValue("@p_format",    fmt);
        cmd.Parameters.AddWithValue("@p_component_id", (object?)componentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p_date_from", (object?)dateFrom ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p_date_to",   (object?)dateTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p_file_path", filePath);

        await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        return 0;
    }).ConfigureAwait(false);

    // Compose a stable filter string for audit trail
    var filterUsed =
        $"componentId={(componentId?.ToString() ?? "")};" +
        $"from={(dateFrom?.ToString("yyyy-MM-dd") ?? "")};" +
        $"to={(dateTo?.ToString("yyyy-MM-dd") ?? "")}";

    // Unified export/print audit row
    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log
(user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'calibrations',@filter,@path,'system','Calibration export')",
        new[]
        {
            new MySqlParameter("@uid",    userId),
            new MySqlParameter("@fmt",    fmt),
            new MySqlParameter("@filter", filterUsed),
            new MySqlParameter("@path",   filePath)
        }, token).ConfigureAwait(false);

    // Also log via central system event logger (if Region 07 is your canonical logger)
    await LogSystemEventAsync(
        userId:      userId,
        eventType:   "EXPORT",
        tableName:   "calibrations",
        module:      "Calibration",
        recordId:    null,
        description: $"Calibration export created: {filePath} ({fmt}) {filterUsed}",
        ip:          "system",
        severity:    "audit",
        deviceInfo:  "server",
        sessionId:   null
    ).ConfigureAwait(false);
}

#endregion
#region === 13 · IoT SENSOR DATA & ANOMALIES =================================

/// <summary>
/// Inserts a single IoT sensor data row. (Schema-tolerant: no strong member refs.)
/// </summary>
public async Task<int> InsertIotSensorDataAsync(
    IotSensorData data, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO iot_sensor_data
(device_id, component_id, data_type, value, unit, timestamp, status, anomaly_detected, processed, note)
VALUES (@dev,@cid,@dtype,@val,@unit,@ts,@status,@anom,@proc,@note)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@dev",   TryGetString(data,"DeviceId") ?? string.Empty),
        new MySqlParameter("@cid",   (object?)(object?)(object?)TryGet<int>(data,"ComponentId") ?? DBNull.Value),
        new MySqlParameter("@dtype", TryGetString(data,"DataType") ?? string.Empty),
        new MySqlParameter("@val",   (object?)(object?)(object?)TryGet<decimal>(data,"Value") ?? DBNull.Value),
        new MySqlParameter("@unit",  TryGetString(data,"Unit") ?? string.Empty),
        new MySqlParameter("@ts",    (object?)TryGet<DateTime>(data,"Timestamp") ?? DateTime.UtcNow),
        new MySqlParameter("@status",TryGetString(data,"Status") ?? "ok"),
        new MySqlParameter("@anom",  (object?)(object?)(object?)TryGet<bool>(data,"AnomalyDetected") ?? DBNull.Value),
        new MySqlParameter("@proc",  (object?)(object?)(object?)TryGet<bool>(data,"Processed") ?? DBNull.Value),
        new MySqlParameter("@note",  (object?)TryGetString(data,"Note") ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

/// <summary>
/// Marks a sensor data row as processed/unprocessed.
/// </summary>
public async Task SetIotSensorDataProcessedAsync(int id, bool processed, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE iot_sensor_data SET processed=@p WHERE id=@id",
        new[]
        {
            new MySqlParameter("@p",  processed),
            new MySqlParameter("@id", id)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// Returns sensor data rows for a component and optional type/range.
/// </summary>
public async Task<List<IotSensorData>> GetSensorDataByComponentAsync(
    int componentId, string? dataType, DateTime? from, DateTime? to, CancellationToken token = default)
{
    var sql = new StringBuilder(@"
SELECT * FROM iot_sensor_data WHERE component_id=@cid");
    var pars = new List<MySqlParameter> { new("@cid", componentId) };

    if (!string.IsNullOrWhiteSpace(dataType))
    {
        sql.Append(" AND data_type=@dt");
        pars.Add(new MySqlParameter("@dt", dataType));
    }
    if (from.HasValue)
    {
        sql.Append(" AND timestamp>=@from");
        pars.Add(new MySqlParameter("@from", from.Value));
    }
    if (to.HasValue)
    {
        sql.Append(" AND timestamp<=@to");
        pars.Add(new MySqlParameter("@to", to.Value));
    }
    sql.Append(" ORDER BY timestamp");

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<IotSensorData>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIotSensorData(r));
    return list;
}

/// <summary>
/// Inserts a row in <c>iot_anomaly_log</c>.
/// </summary>
public async Task<int> InsertIotAnomalyAsync(
    IotAnomaly anomaly, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO iot_anomaly_log
(sensor_data_id, detected_at, detected_by, description, severity, resolved, resolved_at, resolution_note)
VALUES (@sid,@at,@by,@desc,@sev,@res,@resat,@rnote)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@sid",   anomaly.SensorDataId),
        new MySqlParameter("@at",    anomaly.DetectedAt),
        new MySqlParameter("@by",    anomaly.DetectedBy ?? string.Empty),
        new MySqlParameter("@desc",  anomaly.Description ?? string.Empty),
        new MySqlParameter("@sev",   anomaly.Severity ?? "low"),
        new MySqlParameter("@res",   anomaly.Resolved),
        new MySqlParameter("@resat", (object?)anomaly.ResolvedAt ?? DBNull.Value),
        new MySqlParameter("@rnote", anomaly.ResolutionNote ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

/// <summary>
/// Parses a DataRow into <see cref="IotSensorData"/> (schema-tolerant).
/// </summary>
private static IotSensorData ParseIotSensorData(DataRow r)
{
    var s = Activator.CreateInstance<IotSensorData>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(s, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("device_id"))
        SetIfExists(s, "DeviceId", r["device_id"]?.ToString());
    if (r.Table.Columns.Contains("component_id") && r["component_id"] != DBNull.Value)
        SetIfExists(s, "ComponentId", Convert.ToInt32(r["component_id"]));
    if (r.Table.Columns.Contains("data_type"))
        SetIfExists(s, "DataType", r["data_type"]?.ToString());
    if (r.Table.Columns.Contains("value") && r["value"] != DBNull.Value)
        SetIfExists(s, "Value", Convert.ToDecimal(r["value"]));
    if (r.Table.Columns.Contains("unit"))
        SetIfExists(s, "Unit", r["unit"]?.ToString());
    if (r.Table.Columns.Contains("timestamp") && r["timestamp"] != DBNull.Value)
        SetIfExists(s, "Timestamp", Convert.ToDateTime(r["timestamp"]));
    if (r.Table.Columns.Contains("status"))
        SetIfExists(s, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("anomaly_detected") && r["anomaly_detected"] != DBNull.Value)
        SetIfExists(s, "AnomalyDetected", Convert.ToBoolean(r["anomaly_detected"]));
    if (r.Table.Columns.Contains("processed") && r["processed"] != DBNull.Value)
        SetIfExists(s, "Processed", Convert.ToBoolean(r["processed"]));
    if (r.Table.Columns.Contains("note"))
        SetIfExists(s, "Note", r["note"]?.ToString());

    return s;
}

#endregion
#region === 14 · SOP / DOCUMENT CONTROL ====================================

//
// NOTE (mapping):
//   - Model uses VersionNo (int)  -> DB column 'version' (VARCHAR)  : we convert int <-> string.
//   - Model uses ReviewNotes      -> DB column 'notes'              : mapped accordingly.
//   - Model does NOT expose ApprovedBy/ApprovedAt on the POCO; those are handled by
//     dedicated service methods (Approve/Publish/etc.) directly against the DB.
//

/// <summary>
/// Returns all SOP / documents (schema tolerant).
/// </summary>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="SopDocument"/>.</returns>
public async Task<List<SopDocument>> GetAllDocumentsFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM sop_documents ORDER BY name, version DESC", null, token
    ).ConfigureAwait(false);

    var list = new List<SopDocument>();
    foreach (DataRow r in dt.Rows) list.Add(ParseSopDocument(r));
    return list;
}

/// <summary>
/// Returns a single SOP / document by id (schema tolerant).
/// </summary>
/// <param name="id">Document ID.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Document or <c>null</c>.</returns>
public async Task<SopDocument?> GetSopDocumentByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM sop_documents WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token
    ).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseSopDocument(dt.Rows[0]);
}

/// <summary>
/// Inserts or updates an SOP document row (schema-tolerant input mapped to DB).
/// </summary>
/// <param name="doc">Document model (POCO).</param>
/// <param name="update"><c>false</c> for insert, <c>true</c> for update.</param>
/// <param name="actorUserId">User performing the action.</param>
/// <param name="ip">Optional source IP for audit log.</param>
/// <param name="device">Optional device info for audit log.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Inserted/updated document ID.</returns>
public async Task<int> InsertOrUpdateSopDocumentAsync(
    SopDocument doc,
    bool update,
    int actorUserId,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO sop_documents
           (code, name, version, status, created_by, created_at, approved_by, approved_at, file_path, notes)
           VALUES (@code,@name,@ver,@status,@cb,NOW(),@ab,@aa,@path,@notes)"
        : @"UPDATE sop_documents SET
           code=@code, name=@name, version=@ver, status=@status, file_path=@path, notes=@notes
           WHERE id=@id";

    // Map SopDocument -> DB columns
    var versionText = (TryGet<int>(doc, "VersionNo") ?? doc.VersionNo).ToString();
    var reviewNotes = TryGetString(doc, "ReviewNotes") ?? doc.ReviewNotes;

    var pars = new List<MySqlParameter>
    {
        new("@code",   TryGetString(doc, "Code")    ?? doc.Code    ?? string.Empty),
        new("@name",   TryGetString(doc, "Name")    ?? doc.Name    ?? string.Empty),
        new("@ver",    versionText),
        new("@status", TryGetString(doc, "Status")  ?? doc.Status  ?? "draft"),
        new("@cb",     actorUserId),
        new("@ab",     DBNull.Value), // handled via ApproveDocumentAsync
        new("@aa",     DBNull.Value), // handled via ApproveDocumentAsync
        new("@path",   (object?) (TryGetString(doc, "FilePath") ?? doc.FilePath) ?? DBNull.Value),
        new("@notes",  (object?) reviewNotes ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(doc, "Id") ?? doc.Id));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? (TryGet<int>(doc, "Id") ?? doc.Id)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // Local action log
    await ExecuteNonQueryAsync(@"
INSERT INTO sop_document_log (sop_document_id, action, performed_by, note)
VALUES (@id,@act,@by,@note)",
        new[]
        {
            new MySqlParameter("@id",   id),
            new MySqlParameter("@act",  update ? "update" : "create"),
            new MySqlParameter("@by",   actorUserId),
            new MySqlParameter("@note", update ? "SOP updated" : "SOP created")
        }, token).ConfigureAwait(false);

    // Canonical system logger
    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: update ? "UPDATE" : "CREATE",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: id,
        description: update ? "SOP updated" : "SOP created",
        ip: ip,
        severity: "audit",
        deviceInfo: device
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Adds a new document version row.
/// </summary>
/// <param name="relatedTable">Owning table name (e.g., "sop_documents").</param>
/// <param name="relatedId">Owning record ID.</param>
/// <param name="version">Version label (string).</param>
/// <param name="filePath">Path to versioned file.</param>
/// <param name="actorUserId">User performing the action.</param>
/// <param name="note">Optional note.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>New version row ID.</returns>
public async Task<int> AddDocumentVersionAsync(
    string relatedTable,
    int relatedId,
    string version,
    string filePath,
    int actorUserId,
    string? note = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO document_versions
(related_table, related_id, version, file_path, created_by, status, note)
VALUES (@tbl,@rid,@ver,@path,@uid,'active',@note)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@tbl",  relatedTable),
        new MySqlParameter("@rid",  relatedId),
        new MySqlParameter("@ver",  version),
        new MySqlParameter("@path", filePath),
        new MySqlParameter("@uid",  actorUserId),
        new MySqlParameter("@note", (object?)note ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "NEW_VERSION",
        tableName: "document_versions",
        module: "DocControl",
        recordId: id,
        description: $"New version {version} for {relatedTable}#{relatedId}",
        ip: null,
        severity: "audit",
        deviceInfo: null
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Appends a log entry to <c>sop_document_log</c> and the canonical system log.
/// </summary>
/// <param name="sopDocumentId">Document ID.</param>
/// <param name="action">Action label.</param>
/// <param name="performedBy">User performing the action.</param>
/// <param name="note">Optional descriptive note.</param>
/// <param name="token">Cancellation token.</param>
public async Task LogSopActionAsync(
    int sopDocumentId,
    string action,
    int performedBy,
    string? note = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
INSERT INTO sop_document_log (sop_document_id, action, performed_by, note)
VALUES (@id,@act,@by,@note)",
        new[]
        {
            new MySqlParameter("@id",   sopDocumentId),
            new MySqlParameter("@act",  action),
            new MySqlParameter("@by",   performedBy),
            new MySqlParameter("@note", (object?)note ?? DBNull.Value)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: performedBy,
        eventType: action.ToUpperInvariant(),
        tableName: "sop_documents",
        module: "DocControl",
        recordId: sopDocumentId,
        description: note ?? action,
        ip: null,
        severity: "audit",
        deviceInfo: null
    ).ConfigureAwait(false);
}

/// <summary>
/// Approves a document (sets status and approved fields) + audit.  
/// Single overload to avoid call ambiguity; <paramref name="signatureHash"/> is optional.
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="approverUserId">Approver user ID.</param>
/// <param name="ip">Source IP.</param>
/// <param name="deviceInfo">Device info.</param>
/// <param name="signatureHash">Optional digital signature hash.</param>
/// <param name="token">Cancellation token.</param>
public async Task ApproveDocumentAsync(
    int documentId,
    int approverUserId,
    string ip,
    string deviceInfo,
    string? signatureHash = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
UPDATE sop_documents
SET status='approved', approved_by=@uid, approved_at=NOW()
WHERE id=@id",
        new[]
        {
            new MySqlParameter("@uid", approverUserId),
            new MySqlParameter("@id",  documentId)
        }, token).ConfigureAwait(false);

    await LogSopActionAsync(
        sopDocumentId: documentId,
        action: "approve",
        performedBy: approverUserId,
        note: string.IsNullOrWhiteSpace(signatureHash) ? "Document approved" : $"Document approved (sig={signatureHash})",
        token: token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: approverUserId,
        eventType: "APPROVE",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: documentId,
        description: $"Document approved (sig={(signatureHash ?? "")})",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo
    ).ConfigureAwait(false);
}

/// <summary>
/// Publishes a document (status = published) + audit.
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="actorUserId">User performing publish.</param>
/// <param name="ip">Source IP.</param>
/// <param name="deviceInfo">Device info.</param>
/// <param name="token">Cancellation token.</param>
public async Task PublishDocumentAsync(
    int documentId,
    int actorUserId,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE sop_documents SET status='published' WHERE id=@id",
        new[] { new MySqlParameter("@id", documentId) }, token
    ).ConfigureAwait(false);

    await LogSopActionAsync(documentId, "publish", actorUserId, "Document published", token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "PUBLISH",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: documentId,
        description: "Document published",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo
    ).ConfigureAwait(false);
}

/// <summary>
/// Creates a new revision entry (document_versions) and optionally updates the head row.
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="newVersion">New version text.</param>
/// <param name="newFilePath">Optional new file path.</param>
/// <param name="actorUserId">User performing revision.</param>
/// <param name="ip">Source IP.</param>
/// <param name="deviceInfo">Device info.</param>
/// <param name="token">Cancellation token.</param>
public async Task ReviseDocumentAsync(
    int documentId,
    string newVersion,
    string? newFilePath,
    int actorUserId,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
{
    // Add version row referencing sop_documents
    await AddDocumentVersionAsync("sop_documents", documentId, newVersion, newFilePath ?? string.Empty, actorUserId, "Revision", token).ConfigureAwait(false);

    // Update the head document record (version + optional file)
    var pars = new List<MySqlParameter>
    {
        new("@id",  documentId),
        new("@ver", newVersion)
    };
    var sql = "UPDATE sop_documents SET version=@ver";
    if (!string.IsNullOrWhiteSpace(newFilePath))
    {
        sql += ", file_path=@path";
        pars.Add(new MySqlParameter("@path", newFilePath));
    }
    sql += " WHERE id=@id";

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    await LogSopActionAsync(documentId, "revise", actorUserId, $"Revised to {newVersion}", token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "REVISE",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: documentId,
        description: $"Revised to {newVersion}",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo
    ).ConfigureAwait(false);
}

/// <summary>
/// Expires a document (status = expired) + audit.
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="actorUserId">User performing expiration.</param>
/// <param name="ip">Source IP.</param>
/// <param name="deviceInfo">Device info.</param>
/// <param name="token">Cancellation token.</param>
public async Task ExpireDocumentAsync(
    int documentId,
    int actorUserId,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE sop_documents SET status='expired' WHERE id=@id",
        new[] { new MySqlParameter("@id", documentId) }, token
    ).ConfigureAwait(false);

    await LogSopActionAsync(documentId, "expire", actorUserId, "Document expired", token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "EXPIRE",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: documentId,
        description: "Document expired",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo
    ).ConfigureAwait(false);
}

/// <summary>
/// Quick helper to create/initiate a new document (draft). Wrapper over <see cref="InsertOrUpdateSopDocumentAsync"/>.
/// </summary>
/// <param name="code">Document code.</param>
/// <param name="name">Document name.</param>
/// <param name="version">Optional textual version; defaults to "1" if not parseable.</param>
/// <param name="filePath">Optional file path.</param>
/// <param name="actorUserId">User performing creation.</param>
/// <param name="notes">Optional review notes.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>New document ID.</returns>
public async Task<int> InitiateDocumentAsync(
    string code,
    string name,
    string? version,
    string? filePath,
    int actorUserId,
    string? notes = null,
    CancellationToken token = default)
{
    var doc = Activator.CreateInstance<SopDocument>();
    SetIfExists(doc, "Code", code);
    SetIfExists(doc, "Name", name);
    SetIfExists(doc, "VersionNo", int.TryParse(version, out var v) ? v : 1);
    SetIfExists(doc, "Status", "draft");
    if (!string.IsNullOrWhiteSpace(filePath)) SetIfExists(doc, "FilePath", filePath);
    if (!string.IsNullOrWhiteSpace(notes))    SetIfExists(doc, "ReviewNotes", notes);

    int id = await InsertOrUpdateSopDocumentAsync(doc, update: false, actorUserId: actorUserId, ip: null, device: null, token).ConfigureAwait(false);
    return id;
}

/// <summary>
/// Assigns a document to a user for action/acknowledgement (minimal schema).
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="userId">User to assign.</param>
/// <param name="note">Optional note.</param>
/// <param name="actorUserId">Actor performing the assignment (for audit).</param>
/// <param name="ip">Optional IP.</param>
/// <param name="device">Optional device info.</param>
/// <param name="token">Cancellation token.</param>
public async Task AssignDocumentAsync(
    int documentId,
    int userId,
    string? note = null,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
INSERT INTO sop_document_assignments (sop_document_id, user_id, assigned_at, note)
VALUES (@doc,@uid,NOW(),@note)",
        new[]
        {
            new MySqlParameter("@doc",  documentId),
            new MySqlParameter("@uid",  userId),
            new MySqlParameter("@note", (object?)note ?? DBNull.Value),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "ASSIGN",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: documentId,
        description: $"Assigned to user #{userId}. {note}",
        ip: ip,
        severity: "audit",
        deviceInfo: device
    ).ConfigureAwait(false);
}

/// <summary>
/// Links a change control to a document (simple junction).
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="changeControlId">Change control record ID.</param>
/// <param name="actorUserId">Actor user ID.</param>
/// <param name="ip">Optional IP.</param>
/// <param name="device">Optional device.</param>
/// <param name="token">Cancellation token.</param>
public async Task LinkChangeControlToDocumentAsync(
    int documentId,
    int changeControlId,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
INSERT INTO document_change_controls (document_id, change_control_id)
VALUES (@doc,@cc)
ON DUPLICATE KEY UPDATE change_control_id=VALUES(change_control_id)",
        new[]
        {
            new MySqlParameter("@doc", documentId),
            new MySqlParameter("@cc",  changeControlId)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "LINK",
        tableName: "document_change_controls",
        module: "DocControl",
        recordId: null,
        description: $"Linked change control #{changeControlId} to document #{documentId}",
        ip: ip,
        severity: "audit",
        deviceInfo: device
    ).ConfigureAwait(false);
}

/// <summary>
/// Exports (logs) documents and returns a file path where the export would be written (logging only).
/// </summary>
/// <param name="rows">Rows to export.</param>
/// <param name="format">Export format token (e.g., "zip", "pdf").</param>
/// <param name="actorUserId">User performing export.</param>
/// <param name="ip">Source IP.</param>
/// <param name="deviceInfo">Device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Virtual export file path string.</returns>
public async Task<string> ExportDocumentsAsync(
    IEnumerable<SopDocument> rows,
    string format = "zip",
    int actorUserId = 1,
    string ip = "system",
    string deviceInfo = "server",
    string? sessionId = null,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
    string filePath = $"/export/documents_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log
(user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'sop_documents',@filter,@path,@ip,'Documents export')",
        new[]
        {
            new MySqlParameter("@uid",    actorUserId),
            new MySqlParameter("@fmt",    fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path",   filePath),
            new MySqlParameter("@ip",     ip),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: "EXPORT",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: null,
        description: $"Exported {(rows?.Count() ?? 0)} documents to {filePath}.",
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);

    return filePath;
}

/// <summary>
/// Writes a canonical document audit entry into <c>system_event_log</c> (and local log).
/// </summary>
/// <param name="documentId">Document ID.</param>
/// <param name="action">Action.</param>
/// <param name="actorUserId">User.</param>
/// <param name="description">Optional description.</param>
/// <param name="ip">Optional IP.</param>
/// <param name="deviceInfo">Optional device.</param>
/// <param name="sessionId">Optional session ID.</param>
/// <param name="token">Cancellation token.</param>
public async Task LogDocumentAuditAsync(
    int documentId,
    string action,
    int actorUserId,
    string? description = null,
    string? ip = null,
    string? deviceInfo = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: action?.ToUpperInvariant() ?? "ACTION",
        tableName: "sop_documents",
        module: "DocControl",
        recordId: documentId,
        description: description ?? action,
        ip: ip ?? "system",
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);

    await ExecuteNonQueryAsync(@"
INSERT INTO sop_document_log (sop_document_id, action, performed_by, note)
VALUES (@id,@act,@by,@note)",
        new[]
        {
            new MySqlParameter("@id",   documentId),
            new MySqlParameter("@act",  action ?? "action"),
            new MySqlParameter("@by",   actorUserId),
            new MySqlParameter("@note", (object?)description ?? DBNull.Value)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// DataRow → <see cref="SopDocument"/> (schema tolerant).
/// </summary>
/// <param name="r">Data row.</param>
/// <returns>Document.</returns>
private static SopDocument ParseSopDocument(DataRow r)
{
    var d = Activator.CreateInstance<SopDocument>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(d, "Id", Convert.ToInt32(r["id"]));

    if (r.Table.Columns.Contains("code"))
        SetIfExists(d, "Code", r["code"]?.ToString());

    if (r.Table.Columns.Contains("name"))
        SetIfExists(d, "Name", r["name"]?.ToString());

    // version (VARCHAR in DB) -> VersionNo (int in model)
    if (r.Table.Columns.Contains("version"))
    {
        var vtxt = r["version"]?.ToString();
        if (int.TryParse(vtxt, out var vnum))
            SetIfExists(d, "VersionNo", vnum);
        else
            SetIfExists(d, "VersionNo", 1);
    }

    if (r.Table.Columns.Contains("status"))
        SetIfExists(d, "Status", r["status"]?.ToString());

    if (r.Table.Columns.Contains("created_by") && r["created_by"] != DBNull.Value)
        SetIfExists(d, "CreatedById", Convert.ToInt32(r["created_by"]));

    if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
        SetIfExists(d, "DateIssued", Convert.ToDateTime(r["created_at"]));

    if (r.Table.Columns.Contains("approved_at") && r["approved_at"] != DBNull.Value)
        SetIfExists(d, "ApprovalTimestamps", new List<DateTime> { Convert.ToDateTime(r["approved_at"]) });

    if (r.Table.Columns.Contains("file_path"))
        SetIfExists(d, "FilePath", r["file_path"]?.ToString());

    // notes -> ReviewNotes
    if (r.Table.Columns.Contains("notes"))
        SetIfExists(d, "ReviewNotes", r["notes"]?.ToString());

    return d;
}

/// <summary>
/// DataRow → <see cref="SopDocumentLog"/> (schema tolerant).
/// </summary>
/// <param name="r">Data row.</param>
/// <returns>Log item.</returns>
private static SopDocumentLog ParseSopDocumentLog(DataRow r)
{
    var l = Activator.CreateInstance<SopDocumentLog>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(l, "Id", Convert.ToInt32(r["id"]));

    if (r.Table.Columns.Contains("sop_document_id") && r["sop_document_id"] != DBNull.Value)
        SetIfExists(l, "SopDocumentId", Convert.ToInt32(r["sop_document_id"]));

    if (r.Table.Columns.Contains("action"))
        SetIfExists(l, "Action", r["action"]?.ToString());

    if (r.Table.Columns.Contains("performed_by") && r["performed_by"] != DBNull.Value)
        SetIfExists(l, "PerformedBy", Convert.ToInt32(r["performed_by"]));

    if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
        SetIfExists(l, "CreatedAt", Convert.ToDateTime(r["created_at"]));

    if (r.Table.Columns.Contains("note"))
        SetIfExists(l, "Note", r["note"]?.ToString());

    return l;
}

#endregion
#region === 15 · PARAMETERS & REPORTS ======================================

/// <summary>
/// Sets or updates a named system parameter (system_parameters table).
/// </summary>
public async Task SetSystemParameterAsync(
    string name, string value, int updatedBy, string? note = null, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO system_parameters (param_name, param_value, updated_by, note)
VALUES (@name,@value,@user,@note)
ON DUPLICATE KEY UPDATE
    param_value=VALUES(param_value),
    updated_by =VALUES(updated_by),
    updated_at =CURRENT_TIMESTAMP,
    note       =VALUES(note);";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@name", name),
        new MySqlParameter("@value", value),
        new MySqlParameter("@user",  updatedBy),
        new MySqlParameter("@note",  note ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);
}

/// <summary>Returns a parameter value or null if not found.</summary>
public async Task<string?> GetSystemParameterAsync(string name, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT param_value FROM system_parameters WHERE param_name=@n LIMIT 1",
        new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : dt.Rows[0]["param_value"]?.ToString();
}

/// <summary>Returns all system parameters (schema tolerant).</summary>
public async Task<List<object>> GetAllSystemParametersAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM system_parameters ORDER BY param_name", null, token).ConfigureAwait(false);
    var list = new List<object>();
    foreach (DataRow r in dt.Rows)
    {
        var p = new Dictionary<string, object?>();
        if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value) p["Id"] = Convert.ToInt32(r["id"]);
        if (r.Table.Columns.Contains("param_name"))  p["Name"]  = r["param_name"]?.ToString();
        if (r.Table.Columns.Contains("param_value")) p["Value"] = r["param_value"]?.ToString();
        if (r.Table.Columns.Contains("updated_by") && r["updated_by"] != DBNull.Value) p["UpdatedBy"] = Convert.ToInt32(r["updated_by"]);
        if (r.Table.Columns.Contains("updated_at") && r["updated_at"] != DBNull.Value) p["UpdatedAt"] = Convert.ToDateTime(r["updated_at"]);
        if (r.Table.Columns.Contains("note")) p["Note"] = r["note"]?.ToString();
        list.Add(p);
    }
    return list;
}

/// <summary>
/// Creates or updates a scheduled report row. (Schema-tolerant input.)
/// </summary>
public async Task<int> UpsertReportScheduleAsync(
    ReportSchedule s, bool update, CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO report_schedule
           (report_name, schedule_type, format, recipients, next_due, status)
           VALUES (@name,@stype,@fmt,@rcp,@due,@status)"
        : @"UPDATE report_schedule SET
           report_name=@name, schedule_type=@stype, format=@fmt, recipients=@rcp,
           next_due=@due, status=@status
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@name",  TryGetString(s,"ReportName") ?? string.Empty),
        new("@stype", TryGetString(s,"ScheduleType") ?? "on_demand"),
        new("@fmt",   TryGetString(s,"Format") ?? "pdf"),
        new("@rcp",   (object?)TryGetString(s,"Recipients") ?? DBNull.Value),
        new("@due",   (object?)(object?)(object?)TryGet<DateTime>(s,"NextDue") ?? DBNull.Value),
        new("@status",TryGetString(s,"Status") ?? "planned")
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(s,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    int id = update
        ? (TryGet<int>(s,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

/// <summary>
/// Records a report generation result.
/// </summary>
public async Task UpdateReportRunResultAsync(
    int scheduleId, DateTime generatedAt, string status, int? generatedBy, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
UPDATE report_schedule
SET last_generated=@gen, status=@st, generated_by=@by
WHERE id=@id",
        new[]
        {
            new MySqlParameter("@gen", generatedAt),
            new MySqlParameter("@st",  status),
            new MySqlParameter("@by",  (object?)generatedBy ?? DBNull.Value),
            new MySqlParameter("@id",  scheduleId)
        }, token).ConfigureAwait(false);
}

/// <summary>
/// Inserts a unified export/print log record.
/// </summary>
public async Task<int> SaveExportPrintLogAsync(
    int userId, string format, string tableName, string filterUsed, string filePath, string? sourceIp = null, string? note = null,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO export_print_log
(user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,@tbl,@filter,@path,@ip,@note)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid",    userId),
        new MySqlParameter("@fmt",    format),
        new MySqlParameter("@tbl",    tableName),
        new MySqlParameter("@filter", filterUsed),
        new MySqlParameter("@path",   filePath),
        new MySqlParameter("@ip",     sourceIp ?? string.Empty),
        new MySqlParameter("@note",   note ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

//
// ---------------------------- SETTINGS BLOCK ----------------------------
// These fill the missing methods referenced around the app.
//

public async Task<List<Setting>> GetAllSettingsFullAsync(CancellationToken token = default)
{
    DataTable dt;
    try
    {
        dt = await ExecuteSelectAsync("SELECT * FROM settings ORDER BY `key`", null, token).ConfigureAwait(false);
    }
    catch
    {
        dt = await ExecuteSelectAsync("SELECT id, param_name AS `key`, param_value AS `value`, note AS description, updated_by, updated_at FROM system_parameters ORDER BY param_name", null, token).ConfigureAwait(false);
    }

    var list = new List<Setting>();
    foreach (DataRow r in dt.Rows) list.Add(ParseSetting(r));
    return list;
}

public async Task<int> SaveSettingAsync(
    Setting s, bool update = false, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    try
    {
        string sql = !update
            ? @"INSERT INTO settings (`key`,`value`, description, updated_by, updated_at)
                VALUES (@k,@v,@d,@uid,NOW())"
            : @"UPDATE settings SET `key`=@k, `value`=@v, description=@d, updated_by=@uid, updated_at=NOW() WHERE id=@id";

        var pars = new List<MySqlParameter>
        {
            new("@k",   TryGetString(s,"Key") ?? TryGetString(s,"Name") ?? string.Empty),
            new("@v",   (object?)TryGetString(s,"Value") ?? DBNull.Value),
            new("@d",   (object?)TryGetString(s,"Description") ?? DBNull.Value),
            new("@uid", actorUserId)
        };
        if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(s,"Id") ?? 0));

        await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
        int id = update
            ? (TryGet<int>(s,"Id") ?? 0)
            : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

        await LogSettingAuditAsync(id, "UPSERT", actorUserId, $"Setting {(update ? "updated" : "created")}", ip, device, null, token).ConfigureAwait(false);
        return id;
    }
    catch
    {
        var key = TryGetString(s,"Key") ?? TryGetString(s,"Name") ?? string.Empty;
        var val = TryGetString(s,"Value") ?? string.Empty;
        var note = TryGetString(s,"Description");
        await SetSystemParameterAsync(key, val, actorUserId, note, token).ConfigureAwait(false);
        await LogSettingAuditAsync(null, "UPSERT", actorUserId, $"Param '{key}' upserted", ip, device, null, token).ConfigureAwait(false);
        return 0;
    }
}

public async Task SaveSettingAsync(
    string key, string value, int actorUserId = 1, string? ip = null, string? device = null, string? description = null, CancellationToken token = default)
{
    try
    {
        await ExecuteNonQueryAsync(@"
INSERT INTO settings (`key`,`value`, description, updated_by, updated_at)
VALUES (@k,@v,@d,@uid,NOW())
ON DUPLICATE KEY UPDATE
    `value`=VALUES(`value`),
    description=VALUES(description),
    updated_by=VALUES(updated_by),
    updated_at=VALUES(updated_at);",
        new[]
        {
            new MySqlParameter("@k", key),
            new MySqlParameter("@v", value),
            new MySqlParameter("@d", (object?)description ?? DBNull.Value),
            new MySqlParameter("@uid", actorUserId)
        }, token).ConfigureAwait(false);
    }
    catch
    {
        await SetSystemParameterAsync(key, value, actorUserId, description, token).ConfigureAwait(false);
    }

    await LogSettingAuditAsync(null, "UPSERT", actorUserId, $"Setting '{key}' saved", ip, device, null, token).ConfigureAwait(false);
}

public async Task DeleteSettingAsync(
    int id, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    var deleted = 0;
    try
    {
        deleted = await ExecuteNonQueryAsync("DELETE FROM settings WHERE id=@id",
            new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    }
    catch
    {
        deleted = await ExecuteNonQueryAsync("DELETE FROM system_parameters WHERE id=@id",
            new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    }

    await LogSettingAuditAsync(id, "DELETE", actorUserId, deleted > 0 ? "Setting deleted" : "Setting not found", ip, device, null, token).ConfigureAwait(false);
}

public async Task DeleteSettingAsync(
    string key, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    int affected;
    try
    {
        affected = await ExecuteNonQueryAsync("DELETE FROM settings WHERE `key`=@k", new[] { new MySqlParameter("@k", key) }, token).ConfigureAwait(false);
    }
    catch
    {
        affected = await ExecuteNonQueryAsync("DELETE FROM system_parameters WHERE param_name=@k", new[] { new MySqlParameter("@k", key) }, token).ConfigureAwait(false);
    }

    await LogSettingAuditAsync(null, "DELETE", actorUserId, affected > 0 ? $"Setting '{key}' deleted" : $"Setting '{key}' not found",
        ip, device, null, token).ConfigureAwait(false);
}

public async Task RollbackSettingAsync(
    int? settingId, string? key, int actorUserId = 1, string? ip = null, string? device = null, string? contextJson = null, CancellationToken token = default)
{
    var what = settingId.HasValue ? $"id={settingId}" : $"key='{key}'";
    await LogSettingAuditAsync(settingId, "ROLLBACK", actorUserId, $"Rollback requested ({what}). Context: {contextJson}", ip, device, null, token).ConfigureAwait(false);
}

public async Task LogSettingAuditAsync(
    int? settingId, string action, int actorUserId, string? description = null, string? ip = null, string? deviceInfo = null, string? sessionId = null,
    CancellationToken token = default)
{
    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: action?.ToUpperInvariant() ?? "ACTION",
        tableName: "settings",
        module: "Settings",
        recordId: settingId,
        description: description ?? action,
        ip: ip ?? "system",
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

public async Task<string> ExportSettingsAsync(
    IEnumerable<Setting> rows, string format = "csv", int actorUserId = 1, string ip = "system", string deviceInfo = "server", string? sessionId = null,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
    string filePath = $"/export/settings_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await SaveExportPrintLogAsync(
        userId: actorUserId,
        format: fmt,
        tableName: "settings",
        filterUsed: $"count={rows?.Count() ?? 0}",
        filePath: filePath,
        sourceIp: ip,                 // FIXED
        note: "Settings export",
        token: token
    ).ConfigureAwait(false);

    await LogSettingAuditAsync(null, "EXPORT", actorUserId, $"Exported {(rows?.Count() ?? 0)} settings to {filePath}", ip, deviceInfo, sessionId, token).ConfigureAwait(false);
    return filePath;
}

public Task<string> ExportSettingsAsync(IEnumerable<Setting> rows, CancellationToken token = default)
    => ExportSettingsAsync(rows, "csv", 1, "system", "server", null, token);

private static Setting ParseSetting(DataRow r)
{
    var s = Activator.CreateInstance<Setting>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value) SetIfExists(s, "Id", Convert.ToInt32(r["id"]));

    string? key = null;
    if (r.Table.Columns.Contains("key")) key = r["key"]?.ToString();
    else if (r.Table.Columns.Contains("param_name")) key = r["param_name"]?.ToString();
    SetIfExists(s, "Key", key);
    SetIfExists(s, "Name", key);

    string? val = null;
    if (r.Table.Columns.Contains("value")) val = r["value"]?.ToString();
    else if (r.Table.Columns.Contains("param_value")) val = r["param_value"]?.ToString();
    SetIfExists(s, "Value", val);

    string? desc = null;
    if (r.Table.Columns.Contains("description")) desc = r["description"]?.ToString();
    else if (r.Table.Columns.Contains("note"))    desc = r["note"]?.ToString();
    SetIfExists(s, "Description", desc);

    if (r.Table.Columns.Contains("updated_by") && r["updated_by"] != DBNull.Value)
        SetIfExists(s, "UpdatedBy", Convert.ToInt32(r["updated_by"]));
    if (r.Table.Columns.Contains("updated_at") && r["updated_at"] != DBNull.Value)
        SetIfExists(s, "UpdatedAt", Convert.ToDateTime(r["updated_at"]));

    return s;
}

#endregion
#region === 16 · API KEYS & USAGE ==========================================

/// <summary>
/// Creates an API key row.
/// </summary>
public async Task<int> CreateApiKeyAsync(
    string keyValue, string? description, int? ownerId, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO api_keys (key_value, description, owner_id, is_active)
VALUES (@val,@desc,@owner,1)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@val",   keyValue),
        new MySqlParameter("@desc",  description ?? (object)DBNull.Value),
        new MySqlParameter("@owner", (object?)ownerId ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: ownerId, eventType: "CREATE", tableName: "api_keys", module: "API",
        recordId: id, description: "API key created", ip: "system", severity: "audit"
    ).ConfigureAwait(false);

    return id;
}

public async Task SetApiKeyStatusAsync(int apiKeyId, bool isActive, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE api_keys SET is_active=@a WHERE id=@id",
        new[]
        {
            new MySqlParameter("@a",  isActive),
            new MySqlParameter("@id", apiKeyId)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: null, eventType: "UPDATE", tableName: "api_keys", module: "API",
        recordId: apiKeyId, description: $"API key {(isActive ? "enabled" : "disabled")}", ip: "system", severity: "audit"
    ).ConfigureAwait(false);
}

public async Task RotateApiKeyAsync(int apiKeyId, string newKeyValue, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE api_keys SET key_value=@v WHERE id=@id",
        new[]
        {
            new MySqlParameter("@v",  newKeyValue),
            new MySqlParameter("@id", apiKeyId)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "ROTATE", tableName: "api_keys", module: "API",
        recordId: apiKeyId, description: "API key rotated", ip: ip ?? "system", severity: "audit", deviceInfo: device
    ).ConfigureAwait(false);
}

public async Task DeleteApiKeyAsync(int apiKeyId, int actorUserId = 1, string? ip = null, string? device = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM api_keys WHERE id=@id",
        new[] { new MySqlParameter("@id", apiKeyId) }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "DELETE", tableName: "api_keys", module: "API",
        recordId: apiKeyId, description: "API key deleted", ip: ip ?? "system", severity: "audit", deviceInfo: device
    ).ConfigureAwait(false);
}

public async Task<ApiKey?> GetApiKeyByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM api_keys WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    if (dt.Rows.Count == 0) return default;
    return ParseApiKey(dt.Rows[0]);
}

public async Task<ApiKey?> GetApiKeyByValueAsync(string keyValue, bool activeOnly = true, CancellationToken token = default)
{
    var sql = activeOnly
        ? "SELECT * FROM api_keys WHERE key_value=@v AND is_active=1 LIMIT 1"
        : "SELECT * FROM api_keys WHERE key_value=@v LIMIT 1";
    var dt = await ExecuteSelectAsync(sql, new[] { new MySqlParameter("@v", keyValue) }, token).ConfigureAwait(false);
    if (dt.Rows.Count == 0) return default;
    return ParseApiKey(dt.Rows[0]);
}

public async Task<List<ApiKey>> GetAllApiKeysFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM api_keys ORDER BY id DESC", null, token).ConfigureAwait(false);
    var list = new List<ApiKey>();
    foreach (DataRow r in dt.Rows) list.Add(ParseApiKey(r));
    return list;
}

public async Task<int> LogApiUsageAsync(
    int? apiKeyId,
    int? userId,
    string endpoint,
    string method,
    string? @params,
    int responseCode,
    int durationMs,
    bool success,
    string? errorMessage,
    string? sourceIp,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO api_usage_log
(api_key_id, user_id, endpoint, method, params, response_code, duration_ms, success, error_message, source_ip)
VALUES (@kid,@uid,@ep,@m,@p,@rc,@ms,@ok,@err,@ip)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@kid", (object?)apiKeyId ?? DBNull.Value),
        new MySqlParameter("@uid", (object?)userId   ?? DBNull.Value),
        new MySqlParameter("@ep",  endpoint),
        new MySqlParameter("@m",   method),
        new MySqlParameter("@p",   @params ?? (object)DBNull.Value),
        new MySqlParameter("@rc",  responseCode),
        new MySqlParameter("@ms",  durationMs),
        new MySqlParameter("@ok",  success),
        new MySqlParameter("@err", errorMessage ?? (object)DBNull.Value),
        new MySqlParameter("@ip",  sourceIp ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: userId, eventType: "API_USAGE", tableName: "api_usage_log", module: "API",
        recordId: id, description: $"{method} {endpoint} -> {responseCode} ({durationMs} ms, ok={success})",
        ip: sourceIp ?? "n/a", severity: "info"
    ).ConfigureAwait(false);

    return id;
}

public async Task<List<ApiUsageLog>> GetApiUsageAsync(
    DateTime? from = null, DateTime? to = null, int? apiKeyId = null, int? userId = null, string? endpoint = null, CancellationToken token = default)
{
    var sb = new StringBuilder("SELECT * FROM api_usage_log WHERE 1=1");
    var pars = new List<MySqlParameter>();
    if (from.HasValue)   { sb.Append(" AND created_at>=@from"); pars.Add(new("@from", from.Value)); }
    if (to.HasValue)     { sb.Append(" AND created_at<=@to");   pars.Add(new("@to",   to.Value)); }
    if (apiKeyId.HasValue){ sb.Append(" AND api_key_id=@kid");  pars.Add(new("@kid",  apiKeyId.Value)); }
    if (userId.HasValue) { sb.Append(" AND user_id=@uid");      pars.Add(new("@uid",  userId.Value)); }
    if (!string.IsNullOrWhiteSpace(endpoint)) { sb.Append(" AND endpoint LIKE @ep"); pars.Add(new("@ep", $"%{endpoint}%")); }
    sb.Append(" ORDER BY id DESC");

    var dt = await ExecuteSelectAsync(sb.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<ApiUsageLog>();
    foreach (DataRow r in dt.Rows) list.Add(ParseApiUsage(r));
    return list;
}

public async Task<List<ApiUsageStat>> GetApiUsageStatsAsync(DateTime? from = null, DateTime? to = null, CancellationToken token = default)
{
    var sb = new StringBuilder(@"
SELECT endpoint, COUNT(*) AS cnt, AVG(duration_ms) AS avg_ms
FROM api_usage_log WHERE 1=1");
    var pars = new List<MySqlParameter>();
    if (from.HasValue) { sb.Append(" AND created_at>=@from"); pars.Add(new("@from", from.Value)); }
    if (to.HasValue)   { sb.Append(" AND created_at<=@to");   pars.Add(new("@to",   to.Value)); }
    sb.Append(" GROUP BY endpoint ORDER BY cnt DESC");

    var dt = await ExecuteSelectAsync(sb.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<ApiUsageStat>();
    foreach (DataRow r in dt.Rows)
    {
        var s = Activator.CreateInstance<ApiUsageStat>();
        if (r.Table.Columns.Contains("endpoint")) SetIfExists(s, "Endpoint", r["endpoint"]?.ToString());
        if (r.Table.Columns.Contains("cnt") && r["cnt"] != DBNull.Value)       SetIfExists(s, "Count", Convert.ToInt32(r["cnt"]));
        if (r.Table.Columns.Contains("avg_ms") && r["avg_ms"] != DBNull.Value) SetIfExists(s, "AverageMs", Convert.ToDouble(r["avg_ms"]));
        list.Add(s);
    }
    return list;
}

public async Task<int> InsertApiAuditAsync(
    int? apiKeyId,
    int? userId,
    string action,
    string? ipAddress,
    string? requestDetails,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO api_audit_log
(api_key_id, user_id, action, ip_address, request_details)
VALUES (@kid,@uid,@act,@ip,@details)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@kid", (object?)apiKeyId ?? DBNull.Value),
        new MySqlParameter("@uid", (object?)userId   ?? DBNull.Value),
        new MySqlParameter("@act", action),
        new MySqlParameter("@ip",  ipAddress ?? (object)DBNull.Value),
        new MySqlParameter("@details", requestDetails ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: userId, eventType: "API_AUDIT", tableName: "api_audit_log", module: "API",
        recordId: id, description: action, ip: ipAddress ?? "n/a", severity: "audit"
    ).ConfigureAwait(false);

    return id;
}

public async Task<List<ApiAuditLog>> GetApiAuditLogAsync(
    DateTime? from = null, DateTime? to = null, int? apiKeyId = null, int? userId = null, string? actionLike = null, CancellationToken token = default)
{
    var sb = new StringBuilder("SELECT * FROM api_audit_log WHERE 1=1");
    var pars = new List<MySqlParameter>();
    if (from.HasValue)   { sb.Append(" AND created_at>=@from"); pars.Add(new("@from", from.Value)); }
    if (to.HasValue)     { sb.Append(" AND created_at<=@to");   pars.Add(new("@to",   to.Value)); }
    if (apiKeyId.HasValue){ sb.Append(" AND api_key_id=@kid");  pars.Add(new("@kid",  apiKeyId.Value)); }
    if (userId.HasValue) { sb.Append(" AND user_id=@uid");      pars.Add(new("@uid",  userId.Value)); }
    if (!string.IsNullOrWhiteSpace(actionLike)) { sb.Append(" AND action LIKE @act"); pars.Add(new("@act", $"%{actionLike}%")); }
    sb.Append(" ORDER BY id DESC");

    var dt = await ExecuteSelectAsync(sb.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<ApiAuditLog>();
    foreach (DataRow r in dt.Rows)
    {
        var a = Activator.CreateInstance<ApiAuditLog>();
        if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)             SetIfExists(a, "Id", Convert.ToInt32(r["id"]));
        if (r.Table.Columns.Contains("api_key_id") && r["api_key_id"] != DBNull.Value)
                                                                                    SetIfExists(a, "ApiKeyId", Convert.ToInt32(r["api_key_id"]));
        if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)   SetIfExists(a, "UserId", Convert.ToInt32(r["user_id"]));
        if (r.Table.Columns.Contains("action"))                                     SetIfExists(a, "Action", r["action"]?.ToString());
        if (r.Table.Columns.Contains("ip_address"))                                 SetIfExists(a, "IpAddress", r["ip_address"]?.ToString());
        if (r.Table.Columns.Contains("request_details"))                            SetIfExists(a, "RequestDetails", r["request_details"]?.ToString());
        if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
                                                                                    SetIfExists(a, "CreatedAt", Convert.ToDateTime(r["created_at"]));
        list.Add(a);
    }
    return list;
}

public async Task<string> ExportApiUsageAsync(IEnumerable<ApiUsageLog> rows, string format = "csv",
    int actorUserId = 1, string ip = "system", string deviceInfo = "server", string? sessionId = null, CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
    string filePath = $"/export/api_usage_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await SaveExportPrintLogAsync(
        userId: actorUserId,
        format: fmt,
        tableName: "api_usage_log",
        filterUsed: $"count={rows?.Count() ?? 0}",
        filePath: filePath,
        sourceIp: ip,                 // FIXED
        note: "API usage export",
        token: token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "EXPORT", tableName: "api_usage_log", module: "API",
        recordId: null, description: $"Exported {(rows?.Count() ?? 0)} usage rows to {filePath}", ip: ip, severity: "audit", deviceInfo: deviceInfo, sessionId: sessionId
    ).ConfigureAwait(false);

    return filePath;
}

public Task<string> ExportApiUsageAsync(IEnumerable<ApiUsageLog> rows, CancellationToken token = default)
    => ExportApiUsageAsync(rows, "csv", 1, "system", "server", null, token);

private static ApiKey ParseApiKey(DataRow r)
{
    var k = Activator.CreateInstance<ApiKey>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)           SetIfExists(k, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("key_value"))                                SetIfExists(k, "KeyValue", r["key_value"]?.ToString());
    if (r.Table.Columns.Contains("description"))                              SetIfExists(k, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("owner_id") && r["owner_id"] != DBNull.Value) SetIfExists(k, "OwnerId", Convert.ToInt32(r["owner_id"]));
    if (r.Table.Columns.Contains("is_active") && r["is_active"] != DBNull.Value) SetIfExists(k, "IsActive", Convert.ToBoolean(r["is_active"]));
    if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value) SetIfExists(k, "CreatedAt", Convert.ToDateTime(r["created_at"]));
    if (r.Table.Columns.Contains("last_used_at") && r["last_used_at"] != DBNull.Value) SetIfExists(k, "LastUsedAt", Convert.ToDateTime(r["last_used_at"]));
    return k;
}

private static ApiUsageLog ParseApiUsage(DataRow r)
{
    var u = Activator.CreateInstance<ApiUsageLog>();
    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                 SetIfExists(u, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("api_key_id") && r["api_key_id"] != DBNull.Value) SetIfExists(u, "ApiKeyId", Convert.ToInt32(r["api_key_id"]));
    if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)       SetIfExists(u, "UserId", Convert.ToInt32(r["user_id"]));
    if (r.Table.Columns.Contains("endpoint"))                                       SetIfExists(u, "Endpoint", r["endpoint"]?.ToString());
    if (r.Table.Columns.Contains("method"))                                         SetIfExists(u, "Method", r["method"]?.ToString());
    if (r.Table.Columns.Contains("params"))                                         SetIfExists(u, "Params", r["params"]?.ToString());
    if (r.Table.Columns.Contains("response_code") && r["response_code"] != DBNull.Value) SetIfExists(u, "ResponseCode", Convert.ToInt32(r["response_code"]));
    if (r.Table.Columns.Contains("duration_ms") && r["duration_ms"] != DBNull.Value)     SetIfExists(u, "DurationMs", Convert.ToInt32(r["duration_ms"]));
    if (r.Table.Columns.Contains("success") && r["success"] != DBNull.Value)       SetIfExists(u, "Success", Convert.ToBoolean(r["success"]));
    if (r.Table.Columns.Contains("error_message"))                                  SetIfExists(u, "ErrorMessage", r["error_message"]?.ToString());
    if (r.Table.Columns.Contains("source_ip"))                                      SetIfExists(u, "SourceIp", r["source_ip"]?.ToString());
    if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value) SetIfExists(u, "CreatedAt", Convert.ToDateTime(r["created_at"]));
    return u;
}

#endregion

#region === 17 · DIGITAL SIGNATURES ========================================

/// <summary>
/// Core insert used by all overloads.
/// </summary>
private async Task<int> InsertSignatureCoreAsync(
    string tableName,
    int recordId,
    int? userId,
    string signatureHash,
    string method,
    string status,
    string? ip,
    string? device,
    string? note,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO digital_signatures
(table_name, record_id, user_id, signature_hash, method, status, ip_address, device_info, note)
VALUES (@tbl,@rid,@uid,@hash,@method,@status,@ip,@dev,@note)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@tbl",   tableName ?? string.Empty),
        new MySqlParameter("@rid",   recordId),
        new MySqlParameter("@uid",   (object?)userId ?? DBNull.Value),
        new MySqlParameter("@hash",  signatureHash ?? string.Empty),
        new MySqlParameter("@method",method ?? "pin"),
        new MySqlParameter("@status",status ?? "valid"),
        new MySqlParameter("@ip",    (object?)ip ?? DBNull.Value),
        new MySqlParameter("@dev",   (object?)device ?? DBNull.Value),
        new MySqlParameter("@note",  (object?)note ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(
        userId: userId,
        eventType: "SIGN",
        tableName: tableName,
        module: "DigitalSignature",
        recordId: recordId,
        description: $"Signature #{id} added (status={status})",
        ip: ip ?? "system",
        severity: "audit",
        deviceInfo: device
    ).ConfigureAwait(false);

    return id;
}

public async Task<int> InsertDigitalSignatureAsync(
    DigitalSignature sig, CancellationToken token = default)
{
    return await InsertSignatureCoreAsync(
        tableName:     TryGetString(sig,"TableName") ?? string.Empty,
        recordId:      TryGet<int>(sig,"RecordId") ?? 0,
        userId:        TryGet<int>(sig,"UserId"),
        signatureHash: TryGetString(sig,"SignatureHash") ?? string.Empty,
        method:        TryGetString(sig,"Method") ?? "pin",
        status:        TryGetString(sig,"Status") ?? "valid",
        ip:            TryGetString(sig,"IpAddress"),
        device:        TryGetString(sig,"DeviceInfo"),
        note:          TryGetString(sig,"Note"),
        token:         token
    ).ConfigureAwait(false);
}

public Task<int> AddSignatureAsync(
    string tableName, int recordId, int userId, string signatureHash,
    string method = "pin", string status = "valid", string? ip = null, string? device = null, string? note = null,
    CancellationToken token = default)
    => InsertSignatureCoreAsync(tableName, recordId, userId, signatureHash, method, status, ip, device, note, token);

public async Task RevokeDigitalSignatureAsync(int id, string? note = null, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE digital_signatures SET status='revoked', note=COALESCE(@note,note) WHERE id=@id",
        new[]
        {
            new MySqlParameter("@id", id),
            new MySqlParameter("@note", note ?? (object)DBNull.Value)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: null, eventType: "REVOKE", tableName: "digital_signatures", module: "DigitalSignature",
        recordId: id, description: "Signature revoked", ip: "system", severity: "audit"
    ).ConfigureAwait(false);
}

public Task RevokeSignatureAsync(int id, string? note = null, CancellationToken token = default)
    => RevokeDigitalSignatureAsync(id, note, token);

public async Task<bool> VerifySignatureAsync(int signatureId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT status FROM digital_signatures WHERE id=@id",
        new[] { new MySqlParameter("@id", signatureId) }, token).ConfigureAwait(false);
    if (dt.Rows.Count == 0) return false;
    var status = dt.Rows[0]["status"]?.ToString();
    return string.Equals(status, "valid", StringComparison.OrdinalIgnoreCase);
}

public async Task<bool> VerifySignatureAsync(string tableName, int recordId, string signatureHash, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(@"
SELECT COUNT(*) FROM digital_signatures
WHERE table_name=@t AND record_id=@r AND signature_hash=@h AND status='valid'",
        new[]
        {
            new MySqlParameter("@t", tableName),
            new MySqlParameter("@r", recordId),
            new MySqlParameter("@h", signatureHash)
        }, token).ConfigureAwait(false);

    var cnt = Convert.ToInt32(dt.Rows[0][0]);
    return cnt > 0;
}

public async Task<List<DigitalSignature>> GetSignaturesForAsync(string table, int recordId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM digital_signatures WHERE table_name=@t AND record_id=@r ORDER BY signed_at DESC",
        new[] { new MySqlParameter("@t", table), new MySqlParameter("@r", recordId) }, token).ConfigureAwait(false);

    var list = new List<DigitalSignature>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDigitalSignature(r));
    return list;
}

public async Task<List<DigitalSignature>> GetAllSignaturesFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM digital_signatures ORDER BY signed_at DESC", null, token).ConfigureAwait(false);
    var list = new List<DigitalSignature>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDigitalSignature(r));
    return list;
}

public async Task<string> ExportSignaturesAsync(IEnumerable<DigitalSignature> rows, string format = "csv",
    int actorUserId = 1, string ip = "system", string deviceInfo = "server", string? sessionId = null, CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
    string filePath = $"/export/digital_signatures_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await SaveExportPrintLogAsync(
        userId: actorUserId,
        format: fmt,
        tableName: "digital_signatures",
        filterUsed: $"count={rows?.Count() ?? 0}",
        filePath: filePath,
        sourceIp: ip,                 // FIXED
        note: "Signatures export",
        token: token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId, eventType: "EXPORT", tableName: "digital_signatures", module: "DigitalSignature",
        recordId: null, description: $"Exported {(rows?.Count() ?? 0)} signatures to {filePath}", ip: ip, severity: "audit", deviceInfo: deviceInfo, sessionId: sessionId
    ).ConfigureAwait(false);

    return filePath;
}

private static DigitalSignature ParseDigitalSignature(DataRow r)
{
    var d = Activator.CreateInstance<DigitalSignature>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(d, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("table_name"))
        SetIfExists(d, "TableName", r["table_name"]?.ToString());
    if (r.Table.Columns.Contains("record_id") && r["record_id"] != DBNull.Value)
        SetIfExists(d, "RecordId", Convert.ToInt32(r["record_id"]));
    if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)
        SetIfExists(d, "UserId", Convert.ToInt32(r["user_id"]));
    if (r.Table.Columns.Contains("signature_hash"))
        SetIfExists(d, "SignatureHash", r["signature_hash"]?.ToString());
    if (r.Table.Columns.Contains("signed_at") && r["signed_at"] != DBNull.Value)
        SetIfExists(d, "SignedAt", Convert.ToDateTime(r["signed_at"]));
    if (r.Table.Columns.Contains("method"))
        SetIfExists(d, "Method", r["method"]?.ToString());
    if (r.Table.Columns.Contains("status"))
        SetIfExists(d, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("ip_address"))
        SetIfExists(d, "IpAddress", r["ip_address"]?.ToString());
    if (r.Table.Columns.Contains("device_info"))
        SetIfExists(d, "DeviceInfo", r["device_info"]?.ToString());
    if (r.Table.Columns.Contains("note"))
        SetIfExists(d, "Note", r["note"]?.ToString());

    return d;
}

#endregion

#region === 18 · PHOTOS & ATTACHMENTS ======================================

        // ---------- LIST (simple, full table) ----------
        public async Task<List<Attachment>> GetAllAttachmentsFullAsync(CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM attachments ORDER BY uploaded_at DESC",
                null, token).ConfigureAwait(false);

            var list = new List<Attachment>();
            foreach (DataRow r in dt.Rows) list.Add(ParseAttachment(r));
            return list;
        }

        // ---------- LIST (DB-side filtering; PASS NULLS TO SKIP) ----------
        // Filters: related table, related id, file type, search (name/note), uploaded date range
        public async Task<List<Attachment>> GetAttachmentsFilteredAsync(
            string? relatedTable = null,
            int? relatedId = null,
            string? fileType = null,
            string? search = null,
            DateTime? uploadedFrom = null,
            DateTime? uploadedTo = null,
            CancellationToken token = default)
        {
            var sql = new StringBuilder("SELECT * FROM attachments WHERE 1=1");
            var pars = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(relatedTable))
            {
                sql.Append(" AND related_table=@t");
                pars.Add(new MySqlParameter("@t", relatedTable));
            }
            if (relatedId.HasValue)
            {
                sql.Append(" AND related_id=@r");
                pars.Add(new MySqlParameter("@r", relatedId.Value));
            }
            if (!string.IsNullOrWhiteSpace(fileType))
            {
                sql.Append(" AND file_type=@ft");
                pars.Add(new MySqlParameter("@ft", fileType));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql.Append(" AND (file_name LIKE @q OR note LIKE @q)");
                pars.Add(new MySqlParameter("@q", $"%{search}%"));
            }
            if (uploadedFrom.HasValue)
            {
                sql.Append(" AND uploaded_at >= @from");
                pars.Add(new MySqlParameter("@from", uploadedFrom.Value));
            }
            if (uploadedTo.HasValue)
            {
                sql.Append(" AND uploaded_at <= @to");
                pars.Add(new MySqlParameter("@to", uploadedTo.Value));
            }

            sql.Append(" ORDER BY uploaded_at DESC");

            var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
            var list = new List<Attachment>();
            foreach (DataRow r in dt.Rows) list.Add(ParseAttachment(r));
            return list;
        }

        // ---------- LIST (compat — used by some VMs) ----------
        public async Task<List<Attachment>> GetAttachmentsAsync(string relatedTable, int relatedId, CancellationToken token = default)
        {
            return await GetAttachmentsFilteredAsync(
                relatedTable: relatedTable,
                relatedId: relatedId,
                token: token).ConfigureAwait(false);
        }

        // ---------- ADD (multiple overloads; tolerant to model shape) ----------
        // (Attachment payload; tolerant via TryGet*)
        public async Task<int> InsertAttachmentAsync(Attachment a, CancellationToken token = default)
        {
            // Pull values via safe reflection so we don't depend on strict model members
            string relatedTable = TryGetString(a, "RelatedTable")
                               ?? TryGetString(a, "Table")
                               ?? TryGetString(a, "Entity")
                               ?? string.Empty;

            int? relatedId = TryGet<int>(a, "RelatedId")
                          ?? TryGet<int>(a, "RecordId")
                          ?? TryGet<int>(a, "EntityId");

            string filePath = TryGetString(a, "FilePath")
                           ?? TryGetString(a, "Path")
                           ?? string.Empty;

            string fileName = TryGetString(a, "FileName")
                           ?? TryGetString(a, "Name")
                           ?? (System.IO.Path.GetFileName(filePath) ?? string.Empty);

            string? fileType = TryGetString(a, "FileType") ?? TryGetString(a, "Type");
            int? uploadedBy  = TryGet<int>(a, "UploadedBy") ?? TryGet<int>(a, "UserId");
            string? note     = TryGetString(a, "Note") ?? TryGetString(a, "Description");

            return await InsertAttachmentCoreAsync(
                relatedTable: relatedTable,
                relatedId: relatedId,
                fileName: fileName,
                filePath: filePath,
                fileType: fileType,
                uploadedBy: uploadedBy,
                note: note,
                token: token
            ).ConfigureAwait(false);
        }

        // (relatedTable, relatedId, fileName, filePath)
        public async Task<int> AddAttachmentAsync(string relatedTable, int relatedId, string fileName, string filePath, CancellationToken token = default)
        {
            return await InsertAttachmentCoreAsync(
                relatedTable: relatedTable,
                relatedId: relatedId,
                fileName: fileName,
                filePath: filePath,
                fileType: null,
                uploadedBy: null,
                note: null,
                token: token
            ).ConfigureAwait(false);
        }

        // (relatedTable, relatedId as string, fileName, filePath)
        public async Task<int> AddAttachmentAsync(string relatedTable, string relatedId, string fileName, string filePath, CancellationToken token = default)
        {
            int rid; int.TryParse(relatedId, out rid);
            return await AddAttachmentAsync(relatedTable, rid, fileName, filePath, token).ConfigureAwait(false);
        }

        // Actual DB insert + audit (shared core)
        private async Task<int> InsertAttachmentCoreAsync(
            string relatedTable,
            int? relatedId,
            string fileName,
            string filePath,
            string? fileType,
            int? uploadedBy,
            string? note,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO attachments
(related_table, related_id, file_name, file_path, file_type, uploaded_by, note)
VALUES (@tbl,@rid,@name,@path,@type,@uid,@note)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@tbl",  relatedTable ?? string.Empty),
                new MySqlParameter("@rid",  (object?)relatedId ?? DBNull.Value),
                new MySqlParameter("@name", fileName ?? string.Empty),
                new MySqlParameter("@path", filePath ?? string.Empty),
                new MySqlParameter("@type", (object?)fileType ?? DBNull.Value),
                new MySqlParameter("@uid",  (object?)uploadedBy ?? DBNull.Value),
                new MySqlParameter("@note", (object?)note ?? DBNull.Value)
            }, token).ConfigureAwait(false);

            int id = Convert.ToInt32(
                await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

            await LogSystemEventAsync(
                userId: uploadedBy,
                eventType: "CREATE",
                tableName: "attachments",
                module: "Attachment",
                recordId: id,
                description: $"Attachment '{fileName}' added to {relatedTable}#{relatedId}.",
                ip: "system",
                severity: "audit"
            ).ConfigureAwait(false);

            return id;
        }

        // ---------- DELETE ----------
        public async Task DeleteAttachmentAsync(int id, CancellationToken token = default)
        {
            await ExecuteNonQueryAsync("DELETE FROM attachments WHERE id=@id",
                new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: null, eventType: "DELETE", tableName: "attachments", module: "Attachment",
                recordId: id, description: "Attachment deleted.", ip: "system", severity: "audit"
            ).ConfigureAwait(false);
        }

        // with audit context
        public async Task DeleteAttachmentAsync(int id, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            await ExecuteNonQueryAsync("DELETE FROM attachments WHERE id=@id",
                new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: actorUserId, eventType: "DELETE", tableName: "attachments", module: "Attachment",
                recordId: id, description: "Attachment deleted.", ip: ip, severity: "audit", deviceInfo: deviceInfo
            ).ConfigureAwait(false);
        }

        // helper if caller passes id as string
        public async Task DeleteAttachmentAsync(string id, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
        {
            int aid; int.TryParse(id, out aid);
            await DeleteAttachmentAsync(aid, actorUserId, ip, deviceInfo, token).ConfigureAwait(false);
        }

        // ---------- APPROVE (audit only; e-signature already handled elsewhere) ----------
        public async Task ApproveAttachmentAsync(int attachmentId, int actorUserId, string ip, string deviceInfo, string signatureHash, CancellationToken token = default)
        {
            await LogSystemEventAsync(
                userId: actorUserId, eventType: "APPROVE", tableName: "attachments", module: "Attachment",
                recordId: attachmentId, description: $"Attachment approved. sig={signatureHash}", ip: ip,
                severity: "audit", deviceInfo: deviceInfo
            ).ConfigureAwait(false);
        }

        public Task ApproveAttachmentAsync(int attachmentId, int actorUserId, string ip, string deviceInfo, CancellationToken token = default)
            => ApproveAttachmentAsync(attachmentId, actorUserId, ip, deviceInfo, string.Empty, token);

        // ---------- ROLLBACK (audit only) ----------
        public async Task RollbackAttachmentAsync(int attachmentId, int actorUserId, string ip, string deviceInfo, string? sessionId = null, string? contextJson = null, CancellationToken token = default)
        {
            await LogSystemEventAsync(
                userId: actorUserId, eventType: "ROLLBACK", tableName: "attachments", module: "Attachment",
                recordId: attachmentId, description: $"Attachment rollback. Context: {contextJson ?? ""}", ip: ip,
                severity: "audit", deviceInfo: deviceInfo, sessionId: sessionId
            ).ConfigureAwait(false);
        }

        // ---------- EXPORT (logs only; real export handled elsewhere) ----------
        public async Task<string> ExportAttachmentsAsync(IEnumerable<Attachment> rows, string format, int actorUserId = 1, string ip = "system", string deviceInfo = "server", string? sessionId = null, CancellationToken token = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
            string filePath = $"/export/attachments_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";
            int count = rows?.Count() ?? 0;

            await LogSystemEventAsync(
                userId: actorUserId, eventType: "EXPORT", tableName: "attachments", module: "Attachment",
                recordId: null, description: $"Exported {count} attachments to {filePath}.",
                ip: ip, severity: "audit", deviceInfo: deviceInfo, sessionId: sessionId
            ).ConfigureAwait(false);

            return filePath;
        }

        public Task<string> ExportAttachmentsAsync(IEnumerable<Attachment> rows, CancellationToken token = default)
            => ExportAttachmentsAsync(rows, "zip", 1, "system", "server", null, token);

        // ---------- AUDIT (Attachment) ----------
        public async Task<IEnumerable<AttachmentAuditLog>> GetAttachmentAuditLogAsync(int attachmentId, CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(@"
SELECT * FROM system_event_log
WHERE table_name='attachments' AND record_id=@id
ORDER BY event_time DESC",
                new[] { new MySqlParameter("@id", attachmentId) }, token).ConfigureAwait(false);

            var list = new List<AttachmentAuditLog>();
            foreach (DataRow r in dt.Rows)
            {
                var item = Activator.CreateInstance(typeof(AttachmentAuditLog));
                if (item == null) continue;

                void Set(string name, object? value)
                {
                    try
                    {
                        var p = item.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                        if (p == null || !p.CanWrite) return;
                        var v = value == DBNull.Value ? null : value;
                        if (v != null)
                        {
                            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                            if (v.GetType() != t) v = Convert.ChangeType(v, t);
                        }
                        p.SetValue(item, v);
                    }
                    catch { /* ignore mapping issues */ }
                }

                if (r.Table.Columns.Contains("id"))         Set("Id", Convert.ToInt32(r["id"]));
                Set("AttachmentId", attachmentId);
                if (r.Table.Columns.Contains("user_id"))    Set("UserId", r["user_id"] == DBNull.Value ? null : Convert.ToInt32(r["user_id"]));
                if (r.Table.Columns.Contains("event_type")) Set("Action", r["event_type"]?.ToString());
                if (r.Table.Columns.Contains("description"))Set("Description", r["description"]?.ToString());
                if (r.Table.Columns.Contains("event_time")) Set("ActionAt", r["event_time"] == DBNull.Value ? null : Convert.ToDateTime(r["event_time"]));
                if (r.Table.Columns.Contains("source_ip"))  Set("SourceIp", r["source_ip"]?.ToString());
                if (r.Table.Columns.Contains("device_info"))Set("DeviceInfo", r["device_info"]?.ToString());
                if (r.Table.Columns.Contains("session_id")) Set("SessionId", r["session_id"]?.ToString());

                list.Add((AttachmentAuditLog)item);
            }
            return list;
        }

        // ---------- UTILITIES ----------
        public async Task<string> ScanFileForVirusesAsync(string filePath, CancellationToken token = default)
        {
            await LogSystemEventAsync(
                userId: null, eventType: "SCAN", tableName: "attachments", module: "Attachment",
                recordId: null, description: $"Virus scan requested for '{filePath}'.", ip: "system", severity: "info"
            ).ConfigureAwait(false);
            return "clean"; // placeholder
        }

        public async Task<string> ExtractTextFromFileAsync(string filePath, CancellationToken token = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
                    if (!System.IO.File.Exists(filePath))   return string.Empty;
                    return System.IO.File.ReadAllText(filePath);
                }, token).ConfigureAwait(false);
            }
            catch { return string.Empty; }
        }

        public async Task SetPhotoWatermarkAsync(int photoId, bool applied, CancellationToken token = default)
        {
            await ExecuteNonQueryAsync(
                "UPDATE photos SET watermark_applied=@w WHERE id=@id",
                new[] { new MySqlParameter("@w", applied), new MySqlParameter("@id", photoId) }, token).ConfigureAwait(false);
        }

        public async Task<List<Photo>> GetPhotosAsync(int? workOrderId, int? componentId, CancellationToken token = default)
        {
            var sql = new StringBuilder("SELECT * FROM photos WHERE 1=1");
            var pars = new List<MySqlParameter>();
            if (workOrderId.HasValue) { sql.Append(" AND work_order_id=@wo"); pars.Add(new("@wo", workOrderId.Value)); }
            if (componentId.HasValue) { sql.Append(" AND component_id=@cid"); pars.Add(new("@cid", componentId.Value)); }
            sql.Append(" ORDER BY uploaded_at DESC");

            var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
            var list = new List<Photo>();
            foreach (DataRow r in dt.Rows) list.Add(ParsePhoto(r));
            return list;
        }

        // Tolerant parser: only sets members that exist on Photo
        private static Photo ParsePhoto(DataRow r)
        {
            var p = Activator.CreateInstance<Photo>();

            if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
                SetIfExists(p, "Id", Convert.ToInt32(r["id"]));
            if (r.Table.Columns.Contains("work_order_id") && r["work_order_id"] != DBNull.Value)
                SetIfExists(p, "WorkOrderId", Convert.ToInt32(r["work_order_id"]));
            if (r.Table.Columns.Contains("component_id") && r["component_id"] != DBNull.Value)
                SetIfExists(p, "ComponentId", Convert.ToInt32(r["component_id"]));
            if (r.Table.Columns.Contains("file_name"))
                SetIfExists(p, "FileName", r["file_name"]?.ToString());
            if (r.Table.Columns.Contains("file_path"))
                SetIfExists(p, "FilePath", r["file_path"]?.ToString());
            if (r.Table.Columns.Contains("type"))
                SetIfExists(p, "Type", r["type"]?.ToString());
            if (r.Table.Columns.Contains("uploaded_by") && r["uploaded_by"] != DBNull.Value)
                SetIfExists(p, "UploadedBy", Convert.ToInt32(r["uploaded_by"]));
            if (r.Table.Columns.Contains("uploaded_at") && r["uploaded_at"] != DBNull.Value)
                SetIfExists(p, "UploadedAt", Convert.ToDateTime(r["uploaded_at"]));
            if (r.Table.Columns.Contains("watermark_applied") && r["watermark_applied"] != DBNull.Value)
                SetIfExists(p, "WatermarkApplied", Convert.ToBoolean(r["watermark_applied"]));
            if (r.Table.Columns.Contains("note"))
                SetIfExists(p, "Note", r["note"]?.ToString());

            return p!;
        }

        // Tolerant parser: only sets members that exist on Attachment
        private static Attachment ParseAttachment(DataRow r)
        {
            var a = Activator.CreateInstance<Attachment>();

            if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
                SetIfExists(a, "Id", Convert.ToInt32(r["id"]));
            if (r.Table.Columns.Contains("related_table"))
                SetIfExists(a, "RelatedTable", r["related_table"]?.ToString());
            if (r.Table.Columns.Contains("related_id") && r["related_id"] != DBNull.Value)
                SetIfExists(a, "RelatedId", Convert.ToInt32(r["related_id"]));
            if (r.Table.Columns.Contains("file_name"))
                SetIfExists(a, "FileName", r["file_name"]?.ToString());
            if (r.Table.Columns.Contains("file_path"))
                SetIfExists(a, "FilePath", r["file_path"]?.ToString());
            if (r.Table.Columns.Contains("file_type"))
                SetIfExists(a, "FileType", r["file_type"]?.ToString());
            if (r.Table.Columns.Contains("uploaded_by") && r["uploaded_by"] != DBNull.Value)
                SetIfExists(a, "UploadedBy", Convert.ToInt32(r["uploaded_by"]));
            if (r.Table.Columns.Contains("uploaded_at") && r["uploaded_at"] != DBNull.Value)
                SetIfExists(a, "UploadedAt", Convert.ToDateTime(r["uploaded_at"]));
            if (r.Table.Columns.Contains("note"))
                SetIfExists(a, "Note", r["note"]?.ToString());

            return a!;
        }

#endregion

#region === 19 · DASHBOARDS =================================================

        /// <summary>Create or update a dashboard row (schema-tolerant input, with audit log).</summary>
        public async Task<int> UpsertDashboardAsync(
            Dashboard d,
            bool update,
            int actorUserId,
            CancellationToken token = default)
        {
            string sql = !update
                ? @"INSERT INTO dashboards (dashboard_name, description, created_by, config_json, is_active)
                    VALUES (@name,@desc,@uid,@cfg,@active)"
                : @"UPDATE dashboards
                    SET dashboard_name=@name, description=@desc, config_json=@cfg, is_active=@active
                    WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@name",   TryGetString(d, "DashboardName") ?? string.Empty),
                new("@desc",   (object?)TryGetString(d, "Description") ?? DBNull.Value),
                new("@uid",    actorUserId),
                new("@cfg",    (object?)TryGetString(d, "ConfigJson") ?? DBNull.Value),
                new("@active", TryGet<bool>(d, "IsActive") ?? false)
            };
            if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(d, "Id") ?? 0));

            await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

            int id = update
                ? (TryGet<int>(d, "Id") ?? 0)
                : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

            // Audit
            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: update ? "UPDATE" : "CREATE",
                tableName: "dashboards",
                module: "Dashboard",
                recordId: id,
                description: update ? "Dashboard updated" : "Dashboard created",
                ip: "system",
                severity: "audit",
                deviceInfo: "server"
            ).ConfigureAwait(false);

            return id;
        }

        /// <summary>Returns a single dashboard by name (case-sensitive DB collation rules apply).</summary>
        public async Task<Dashboard?> GetDashboardByNameAsync(string name, CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM dashboards WHERE dashboard_name=@n LIMIT 1",
                new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);

            return dt.Rows.Count == 0 ? null : ParseDashboard(dt.Rows[0]);
        }

        /// <summary>Returns a single dashboard by id.</summary>
        public async Task<Dashboard?> GetDashboardByIdAsync(int id, CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM dashboards WHERE id=@id LIMIT 1",
                new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

            return dt.Rows.Count == 0 ? null : ParseDashboard(dt.Rows[0]);
        }

        /// <summary>Lists dashboards (active first, newest first).</summary>
        public async Task<List<Dashboard>> GetAllDashboardsAsync(CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM dashboards ORDER BY is_active DESC, created_at DESC",
                null, token).ConfigureAwait(false);

            var list = new List<Dashboard>();
            foreach (DataRow r in dt.Rows) list.Add(ParseDashboard(r));
            return list;
        }

        /// <summary>Enable/disable a dashboard (with audit log).</summary>
        public async Task SetDashboardActiveAsync(int id, bool active, CancellationToken token = default)
        {
            await ExecuteNonQueryAsync(
                "UPDATE dashboards SET is_active=@a WHERE id=@id",
                new[] { new MySqlParameter("@a", active), new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: null,
                eventType: "STATUS",
                tableName: "dashboards",
                module: "Dashboard",
                recordId: id,
                description: $"Dashboard {(active ? "activated" : "deactivated")}",
                ip: "system",
                severity: "audit",
                deviceInfo: "server"
            ).ConfigureAwait(false);
        }

        /// <summary>Deletes a dashboard (hard delete) with audit log.</summary>
        public async Task DeleteDashboardAsync(int id, int actorUserId, CancellationToken token = default)
        {
            await ExecuteNonQueryAsync(
                "DELETE FROM dashboards WHERE id=@id",
                new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

            await LogSystemEventAsync(
                userId: actorUserId,
                eventType: "DELETE",
                tableName: "dashboards",
                module: "Dashboard",
                recordId: id,
                description: "Dashboard deleted",
                ip: "system",
                severity: "audit",
                deviceInfo: "server"
            ).ConfigureAwait(false);
        }

        /// <summary>Row → Dashboard (schema-tolerant).</summary>
        private static Dashboard ParseDashboard(DataRow r)
        {
            var d = Activator.CreateInstance<Dashboard>();
            if (d == null) return new Dashboard();

            if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
                SetIfExists(d, "Id", Convert.ToInt32(r["id"]));
            if (r.Table.Columns.Contains("dashboard_name"))
                SetIfExists(d, "DashboardName", r["dashboard_name"]?.ToString());
            if (r.Table.Columns.Contains("description"))
                SetIfExists(d, "Description", r["description"]?.ToString());
            if (r.Table.Columns.Contains("created_by") && r["created_by"] != DBNull.Value)
                SetIfExists(d, "CreatedBy", Convert.ToInt32(r["created_by"]));
            if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
                SetIfExists(d, "CreatedAt", Convert.ToDateTime(r["created_at"]));
            if (r.Table.Columns.Contains("config_json"))
                SetIfExists(d, "ConfigJson", r["config_json"]?.ToString());
            if (r.Table.Columns.Contains("is_active") && r["is_active"] != DBNull.Value)
                SetIfExists(d, "IsActive", Convert.ToBoolean(r["is_active"]));

            return d;
        }

#endregion

#region === 20 · SYSTEM EVENT LOG (READ/MAINT) ================================

public async Task<List<SystemEvent>> GetSystemEventsAsync(
    int? userId = null,
    string? module = null,
    string? tableName = null,
    string? severity = null,
    DateTime? from = null,
    DateTime? to = null,
    bool? processed = null,
    int limit = 200,
    int offset = 0,
    CancellationToken token = default)
{
    // sane guards for paging
    if (limit <= 0) limit = 50;
    if (limit > 1000) limit = 1000;
    if (offset < 0) offset = 0;

    var sql = new StringBuilder("SELECT * FROM system_event_log WHERE 1=1");
    var pars = new List<MySqlParameter>();

    if (userId.HasValue) { sql.Append(" AND user_id=@u"); pars.Add(new("@u", userId.Value)); }
    if (!string.IsNullOrWhiteSpace(module)) { sql.Append(" AND related_module=@m"); pars.Add(new("@m", module)); }
    if (!string.IsNullOrWhiteSpace(tableName)) { sql.Append(" AND table_name=@t"); pars.Add(new("@t", tableName)); }
    if (!string.IsNullOrWhiteSpace(severity)) { sql.Append(" AND severity=@s"); pars.Add(new("@s", severity)); }
    if (from.HasValue) { sql.Append(" AND event_time >= @f"); pars.Add(new("@f", from.Value)); }
    if (to.HasValue) { sql.Append(" AND event_time <= @to"); pars.Add(new("@to", to.Value)); }
    if (processed.HasValue) { sql.Append(" AND processed=@p"); pars.Add(new("@p", processed.Value)); }

    sql.Append(" ORDER BY event_time DESC LIMIT @lim OFFSET @off");
    pars.Add(new MySqlParameter("@lim", limit));
    pars.Add(new MySqlParameter("@off", offset));

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);

    var list = new List<SystemEvent>();
    foreach (DataRow r in dt.Rows) list.Add(ParseSystemEvent(r));
    return list;
}

public async Task MarkSystemEventsProcessedAsync(IEnumerable<int> ids, bool processed = true, CancellationToken token = default)
{
    var idArray = (ids ?? Array.Empty<int>()).ToArray();
    if (idArray.Length == 0) return;

    // update in chunks to avoid oversized parameter lists
    const int chunkSize = 500;
    for (int start = 0; start < idArray.Length; start += chunkSize)
    {
        int count = Math.Min(chunkSize, idArray.Length - start);

        var sb = new StringBuilder();
        var pars = new List<MySqlParameter>();
        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            string pn = "@p" + i.ToString();
            sb.Append(pn);
            pars.Add(new MySqlParameter(pn, idArray[start + i]));
        }
        pars.Add(new MySqlParameter("@proc", processed));

        string sql = $"UPDATE system_event_log SET processed=@proc WHERE id IN ({sb})";
        await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    }
}

// Schema-tolerant parser: only sets members that exist on SystemEvent
private static SystemEvent ParseSystemEvent(DataRow r)
{
    var e = Activator.CreateInstance<SystemEvent>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(e, "Id", Convert.ToInt32(r["id"]));

    if (r.Table.Columns.Contains("event_time") && r["event_time"] != DBNull.Value)
        SetIfExists(e, "EventTime", Convert.ToDateTime(r["event_time"]));

    if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)
        SetIfExists(e, "UserId", Convert.ToInt32(r["user_id"]));

    if (r.Table.Columns.Contains("event_type"))
        SetIfExists(e, "EventType", r["event_type"]?.ToString());

    if (r.Table.Columns.Contains("table_name"))
        SetIfExists(e, "TableName", r["table_name"]?.ToString());

    if (r.Table.Columns.Contains("related_module"))
        SetIfExists(e, "RelatedModule", r["related_module"]?.ToString());

    if (r.Table.Columns.Contains("record_id") && r["record_id"] != DBNull.Value)
        SetIfExists(e, "RecordId", Convert.ToInt32(r["record_id"]));

    if (r.Table.Columns.Contains("field_name"))
        SetIfExists(e, "FieldName", r["field_name"]?.ToString());

    if (r.Table.Columns.Contains("old_value"))
        SetIfExists(e, "OldValue", r["old_value"]?.ToString());

    if (r.Table.Columns.Contains("new_value"))
        SetIfExists(e, "NewValue", r["new_value"]?.ToString());

    if (r.Table.Columns.Contains("description"))
        SetIfExists(e, "Description", r["description"]?.ToString());

    if (r.Table.Columns.Contains("source_ip"))
        SetIfExists(e, "SourceIp", r["source_ip"]?.ToString());

    if (r.Table.Columns.Contains("device_info"))
        SetIfExists(e, "DeviceInfo", r["device_info"]?.ToString());

    if (r.Table.Columns.Contains("session_id"))
        SetIfExists(e, "SessionId", r["session_id"]?.ToString());

    if (r.Table.Columns.Contains("severity"))
        SetIfExists(e, "Severity", r["severity"]?.ToString());

    if (r.Table.Columns.Contains("processed") && r["processed"] != DBNull.Value)
        SetIfExists(e, "Processed", Convert.ToBoolean(r["processed"]));

    return e!;
}

#endregion

#region === 21 · INCIDENTS & AUDIT =========================================

public async Task<int> CreateIncidentAsync(Incident inc, CancellationToken token = default)
{
    // pull via tolerant getters (model may not expose these members)
    var detectedAt   = TryGet<DateTime>(inc, "DetectedAt");
    var reportedById = TryGet<int>(inc, "ReportedById") ?? TryGet<int>(inc, "ReportedBy");
    var severity     = TryGetString(inc, "Severity")        ?? "low";
    var title        = TryGetString(inc, "Title")           ?? string.Empty;
    var description  = TryGetString(inc, "Description");
    var actionsTaken = TryGetString(inc, "ActionsTaken");
    var followUp     = TryGetString(inc, "FollowUp");
    var note         = TryGetString(inc, "Note");
    var sourceIp     = TryGetString(inc, "SourceIp");

    const string sql = @"
INSERT INTO incident_log
(detected_at, reported_by, severity, title, description, resolved, actions_taken, follow_up, note, source_ip)
VALUES (@det,@by,@sev,@title,@desc,0,@act,@fu,@note,@ip)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@det",   (object?)detectedAt   ?? DBNull.Value),
        new MySqlParameter("@by",    (object?)reportedById ?? DBNull.Value),
        new MySqlParameter("@sev",   severity),
        new MySqlParameter("@title", title),
        new MySqlParameter("@desc",  (object?)description  ?? DBNull.Value),
        new MySqlParameter("@act",   (object?)actionsTaken ?? DBNull.Value),
        new MySqlParameter("@fu",    (object?)followUp     ?? DBNull.Value),
        new MySqlParameter("@note",  (object?)note         ?? DBNull.Value),
        new MySqlParameter("@ip",    (object?)sourceIp     ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    var id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // High-level audit
    await LogSystemEventAsync(
        userId:      reportedById,
        eventType:   "CREATE",
        tableName:   "incident_log",
        module:      "IncidentModule",
        recordId:    id,
        description: "Incident created",
        ip:          sourceIp ?? "system",
        severity:    "audit",
        deviceInfo:  null,
        sessionId:   null
    ).ConfigureAwait(false);

    return id;
}

public async Task UpdateIncidentAsync(Incident inc, CancellationToken token = default)
{
    // safe reflection pull
    var id           = TryGet<int>(inc, "Id") ?? 0;
    var severity     = TryGetString(inc, "Severity")        ?? "low";
    var title        = TryGetString(inc, "Title")           ?? string.Empty;
    var description  = TryGetString(inc, "Description");
    var actionsTaken = TryGetString(inc, "ActionsTaken");
    var followUp     = TryGetString(inc, "FollowUp");
    var note         = TryGetString(inc, "Note");
    var sourceIp     = TryGetString(inc, "SourceIp");

    const string sql = @"
UPDATE incident_log SET
severity=@sev, title=@title, description=@desc, actions_taken=@act, follow_up=@fu, note=@note
WHERE id=@id";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@sev",   severity),
        new MySqlParameter("@title", title),
        new MySqlParameter("@desc",  (object?)description  ?? DBNull.Value),
        new MySqlParameter("@act",   (object?)actionsTaken ?? DBNull.Value),
        new MySqlParameter("@fu",    (object?)followUp     ?? DBNull.Value),
        new MySqlParameter("@note",  (object?)note         ?? DBNull.Value),
        new MySqlParameter("@id",    id)
    }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId:      null,
        eventType:   "UPDATE",
        tableName:   "incident_log",
        module:      "IncidentModule",
        recordId:    id,
        description: "Incident updated",
        ip:          sourceIp ?? "system",
        severity:    "audit",
        deviceInfo:  null,
        sessionId:   null
    ).ConfigureAwait(false);
}

public async Task ResolveIncidentAsync(
    int incidentId, int resolvedBy, string? actionsTaken = null, string? note = null, CancellationToken token = default)
{
    const string sql = @"
UPDATE incident_log
SET resolved=1, resolved_at=NOW(), resolved_by=@by,
    actions_taken=COALESCE(@act,actions_taken),
    note=COALESCE(@note,note)
WHERE id=@id";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@by",   resolvedBy),
        new MySqlParameter("@act",  (object?)actionsTaken ?? DBNull.Value),
        new MySqlParameter("@note", (object?)note ?? DBNull.Value),
        new MySqlParameter("@id",   incidentId)
    }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId:      resolvedBy,
        eventType:   "RESOLVE",
        tableName:   "incident_log",
        module:      "IncidentModule",
        recordId:    incidentId,
        description: "Incident resolved",
        ip:          "system",
        severity:    "audit",
        deviceInfo:  null,
        sessionId:   null
    ).ConfigureAwait(false);
}

// NOTE: InsertIncidentAuditAsync and ParseIncident are defined elsewhere (Section 06).

public async Task<List<Incident>> GetIncidentsAsync(
    string? severity = null,
    bool? resolved = null,
    DateTime? from = null,
    DateTime? to = null,
    int limit = 200,
    int offset = 0,
    CancellationToken token = default)
{
    // guardrails
    if (limit <= 0) limit = 50;
    if (limit > 1000) limit = 1000;
    if (offset < 0) offset = 0;

    var sql = new StringBuilder("SELECT * FROM incident_log WHERE 1=1");
    var pars = new List<MySqlParameter>();
    if (!string.IsNullOrWhiteSpace(severity)) { sql.Append(" AND severity=@s"); pars.Add(new("@s", severity)); }
    if (resolved.HasValue)                    { sql.Append(" AND resolved=@r"); pars.Add(new("@r", resolved.Value)); }
    if (from.HasValue)                        { sql.Append(" AND detected_at>=@f"); pars.Add(new("@f", from.Value)); }
    if (to.HasValue)                          { sql.Append(" AND detected_at<=@t"); pars.Add(new("@t", to.Value)); }

    sql.Append(" ORDER BY detected_at DESC LIMIT @lim OFFSET @off");
    pars.Add(new MySqlParameter("@lim", limit));
    pars.Add(new MySqlParameter("@off", offset));

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<Incident>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIncident(r)); // uses the Section 06 parser
    return list;
}

#endregion
#region === 22 · SENSITIVE DATA ACCESS LOG ==================================

/// <summary>
/// Insert a sensitive data access audit row.
/// </summary>
public async Task<int> InsertSensitiveAccessLogAsync(
    int userId,
    string tableName,
    int recordId,
    string? fieldName,
    string accessType,
    string? sourceIp,
    string? purpose,
    int? approvedBy,
    string? note,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO sensitive_data_access_log
(user_id, table_name, record_id, field_name, access_type, source_ip, purpose, approved_by, note)
VALUES (@uid,@tbl,@rid,@field,@type,@ip,@purpose,@appr,@note)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid",    userId),
        new MySqlParameter("@tbl",    tableName),
        new MySqlParameter("@rid",    recordId),
        new MySqlParameter("@field",  fieldName ?? (object)DBNull.Value),
        new MySqlParameter("@type",   accessType),
        new MySqlParameter("@ip",     sourceIp ?? (object)DBNull.Value),
        new MySqlParameter("@purpose",purpose ?? (object)DBNull.Value),
        new MySqlParameter("@appr",   (object?)approvedBy ?? DBNull.Value),
        new MySqlParameter("@note",   note ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(
        await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

/// <summary>
/// Convenience overload when the caller has the record id as a string.
/// </summary>
public Task<int> InsertSensitiveAccessLogAsync(
    int userId,
    string tableName,
    string recordId,
    string? fieldName,
    string accessType,
    string? sourceIp,
    string? purpose,
    int? approvedBy,
    string? note,
    CancellationToken token = default)
{
    int rid; int.TryParse(recordId, out rid);
    return InsertSensitiveAccessLogAsync(userId, tableName, rid, fieldName, accessType, sourceIp, purpose, approvedBy, note, token);
}

/// <summary>
/// Query sensitive access log with server-side filters. Pass nulls to skip filters.
/// </summary>
public async Task<List<SensitiveDataAccessLog>> GetSensitiveAccessLogsAsync(
    int? userId = null,
    string? tableName = null,
    int? recordId = null,
    string? fieldName = null,
    string? accessType = null,
    DateTime? from = null,
    DateTime? to = null,
    int limit = 500,
    int offset = 0,
    CancellationToken token = default)
{
    if (limit <= 0) limit = 100;
    if (limit > 2000) limit = 2000;
    if (offset < 0) offset = 0;

    var sql = new StringBuilder("SELECT * FROM sensitive_data_access_log WHERE 1=1");
    var pars = new List<MySqlParameter>();

    if (userId.HasValue)      { sql.Append(" AND user_id=@u");         pars.Add(new("@u", userId.Value)); }
    if (!string.IsNullOrWhiteSpace(tableName))
                               { sql.Append(" AND table_name=@t");      pars.Add(new("@t", tableName)); }
    if (recordId.HasValue)    { sql.Append(" AND record_id=@r");       pars.Add(new("@r", recordId.Value)); }
    if (!string.IsNullOrWhiteSpace(fieldName))
                               { sql.Append(" AND field_name=@f");      pars.Add(new("@f", fieldName)); }
    if (!string.IsNullOrWhiteSpace(accessType))
                               { sql.Append(" AND access_type=@a");     pars.Add(new("@a", accessType)); }
    if (from.HasValue)        { sql.Append(" AND (accessed_at>=@from OR created_at>=@from OR timestamp>=@from)"); pars.Add(new("@from", from.Value)); }
    if (to.HasValue)          { sql.Append(" AND (accessed_at<=@to OR created_at<=@to OR timestamp<=@to)");       pars.Add(new("@to", to.Value)); }

    sql.Append(" ORDER BY COALESCE(accessed_at, created_at, timestamp) DESC, id DESC LIMIT @lim OFFSET @off");
    pars.Add(new MySqlParameter("@lim", limit));
    pars.Add(new MySqlParameter("@off", offset));

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<SensitiveDataAccessLog>();
    foreach (DataRow r in dt.Rows) list.Add(ParseSensitiveAccessLog(r));
    return list;
}

/// <summary>
/// DataRow → SensitiveDataAccessLog (schema-tolerant).
/// Only sets properties that exist on the model.
/// </summary>
private static SensitiveDataAccessLog ParseSensitiveAccessLog(DataRow r)
{
    var s = Activator.CreateInstance<SensitiveDataAccessLog>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(s, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value)
        SetIfExists(s, "UserId", Convert.ToInt32(r["user_id"]));
    if (r.Table.Columns.Contains("table_name"))
        SetIfExists(s, "TableName", r["table_name"]?.ToString());
    if (r.Table.Columns.Contains("record_id") && r["record_id"] != DBNull.Value)
        SetIfExists(s, "RecordId", Convert.ToInt32(r["record_id"]));
    if (r.Table.Columns.Contains("field_name"))
        SetIfExists(s, "FieldName", r["field_name"]?.ToString());
    if (r.Table.Columns.Contains("access_type"))
        SetIfExists(s, "AccessType", r["access_type"]?.ToString());
    if (r.Table.Columns.Contains("source_ip"))
        SetIfExists(s, "SourceIp", r["source_ip"]?.ToString());
    if (r.Table.Columns.Contains("purpose"))
        SetIfExists(s, "Purpose", r["purpose"]?.ToString());
    if (r.Table.Columns.Contains("approved_by") && r["approved_by"] != DBNull.Value)
        SetIfExists(s, "ApprovedBy", Convert.ToInt32(r["approved_by"]));
    if (r.Table.Columns.Contains("note"))
        SetIfExists(s, "Note", r["note"]?.ToString());

    // Timestamp field can vary by schema; map whichever exists.
    if (r.Table.Columns.Contains("accessed_at") && r["accessed_at"] != DBNull.Value)
        SetIfExists(s, "AccessedAt", Convert.ToDateTime(r["accessed_at"]));
    else if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
        SetIfExists(s, "AccessedAt", Convert.ToDateTime(r["created_at"]));
    else if (r.Table.Columns.Contains("timestamp") && r["timestamp"] != DBNull.Value)
        SetIfExists(s, "AccessedAt", Convert.ToDateTime(r["timestamp"]));

    return s!;
}

#endregion
#region === 23 · DELETE LOG =================================================

public async Task<int> InsertDeleteLogAsync(
    int deletedBy,
    string tableName,
    int recordId,
    string deleteType,   // soft | hard
    string? reason,
    bool recoverable,
    string? backupFile,
    string? sourceIp,
    string? note,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO delete_log
(deleted_by, table_name, record_id, delete_type, reason, recoverable, backup_file, source_ip, note)
VALUES (@by,@tbl,@rid,@type,@reason,@rec,@file,@ip,@note)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@by",     deletedBy),
        new MySqlParameter("@tbl",    tableName),
        new MySqlParameter("@rid",    recordId),
        new MySqlParameter("@type",   deleteType),
        new MySqlParameter("@reason", reason ?? (object)DBNull.Value),
        new MySqlParameter("@rec",    recoverable),
        new MySqlParameter("@file",   backupFile ?? (object)DBNull.Value),
        new MySqlParameter("@ip",     sourceIp ?? (object)DBNull.Value),
        new MySqlParameter("@note",   note ?? (object)DBNull.Value)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(
        await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

/// <summary>
/// Convenience overload when the caller has the record id as a string.
/// </summary>
public Task<int> InsertDeleteLogAsync(
    int deletedBy,
    string tableName,
    string recordId,
    string deleteType,
    string? reason,
    bool recoverable,
    string? backupFile,
    string? sourceIp,
    string? note,
    CancellationToken token = default)
{
    int rid; int.TryParse(recordId, out rid);
    return InsertDeleteLogAsync(deletedBy, tableName, rid, deleteType, reason, recoverable, backupFile, sourceIp, note, token);
}

/// <summary>
/// Query delete_log with server-side filters. Pass nulls to skip filters.
/// </summary>
public async Task<List<DeleteLog>> GetDeleteLogsAsync(
    int? deletedBy = null,
    string? tableName = null,
    int? recordId = null,
    string? deleteType = null,
    bool? recoverable = null,
    DateTime? from = null,
    DateTime? to = null,
    int limit = 500,
    int offset = 0,
    CancellationToken token = default)
{
    if (limit <= 0) limit = 100;
    if (limit > 2000) limit = 2000;
    if (offset < 0) offset = 0;

    var sql = new StringBuilder("SELECT * FROM delete_log WHERE 1=1");
    var pars = new List<MySqlParameter>();

    if (deletedBy.HasValue)          { sql.Append(" AND deleted_by=@by");       pars.Add(new MySqlParameter("@by", deletedBy.Value)); }
    if (!string.IsNullOrWhiteSpace(tableName))
                                     { sql.Append(" AND table_name=@tbl");      pars.Add(new MySqlParameter("@tbl", tableName)); }
    if (recordId.HasValue)           { sql.Append(" AND record_id=@rid");       pars.Add(new MySqlParameter("@rid", recordId.Value)); }
    if (!string.IsNullOrWhiteSpace(deleteType))
                                     { sql.Append(" AND delete_type=@type");    pars.Add(new MySqlParameter("@type", deleteType)); }
    if (recoverable.HasValue)        { sql.Append(" AND recoverable=@rec");     pars.Add(new MySqlParameter("@rec", recoverable.Value)); }
    if (from.HasValue)               { sql.Append(" AND (deleted_at>=@from OR created_at>=@from OR timestamp>=@from)"); pars.Add(new MySqlParameter("@from", from.Value)); }
    if (to.HasValue)                 { sql.Append(" AND (deleted_at<=@to OR created_at<=@to OR timestamp<=@to)");       pars.Add(new MySqlParameter("@to", to.Value)); }

    sql.Append(" ORDER BY COALESCE(deleted_at, created_at, timestamp) DESC, id DESC LIMIT @lim OFFSET @off");
    pars.Add(new MySqlParameter("@lim", limit));
    pars.Add(new MySqlParameter("@off", offset));

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<DeleteLog>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDeleteLog(r));
    return list;
}

/// <summary>
/// DataRow → DeleteLog (schema tolerant).
/// </summary>
private static DeleteLog ParseDeleteLog(DataRow r)
{
    var d = Activator.CreateInstance<DeleteLog>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(d, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("deleted_by") && r["deleted_by"] != DBNull.Value)
        SetIfExists(d, "DeletedBy", Convert.ToInt32(r["deleted_by"]));
    if (r.Table.Columns.Contains("table_name"))
        SetIfExists(d, "TableName", r["table_name"]?.ToString());
    if (r.Table.Columns.Contains("record_id") && r["record_id"] != DBNull.Value)
        SetIfExists(d, "RecordId", Convert.ToInt32(r["record_id"]));
    if (r.Table.Columns.Contains("delete_type"))
        SetIfExists(d, "DeleteType", r["delete_type"]?.ToString());
    if (r.Table.Columns.Contains("reason"))
        SetIfExists(d, "Reason", r["reason"]?.ToString());
    if (r.Table.Columns.Contains("recoverable") && r["recoverable"] != DBNull.Value)
        SetIfExists(d, "Recoverable", Convert.ToBoolean(r["recoverable"]));
    if (r.Table.Columns.Contains("backup_file"))
        SetIfExists(d, "BackupFile", r["backup_file"]?.ToString());
    if (r.Table.Columns.Contains("source_ip"))
        SetIfExists(d, "SourceIp", r["source_ip"]?.ToString());
    if (r.Table.Columns.Contains("note"))
        SetIfExists(d, "Note", r["note"]?.ToString());

    // timestamp column name may vary; map whichever exists
    if (r.Table.Columns.Contains("deleted_at") && r["deleted_at"] != DBNull.Value)
        SetIfExists(d, "DeletedAt", Convert.ToDateTime(r["deleted_at"]));
    else if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
        SetIfExists(d, "DeletedAt", Convert.ToDateTime(r["created_at"]));
    else if (r.Table.Columns.Contains("timestamp") && r["timestamp"] != DBNull.Value)
        SetIfExists(d, "DeletedAt", Convert.ToDateTime(r["timestamp"]));

    return d!;
}

#endregion
#region === 24 · CONFIG CHANGE LOG =========================================

        public async Task<int> InsertConfigChangeAsync(
            int? changedBy,
            string configName,
            string? oldValue,
            string? newValue,
            string changeType,
            string? note,
            string? sourceIp,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO config_change_log
(changed_by, config_name, old_value, new_value, change_type, note, source_ip)
VALUES (@by,@name,@old,@new,@type,@note,@ip)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@by",   (object?)changedBy ?? DBNull.Value),
                new MySqlParameter("@name", configName),
                new MySqlParameter("@old",  oldValue ?? (object)DBNull.Value),
                new MySqlParameter("@new",  newValue ?? (object)DBNull.Value),
                new MySqlParameter("@type", changeType),
                new MySqlParameter("@note", note ?? (object)DBNull.Value),
                new MySqlParameter("@ip",   sourceIp ?? (object)DBNull.Value)
            }, token).ConfigureAwait(false);

            return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        #endregion
#region === 25 · INTEGRATIONS ==============================================

public async Task<int> InsertIntegrationLogAsync(
    string systemName,
    string apiEndpoint,
    string? requestJson,
    string? responseJson,
    int? statusCode,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO integration_log
(system_name, api_endpoint, request_json, response_json, status_code, processed)
VALUES (@sys,@ep,@req,@res,@code,0)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@sys",  systemName),
        new MySqlParameter("@ep",   apiEndpoint),
        new MySqlParameter("@req",  requestJson  ?? (object)DBNull.Value),
        new MySqlParameter("@res",  responseJson ?? (object)DBNull.Value),
        new MySqlParameter("@code", (object?)statusCode ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(
        await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

public async Task MarkIntegrationProcessedAsync(int id, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE integration_log SET processed=1, processed_at=NOW() WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
}

/// <summary>
/// Batch mark multiple integration_log rows as processed/unprocessed.
/// </summary>
public async Task MarkIntegrationsProcessedAsync(IEnumerable<int> ids, bool processed = true, CancellationToken token = default)
{
    var idArray = (ids ?? Array.Empty<int>()).ToArray();
    if (idArray.Length == 0) return;

    var sb = new StringBuilder();
    var pars = new List<MySqlParameter>();
    for (int i = 0; i < idArray.Length; i++)
    {
        if (i > 0) sb.Append(',');
        string pn = "@p" + i.ToString();
        sb.Append(pn);
        pars.Add(new MySqlParameter(pn, idArray[i]));
    }
    pars.Add(new MySqlParameter("@proc", processed));

    string sql = $"UPDATE integration_log SET processed=@proc, processed_at=CASE WHEN @proc=1 THEN NOW() ELSE processed_at END WHERE id IN ({sb})";
    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
}

/// <summary>
/// Query integration_log with server-side filters (pass nulls to skip).
/// </summary>
public async Task<List<IntegrationLog>> GetIntegrationLogsAsync(
    string? systemName = null,
    string? apiEndpoint = null,
    int? statusCode = null,
    bool? processed = null,
    DateTime? from = null,
    DateTime? to = null,
    int limit = 500,
    int offset = 0,
    CancellationToken token = default)
{
    if (limit <= 0) limit = 100;
    if (limit > 2000) limit = 2000;
    if (offset < 0) offset = 0;

    var sql = new StringBuilder("SELECT * FROM integration_log WHERE 1=1");
    var pars = new List<MySqlParameter>();

    if (!string.IsNullOrWhiteSpace(systemName))
    {
        sql.Append(" AND system_name=@sys");
        pars.Add(new MySqlParameter("@sys", systemName));
    }
    if (!string.IsNullOrWhiteSpace(apiEndpoint))
    {
        sql.Append(" AND api_endpoint=@ep");
        pars.Add(new MySqlParameter("@ep", apiEndpoint));
    }
    if (statusCode.HasValue)
    {
        sql.Append(" AND status_code=@code");
        pars.Add(new MySqlParameter("@code", statusCode.Value));
    }
    if (processed.HasValue)
    {
        sql.Append(" AND processed=@proc");
        pars.Add(new MySqlParameter("@proc", processed.Value));
    }
    if (from.HasValue)
    {
        sql.Append(" AND (created_at>=@from OR timestamp>=@from)");
        pars.Add(new MySqlParameter("@from", from.Value));
    }
    if (to.HasValue)
    {
        sql.Append(" AND (created_at<=@to OR timestamp<=@to)");
        pars.Add(new MySqlParameter("@to", to.Value));
    }

    sql.Append(" ORDER BY COALESCE(created_at, timestamp) DESC, id DESC LIMIT @lim OFFSET @off");
    pars.Add(new MySqlParameter("@lim", limit));
    pars.Add(new MySqlParameter("@off", offset));

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);

    var list = new List<IntegrationLog>();
    foreach (DataRow r in dt.Rows) list.Add(ParseIntegrationLog(r));
    return list;
}

/// <summary>
/// DataRow → IntegrationLog (schema tolerant).
/// Only sets properties that exist on the model using SetIfExists.
/// </summary>
private static IntegrationLog ParseIntegrationLog(DataRow r)
{
    var x = Activator.CreateInstance<IntegrationLog>();

    if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)
        SetIfExists(x, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("system_name"))
        SetIfExists(x, "SystemName", r["system_name"]?.ToString());
    if (r.Table.Columns.Contains("api_endpoint"))
        SetIfExists(x, "ApiEndpoint", r["api_endpoint"]?.ToString());
    if (r.Table.Columns.Contains("request_json"))
        SetIfExists(x, "RequestJson", r["request_json"]?.ToString());
    if (r.Table.Columns.Contains("response_json"))
        SetIfExists(x, "ResponseJson", r["response_json"]?.ToString());
    if (r.Table.Columns.Contains("status_code") && r["status_code"] != DBNull.Value)
        SetIfExists(x, "StatusCode", Convert.ToInt32(r["status_code"]));
    if (r.Table.Columns.Contains("processed") && r["processed"] != DBNull.Value)
        SetIfExists(x, "Processed", Convert.ToBoolean(r["processed"]));
    if (r.Table.Columns.Contains("created_at") && r["created_at"] != DBNull.Value)
        SetIfExists(x, "CreatedAt", Convert.ToDateTime(r["created_at"]));
    else if (r.Table.Columns.Contains("timestamp") && r["timestamp"] != DBNull.Value)
        SetIfExists(x, "CreatedAt", Convert.ToDateTime(r["timestamp"]));
    if (r.Table.Columns.Contains("processed_at") && r["processed_at"] != DBNull.Value)
        SetIfExists(x, "ProcessedAt", Convert.ToDateTime(r["processed_at"]));
    if (r.Table.Columns.Contains("error_message"))
        SetIfExists(x, "ErrorMessage", r["error_message"]?.ToString());

    return x!;
}

#endregion
#region === 26 · SCHEDULED JOBS ============================================

/// <summary>
/// Inserts or updates a scheduled job using schema-tolerant reflection.
/// Existing implementation preserved; called by the wrappers below.
/// </summary>
public async Task<int> UpsertScheduledJobAsync(
    object j, bool update, int actorUserId, string? ip = null, string? device = null, string? sessionId = null,
    CancellationToken token = default)
{
    // Pull values via safe reflection so older POCOs still compile
    var name              = TryGetString(j, "Name") ?? string.Empty;
    var jobType           = TryGetString(j, "JobType") ?? string.Empty;
    var entityType        = TryGetString(j, "EntityType");
    var entityId          = TryGet<int>(j, "EntityId");
    var status            = TryGetString(j, "Status") ?? "scheduled";
    var nextDue           = TryGet<DateTime>(j, "NextDue");
    var recurrence        = TryGetString(j, "RecurrencePattern");
    var cron              = TryGetString(j, "CronExpression");
    var lastExecuted      = TryGet<DateTime>(j, "LastExecuted");
    var lastResult        = TryGetString(j, "LastResult");
    var escalationLevel   = TryGet<int>(j, "EscalationLevel");
    var escalationNote    = TryGetString(j, "EscalationNote");
    var chainJobId        = TryGet<int>(j, "ChainJobId");
    var isCritical        = TryGet<bool>(j, "IsCritical");
    var needsAck          = TryGet<bool>(j, "NeedsAcknowledgment");
    var alertOnFailure    = TryGet<bool>(j, "AlertOnFailure");
    var retries           = TryGet<int>(j, "Retries");
    var maxRetries        = TryGet<int>(j, "MaxRetries");
    var lastError         = TryGetString(j, "LastError");
    var iotDeviceId       = TryGetString(j, "IotDeviceId");
    var extraParams       = TryGetString(j, "ExtraParamsJson") ?? TryGetString(j, "ExtraParams"); // tolerate both names
    var digitalSignature  = TryGetString(j, "DigitalSignature");
    var comment           = TryGetString(j, "Comment");

    string sql = !update
        ? @"INSERT INTO scheduled_jobs
(name, job_type, entity_type, entity_id, status, next_due, recurrence_pattern, cron_expression, last_executed,
 last_result, escalation_level, escalation_note, chain_job_id, is_critical, needs_acknowledgment,
 alert_on_failure, retries, max_retries, last_error, iot_device_id, extra_params, created_by, digital_signature,
 device_info, session_id, ip_address, comment)
VALUES
(@name,@jtype,@etype,@eid,@status,@due,@recur,@cron,@lastExec,@lastRes,@escLvl,@escNote,@chain,@crit,@ack,
@alert,@retries,@max,@err,@iot,@extra,@by,@sig,@dev,@sess,@ip,@comment)"
        : @"UPDATE scheduled_jobs SET
name=@name, job_type=@jtype, entity_type=@etype, entity_id=@eid, status=@status, next_due=@due,
recurrence_pattern=@recur, cron_expression=@cron, last_executed=@lastExec, last_result=@lastRes,
escalation_level=@escLvl, escalation_note=@escNote, chain_job_id=@chain, is_critical=@crit,
needs_acknowledgment=@ack, alert_on_failure=@alert, retries=@retries, max_retries=@max, last_error=@err,
iot_device_id=@iot, extra_params=@extra, last_modified=NOW(), last_modified_by=@by, digital_signature=@sig,
device_info=@dev, session_id=@sess, ip_address=@ip, comment=@comment
WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@name",   name),
        new("@jtype",  jobType),
        new("@etype",  (object?)entityType ?? DBNull.Value),
        new("@eid",    (object?)entityId ?? DBNull.Value),
        new("@status", status),
        new("@due",    (object?)nextDue ?? DBNull.Value),
        new("@recur",  (object?)recurrence ?? DBNull.Value),
        new("@cron",   (object?)cron ?? DBNull.Value),
        new("@lastExec", (object?)lastExecuted ?? DBNull.Value),
        new("@lastRes", (object?)lastResult ?? DBNull.Value),
        new("@escLvl", (object?)escalationLevel ?? DBNull.Value),
        new("@escNote", (object?)escalationNote ?? DBNull.Value),
        new("@chain",  (object?)chainJobId ?? DBNull.Value),
        new("@crit",   (object?)isCritical ?? DBNull.Value),
        new("@ack",    (object?)needsAck ?? DBNull.Value),
        new("@alert",  (object?)alertOnFailure ?? DBNull.Value),
        new("@retries",(object?)retries ?? DBNull.Value),
        new("@max",    (object?)maxRetries ?? DBNull.Value),
        new("@err",    (object?)lastError ?? DBNull.Value),
        new("@iot",    (object?)iotDeviceId ?? DBNull.Value),
        new("@extra",  (object?)extraParams ?? DBNull.Value),
        new("@by",     actorUserId),
        new("@sig",    (object?)digitalSignature ?? DBNull.Value),
        new("@dev",    (object?)device ?? DBNull.Value),
        new("@sess",   (object?)sessionId ?? DBNull.Value),
        new("@ip",     (object?)ip ?? DBNull.Value),
        new("@comment",(object?)comment ?? DBNull.Value)
    };

    if (update)
    {
        // tolerate either Id or ScheduledJobId naming
        var idVal = TryGet<int>(j, "Id") ?? TryGet<int>(j, "ScheduledJobId") ?? 0;
        pars.Add(new MySqlParameter("@id", idVal));
    }

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    int id = update
        ? (TryGet<int>(j, "Id") ?? TryGet<int>(j, "ScheduledJobId") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
    return id;
}

/// <summary>
/// Returns a single scheduled job by primary key.
/// </summary>
public async Task<ScheduledJob?> GetScheduledJobAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM scheduled_jobs WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    return dt.Rows.Count == 0 ? null : ParseScheduledJob(dt.Rows[0]);
}

/// <summary>
/// Returns all scheduled jobs (full list for UI), newest/nearest due first.
/// </summary>
public async Task<List<ScheduledJob>> GetAllScheduledJobsFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM scheduled_jobs ORDER BY COALESCE(next_due, '9999-12-31'), name", null, token).ConfigureAwait(false);
    var list = new List<ScheduledJob>();
    foreach (DataRow r in dt.Rows) list.Add(ParseScheduledJob(r));
    return list;
}

/// <summary>
/// Returns due jobs (view driven).
/// </summary>
public async Task<List<ScheduledJob>> GetDueScheduledJobsAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM vw_scheduled_jobs_due ORDER BY next_due", null, token).ConfigureAwait(false);
    var list = new List<ScheduledJob>();
    foreach (DataRow r in dt.Rows) list.Add(ParseScheduledJob(r));
    return list;
}

/// <summary>
/// Sets status/result/error/last-executed, plus optional counters/escalation.
/// </summary>
public async Task SetScheduledJobStatusAsync(
    int id, string status, string? lastResult = null, string? lastError = null, DateTime? lastExecuted = null, int? retries = null,
    int? escalationLevel = null, string? escalationNote = null, CancellationToken token = default)
{
    var sql = new StringBuilder(@"
UPDATE scheduled_jobs SET status=@st, last_result=COALESCE(@res,last_result), last_error=COALESCE(@err,last_error), last_executed=COALESCE(@lex,last_executed)");
    var pars = new List<MySqlParameter>
    {
        new("@st", status),
        new("@res", (object?)lastResult ?? DBNull.Value),
        new("@err", (object?)lastError ?? DBNull.Value),
        new("@lex", (object?)lastExecuted ?? DBNull.Value),
        new("@id", id)
    };
    if (retries.HasValue)         { sql.Append(", retries=@r");          pars.Add(new("@r", retries.Value)); }
    if (escalationLevel.HasValue) { sql.Append(", escalation_level=@e");  pars.Add(new("@e", escalationLevel.Value)); }
    if (escalationNote != null)   { sql.Append(", escalation_note=@en");  pars.Add(new("@en", escalationNote)); }
    sql.Append(" WHERE id=@id");

    await ExecuteNonQueryAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
}

/// <summary>
/// Acknowledges a pending job (short overload).
/// </summary>
public async Task AcknowledgeScheduledJobAsync(int id, int userId, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(@"
UPDATE scheduled_jobs
SET needs_acknowledgment=0, acknowledged_by=@u, acknowledged_at=NOW(), status=IF(status='pending_ack','scheduled',status)
WHERE id=@id",
        new[] { new MySqlParameter("@u", userId), new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    // explicit business-action audit
    await InsertScheduledJobAuditAsync(id, userId, "ACKNOWLEDGE", null, "Acknowledged", null, null, null, token).ConfigureAwait(false);
}

/// <summary>
/// Adds a job (wrapper around <see cref="UpsertScheduledJobAsync"/>).
/// </summary>
public async Task AddScheduledJobAsync(ScheduledJob job, CancellationToken token = default)
{
    // If you have a numeric CreatedById on your model, you can reflect it; otherwise 0 is fine.
    int actorUserId = TryGet<int>(job, "CreatedById") ?? 0;
    await UpsertScheduledJobAsync(job, update: false, actorUserId: actorUserId, token: token).ConfigureAwait(false);
}

/// <summary>
/// Updates a job and writes sys/audit logs.
/// </summary>
public async Task UpdateScheduledJobAsync(ScheduledJob job, int actorUserId, string ip, string device, string sessionId, CancellationToken token = default)
{
    await UpsertScheduledJobAsync(job, update: true, actorUserId: actorUserId, ip: ip, device: device, sessionId: sessionId, token: token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "JOB_UPDATE", "scheduled_jobs", "Scheduler", TryGet<int>(job, "Id"), $"Job '{TryGetString(job, "Name")}' updated.", ip, "audit", device, sessionId).ConfigureAwait(false);
    await LogScheduledJobAuditAsync(job, "UPDATE", ip, device, sessionId, TryGetString(job, "DigitalSignature"), token).ConfigureAwait(false);
}

/// <summary>
/// Deletes a job (hard delete if soft flag absent) and logs audit.
/// </summary>
public async Task DeleteScheduledJobAsync(int id, string ip, string device, string sessionId, CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM scheduled_jobs WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    await LogSystemEventAsync(null, "JOB_DELETE", "scheduled_jobs", "Scheduler", id, "Scheduled job deleted", ip, "audit", device, sessionId).ConfigureAwait(false);
    // Audit table is optional
    await InsertScheduledJobAuditAsync(id, null, "DELETE", null, "Deleted", ip, device, sessionId, token).ConfigureAwait(false);
}

/// <summary>
/// Acknowledges a job (overload with full forensic context).
/// </summary>
public async Task AcknowledgeScheduledJobAsync(int id, int userId, string ip, string device, string sessionId, CancellationToken token = default)
{
    await AcknowledgeScheduledJobAsync(id, userId, token).ConfigureAwait(false);
    await LogSystemEventAsync(userId, "JOB_ACK", "scheduled_jobs", "Scheduler", id, "Job acknowledged", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>
/// Executes a job (simple handler) – marks completed & schedules next based on a few common recurrences.
/// </summary>
public async Task ExecuteScheduledJobAsync(int id, int actorUserId, string ip, string device, string sessionId, CancellationToken token = default)
{
    // Basic finalize
    await ExecuteNonQueryAsync(@"
UPDATE scheduled_jobs
SET status='completed',
    last_executed=NOW(),
    last_result=COALESCE(last_result,'OK'),
    retries=0,
    next_due = CASE
                 WHEN recurrence_pattern='Daily'   THEN DATE_ADD(NOW(), INTERVAL 1 DAY)
                 WHEN recurrence_pattern='Weekly'  THEN DATE_ADD(NOW(), INTERVAL 1 WEEK)
                 WHEN recurrence_pattern='Monthly' THEN DATE_ADD(NOW(), INTERVAL 1 MONTH)
                 ELSE next_due
               end
WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await InsertScheduledJobAuditAsync(id, actorUserId, "EXECUTE", null, "Executed", ip, device, sessionId, token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "JOB_EXECUTE", "scheduled_jobs", "Scheduler", id, "Job executed", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>
/// Exports scheduled jobs – logs to export_print_log and returns the file path.
/// </summary>
public async Task<string> ExportScheduledJobsAsync(IEnumerable<ScheduledJob> rows, string ip, string device, string sessionId, string format = "csv", CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
    string filePath = $"/export/scheduled_jobs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (NULL,@fmt,'scheduled_jobs',@filter,@path,@ip,'Scheduled jobs export')",
        new[]
        {
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(null, "JOB_EXPORT", "scheduled_jobs", "Scheduler", null, $"Exported {(rows?.Count() ?? 0)} jobs to {filePath}.", ip, "audit", device, sessionId).ConfigureAwait(false);
    return filePath;
}

/// <summary>
/// Low-level audit helper for scheduled jobs. If <paramref name="job"/> is null,
/// writes only a high-level system event.
/// </summary>
public async Task LogScheduledJobAuditAsync(
    ScheduledJob? job,
    string action,
    string ip,
    string device,
    string? sessionId,
    string? signature,
    CancellationToken token = default)
{
    int? jobId = job != null ? TryGet<int>(job, "Id") : null;
    string? oldVal = null; // Extend to snapshot previous state if needed
    string? newVal = job != null ? TryGetString(job, "Comment") ?? TryGetString(job, "Status") : null;

    if (jobId.HasValue && jobId.Value > 0)
    {
        await InsertScheduledJobAuditAsync(jobId.Value, null, action, oldVal, newVal, ip, device, sessionId, token).ConfigureAwait(false);
    }

    await LogSystemEventAsync(null, $"JOB_{action}", "scheduled_jobs", "Scheduler", jobId, $"Action {action} {(job != null ? $"for '{TryGetString(job, "Name")}'" : string.Empty)}", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>
/// Inserts an audit row into <c>scheduled_job_audit_log</c>.
/// </summary>
public async Task<int> InsertScheduledJobAuditAsync(
    int scheduledJobId,
    int? userId,
    string action,
    string? oldValue,
    string? newValue,
    string? sourceIp,
    string? deviceInfo,
    string? sessionId,
    CancellationToken token = default)
{
    const string sql = @"
INSERT INTO scheduled_job_audit_log
(scheduled_job_id, user_id, action, old_value, new_value, source_ip, device_info, session_id)
VALUES (@id,@uid,@act,@old,@new,@ip,@dev,@sess)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@id",  scheduledJobId),
        new MySqlParameter("@uid", (object?)userId ?? DBNull.Value),
        new MySqlParameter("@act", action),
        new MySqlParameter("@old", (object?)oldValue ?? DBNull.Value),
        new MySqlParameter("@new", (object?)newValue ?? DBNull.Value),
        new MySqlParameter("@ip",  (object?)sourceIp ?? DBNull.Value),
        new MySqlParameter("@dev", (object?)deviceInfo ?? DBNull.Value),
        new MySqlParameter("@sess",(object?)sessionId ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

// ------------------------- schema-tolerant parser --------------------------

/// <summary>DataRow → <see cref="ScheduledJob"/> (schema tolerant).</summary>
private static ScheduledJob ParseScheduledJob(DataRow r)
{
    var j = Activator.CreateInstance<ScheduledJob>();

    bool Has(string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value;

    SetIfExists(j, "Id",               Convert.ToInt32(r["id"]));
    if (Has("name"))                   SetIfExists(j, "Name", r["name"]?.ToString());
    if (Has("job_type"))               SetIfExists(j, "JobType", r["job_type"]?.ToString());
    if (Has("entity_type"))            SetIfExists(j, "EntityType", r["entity_type"]?.ToString());
    if (Has("entity_id"))              SetIfExists(j, "EntityId", Convert.ToInt32(r["entity_id"]));
    if (Has("status"))                 SetIfExists(j, "Status", r["status"]?.ToString());
    if (Has("next_due"))               SetIfExists(j, "NextDue", Convert.ToDateTime(r["next_due"]));
    if (Has("recurrence_pattern"))     SetIfExists(j, "RecurrencePattern", r["recurrence_pattern"]?.ToString());
    if (Has("cron_expression"))        SetIfExists(j, "CronExpression", r["cron_expression"]?.ToString());
    if (Has("last_executed"))          SetIfExists(j, "LastExecuted", Convert.ToDateTime(r["last_executed"]));
    if (Has("last_result"))            SetIfExists(j, "LastResult", r["last_result"]?.ToString());
    if (Has("escalation_level"))       SetIfExists(j, "EscalationLevel", Convert.ToInt32(r["escalation_level"]));
    if (Has("escalation_note"))        SetIfExists(j, "EscalationNote", r["escalation_note"]?.ToString());
    if (Has("chain_job_id"))           SetIfExists(j, "ChainJobId", Convert.ToInt32(r["chain_job_id"]));
    if (Has("is_critical"))            SetIfExists(j, "IsCritical", Convert.ToBoolean(r["is_critical"]));
    if (Has("needs_acknowledgment"))   SetIfExists(j, "NeedsAcknowledgment", Convert.ToBoolean(r["needs_acknowledgment"]));
    if (Has("acknowledged_by"))        SetIfExists(j, "AcknowledgedBy", Convert.ToInt32(r["acknowledged_by"]));
    if (Has("acknowledged_at"))        SetIfExists(j, "AcknowledgedAt", Convert.ToDateTime(r["acknowledged_at"]));
    if (Has("alert_on_failure"))       SetIfExists(j, "AlertOnFailure", Convert.ToBoolean(r["alert_on_failure"]));
    if (Has("retries"))                SetIfExists(j, "Retries", Convert.ToInt32(r["retries"]));
    if (Has("max_retries"))            SetIfExists(j, "MaxRetries", Convert.ToInt32(r["max_retries"]));
    if (Has("last_error"))             SetIfExists(j, "LastError", r["last_error"]?.ToString());
    if (Has("iot_device_id"))          SetIfExists(j, "IotDeviceId", r["iot_device_id"]?.ToString());
    if (Has("extra_params"))           SetIfExists(j, "ExtraParamsJson", r["extra_params"]?.ToString());
    if (Has("created_by"))             SetIfExists(j, "CreatedBy", r["created_by"]?.ToString());
    if (Has("created_at"))             SetIfExists(j, "CreatedAt", Convert.ToDateTime(r["created_at"]));
    if (Has("last_modified"))          SetIfExists(j, "LastModified", Convert.ToDateTime(r["last_modified"]));
    if (Has("last_modified_by"))       SetIfExists(j, "LastModifiedBy", Convert.ToInt32(r["last_modified_by"]));
    if (Has("digital_signature"))      SetIfExists(j, "DigitalSignature", r["digital_signature"]?.ToString());
    if (Has("device_info"))            SetIfExists(j, "DeviceInfo", r["device_info"]?.ToString());
    if (Has("session_id"))             SetIfExists(j, "SessionId", r["session_id"]?.ToString());
    if (Has("ip_address"))             SetIfExists(j, "IpAddress", r["ip_address"]?.ToString());
    if (Has("comment"))                SetIfExists(j, "Comment", r["comment"]?.ToString());

    return j;
}

// === SUPPLEMENT · SCHEDULED JOBS – convenience & missing API =================



public Task<int> AddScheduledJobAsync(object job,
    int actorUserId = 1, string? ip = null, string? device = null, string? sessionId = null,
    CancellationToken token = default)
    => UpsertScheduledJobAsync(job, update: false, actorUserId: actorUserId,
                               ip: ip, device: device, sessionId: sessionId, token: token);

public Task<int> UpdateScheduledJobAsync(object job,
    int actorUserId, string? ip = null, string? device = null, string? sessionId = null,
    CancellationToken token = default)
    => UpsertScheduledJobAsync(job, update: true, actorUserId: actorUserId,
                               ip: ip, device: device, sessionId: sessionId, token: token);



public async Task<string> ExportScheduledJobsAsync(IEnumerable<ScheduledJob> rows,
    string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
{
    string filePath = $"/export/scheduled_jobs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'scheduled_jobs',@filter,@path,@ip,'Scheduled jobs export')",
        new[]
        {
            new MySqlParameter("@uid", DBNull.Value),
            new MySqlParameter("@fmt", "zip"),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip",   (object?)ip ?? DBNull.Value),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(null, "SCHED_EXPORT", "scheduled_jobs", "Scheduler",
        null, $"Exported {(rows?.Count() ?? 0)} scheduled jobs → {filePath}",
        ip, "audit", device, sessionId, token).ConfigureAwait(false);

    return filePath;
}

public Task LogScheduledJobAuditAsync(object? row, string action,
    string? ip = null, string? device = null, string? sessionId = null, string? note = null,
    CancellationToken token = default)
{
    int id = TryGet<int>(row, "Id") ?? 0;
    int? uid = TryGet<int>(row, "LastModifiedBy") ?? TryGet<int>(row, "CreatedBy");
    return InsertScheduledJobAuditAsync(id, uid, action, null, note, ip, device, sessionId, token);
}


#endregion
#region === 27 · API KEYS ===================================================

public async Task<int> InsertApiKeyAsync(ApiKey k, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO api_keys (key_value, description, owner_id, is_active)
VALUES (@key,@desc,@owner,1)";
    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@key",   k?.KeyValue ?? string.Empty),
        new MySqlParameter("@desc",  (object?)k?.Description ?? DBNull.Value),
        new MySqlParameter("@owner", (object?)k?.OwnerId ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
}

/// <summary>
/// Convenience wrapper so older callers work: toggles is_active via the unified method from Region 16.
/// </summary>
public async Task SetApiKeyActiveAsync(int id, bool active, CancellationToken token = default)
{
    // If Region 16's SetApiKeyStatusAsync exists (it does in our fixed code), prefer it.
    await SetApiKeyStatusAsync(id, active, token).ConfigureAwait(false);
}

/// <summary>Updates last_used_at on the key.</summary>
public async Task TouchApiKeyLastUsedAsync(int id, CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE api_keys SET last_used_at=NOW() WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
}

/// <summary>Lookup by the opaque key value.</summary>
public async Task<ApiKey?> GetApiKeyByValueAsync(string keyValue, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM api_keys WHERE key_value=@k LIMIT 1",
        new[] { new MySqlParameter("@k", keyValue) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseApiKey(dt.Rows[0]);
}


/// <summary>List API keys with optional filters.</summary>
public async Task<List<ApiKey>> ListApiKeysAsync(int? ownerId = null, bool? active = null, CancellationToken token = default)
{
    var sql = new StringBuilder("SELECT * FROM api_keys WHERE 1=1");
    var pars = new List<MySqlParameter>();
    if (ownerId.HasValue) { sql.Append(" AND owner_id=@o"); pars.Add(new("@o", ownerId.Value)); }
    if (active.HasValue)  { sql.Append(" AND is_active=@a"); pars.Add(new("@a", active.Value)); }
    sql.Append(" ORDER BY created_at DESC");

    var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
    var list = new List<ApiKey>();
    foreach (DataRow r in dt.Rows) list.Add(ParseApiKey(r));
    return list;
}



#endregion
#region === 28 · API AUDIT & USAGE ==========================================

        public async Task<int> InsertApiAuditLogAsync(
            int? apiKeyId, int? userId, string action, string? ip, string? requestDetails, CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO api_audit_log (api_key_id, user_id, action, ip_address, request_details)
VALUES (@kid,@uid,@act,@ip,@req)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@kid", (object?)apiKeyId ?? DBNull.Value),
                new MySqlParameter("@uid", (object?)userId ?? DBNull.Value),
                new MySqlParameter("@act", action),
                new MySqlParameter("@ip",  ip ?? (object)DBNull.Value),
                new MySqlParameter("@req", requestDetails ?? (object)DBNull.Value)
            }, token).ConfigureAwait(false);

            return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        public async Task<int> InsertApiUsageLogAsync(
            int? apiKeyId, int? userId, string endpoint, string method, string? @params,
            int responseCode, int durationMs, bool success, string? errorMessage, string? sourceIp, CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO api_usage_log
(api_key_id, user_id, endpoint, method, params, response_code, duration_ms, success, error_message, source_ip)
VALUES (@kid,@uid,@ep,@m,@p,@code,@dur,@succ,@err,@ip)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@kid",  (object?)apiKeyId ?? DBNull.Value),
                new MySqlParameter("@uid",  (object?)userId ?? DBNull.Value),
                new MySqlParameter("@ep",   endpoint),
                new MySqlParameter("@m",    method),
                new MySqlParameter("@p",    (object?)@params ?? DBNull.Value),
                new MySqlParameter("@code", responseCode),
                new MySqlParameter("@dur",  durationMs),
                new MySqlParameter("@succ", success),
                new MySqlParameter("@err",  errorMessage ?? (object)DBNull.Value),
                new MySqlParameter("@ip",   sourceIp ?? (object)DBNull.Value)
            }, token).ConfigureAwait(false);

            return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        public async Task<List<ApiUsageLog>> GetApiUsageAsync(
            DateTime? from = null, DateTime? to = null, string? endpoint = null, string? method = null, bool? success = null, int limit = 500,
            CancellationToken token = default)
        {
            var sql = new StringBuilder("SELECT * FROM api_usage_log WHERE 1=1");
            var pars = new List<MySqlParameter>();
            if (from.HasValue) { sql.Append(" AND call_time>=@f"); pars.Add(new("@f", from.Value)); }
            if (to.HasValue) { sql.Append(" AND call_time<=@t"); pars.Add(new("@t", to.Value)); }
            if (!string.IsNullOrWhiteSpace(endpoint)) { sql.Append(" AND endpoint=@e"); pars.Add(new("@e", endpoint)); }
            if (!string.IsNullOrWhiteSpace(method)) { sql.Append(" AND method=@m"); pars.Add(new("@m", method)); }
            if (success.HasValue) { sql.Append(" AND success=@s"); pars.Add(new("@s", success.Value)); }
            sql.Append(" ORDER BY call_time DESC LIMIT @lim");
            pars.Add(new MySqlParameter("@lim", limit));

            var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
            var list = new List<ApiUsageLog>();
            foreach (DataRow r in dt.Rows) list.Add(ParseApiUsage(r));
            return list;
        }

 
        #endregion
#region === 29 · EXPORT / PRINT LOG =========================================

        public async Task<int> InsertExportPrintLogAsync(
            int? userId, string format, string tableName, string? filterUsed, string? filePath, string? sourceIp, string? note,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO export_print_log
(user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,@tbl,@filter,@file,@ip,@note)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@uid",   (object?)userId ?? DBNull.Value),
                new MySqlParameter("@fmt",   format),
                new MySqlParameter("@tbl",   tableName),
                new MySqlParameter("@filter",filterUsed ?? (object)DBNull.Value),
                new MySqlParameter("@file",  filePath ?? (object)DBNull.Value),
                new MySqlParameter("@ip",    sourceIp ?? (object)DBNull.Value),
                new MySqlParameter("@note",  note ?? (object)DBNull.Value)
            }, token).ConfigureAwait(false);

            return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        public async Task<List<ExportPrintEntry>> GetRecentExportsAsync(
            DateTime? from = null, DateTime? to = null, string? tableName = null, string? format = null, int limit = 200,
            CancellationToken token = default)
        {
            var sql = new StringBuilder("SELECT * FROM export_print_log WHERE 1=1");
            var pars = new List<MySqlParameter>();
            if (from.HasValue) { sql.Append(" AND export_time>=@f"); pars.Add(new("@f", from.Value)); }
            if (to.HasValue) { sql.Append(" AND export_time<=@t"); pars.Add(new("@t", to.Value)); }
            if (!string.IsNullOrWhiteSpace(tableName)) { sql.Append(" AND table_name=@tbl"); pars.Add(new("@tbl", tableName)); }
            if (!string.IsNullOrWhiteSpace(format)) { sql.Append(" AND format=@fmt"); pars.Add(new("@fmt", format)); }
            sql.Append(" ORDER BY export_time DESC LIMIT @lim");
            pars.Add(new MySqlParameter("@lim", limit));

            var dt = await ExecuteSelectAsync(sql.ToString(), pars.ToArray(), token).ConfigureAwait(false);
            var list = new List<ExportPrintEntry>();
            foreach (DataRow r in dt.Rows) list.Add(ParseExportPrintEntry(r));
            return list;
        }

        private static ExportPrintEntry ParseExportPrintEntry(DataRow r) => new ExportPrintEntry
        {
            Id         = Convert.ToInt32(r["id"]),
            UserId     = r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value ? Convert.ToInt32(r["user_id"]) : (int?)null,
            ExportTime = r.Table.Columns.Contains("export_time") && r["export_time"] != DBNull.Value ? Convert.ToDateTime(r["export_time"]) : DateTime.MinValue,
            Format     = r["format"]?.ToString(),
            TableName  = r["table_name"]?.ToString(),
            FilterUsed = r["filter_used"]?.ToString(),
            FilePath   = r["file_path"]?.ToString(),
            SourceIp   = r["source_ip"]?.ToString(),
            Note       = r["note"]?.ToString()
        };

        #endregion
#region === 30 · REPORT SCHEDULE ============================================

        public async Task<int> UpsertReportScheduleAsync(ReportSchedule r, bool update, int actorUserId, CancellationToken token = default)
        {
            string sql = !update
                ? @"INSERT INTO report_schedule
(report_name, schedule_type, format, recipients, last_generated, next_due, status, generated_by)
VALUES (@name,@stype,@fmt,@rec,@last,@next,@status,@by)"
                : @"UPDATE report_schedule SET
report_name=@name, schedule_type=@stype, format=@fmt, recipients=@rec, last_generated=@last, next_due=@next, status=@status, generated_by=@by
WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@name",   r.ReportName ?? string.Empty),
                new("@stype",  r.ScheduleType ?? "on_demand"),
                new("@fmt",    r.Format ?? "pdf"),
                new("@rec",    r.Recipients ?? (object)DBNull.Value),
                new("@last",   (object?)r.LastGenerated ?? DBNull.Value),
                new("@next",   (object?)r.NextDue ?? DBNull.Value),
                new("@status", r.Status ?? "planned"),
                new("@by",     (object?)actorUserId ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", r.Id));

            await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
            return update ? r.Id : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        public async Task SetReportScheduleStatusAsync(int id, string status, DateTime? lastGenerated, DateTime? nextDue, CancellationToken token = default)
        {
            await ExecuteNonQueryAsync(@"
UPDATE report_schedule
SET status=@st, last_generated=COALESCE(@lg,last_generated), next_due=COALESCE(@nd,next_due)
WHERE id=@id",
                new[]
                {
                    new MySqlParameter("@st", status),
                    new MySqlParameter("@lg", (object?)lastGenerated ?? DBNull.Value),
                    new MySqlParameter("@nd", (object?)nextDue ?? DBNull.Value),
                    new MySqlParameter("@id", id)
                }, token).ConfigureAwait(false);
        }

        public async Task<List<ReportSchedule>> GetDueReportSchedulesAsync(CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM report_schedule WHERE status IN ('planned','failed') AND next_due IS NOT NULL AND next_due<=NOW() ORDER BY next_due",
                null, token).ConfigureAwait(false);

            var list = new List<ReportSchedule>();
            foreach (DataRow r in dt.Rows) list.Add(ParseReportSchedule(r));
            return list;
        }

        private static ReportSchedule ParseReportSchedule(DataRow r) => new ReportSchedule
        {
            Id            = Convert.ToInt32(r["id"]),
            ReportName    = r["report_name"]?.ToString(),
            ScheduleType  = r["schedule_type"]?.ToString(),
            Format        = r["format"]?.ToString(),
            Recipients    = r["recipients"]?.ToString(),
            LastGenerated = r.Table.Columns.Contains("last_generated") && r["last_generated"] != DBNull.Value ? Convert.ToDateTime(r["last_generated"]) : (DateTime?)null,
            NextDue       = r.Table.Columns.Contains("next_due") && r["next_due"] != DBNull.Value ? Convert.ToDateTime(r["next_due"]) : (DateTime?)null,
            Status        = r["status"]?.ToString(),
            GeneratedBy   = r.Table.Columns.Contains("generated_by") && r["generated_by"] != DBNull.Value ? Convert.ToInt32(r["generated_by"]) : (int?)null
        };

        #endregion
#region === 31 · SYSTEM PARAMETERS ==========================================

        public async Task<SystemParameter?> GetParameterAsync(string name, CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM system_parameters WHERE param_name=@n LIMIT 1",
                new[] { new MySqlParameter("@n", name) }, token).ConfigureAwait(false);

            return dt.Rows.Count == 0 ? null : ParseSystemParameter(dt.Rows[0]);
        }

        public async Task<List<SystemParameter>> GetAllParametersAsync(CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync("SELECT * FROM system_parameters ORDER BY param_name", null, token).ConfigureAwait(false);
            var list = new List<SystemParameter>();
            foreach (DataRow r in dt.Rows) list.Add(ParseSystemParameter(r));
            return list;
        }

        public async Task UpsertParameterAsync(string name, string? value, int? updatedBy, string? note, CancellationToken token = default)
        {
            // Insert if not exists; else update
            const string sql = @"
INSERT INTO system_parameters (param_name, param_value, updated_by, note)
VALUES (@n,@v,@u,@note)
ON DUPLICATE KEY UPDATE param_value=VALUES(param_value), updated_by=VALUES(updated_by), updated_at=CURRENT_TIMESTAMP, note=VALUES(note)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@n", name),
                new MySqlParameter("@v", value ?? (object)DBNull.Value),
                new MySqlParameter("@u", (object?)updatedBy ?? DBNull.Value),
                new MySqlParameter("@note", note ?? (object)DBNull.Value)
            }, token).ConfigureAwait(false);
        }

        private static SystemParameter ParseSystemParameter(DataRow r) => new SystemParameter
        {
            Id         = Convert.ToInt32(r["id"]),
            Name       = r["param_name"]?.ToString(),
            Value      = r["param_value"]?.ToString(),
            UpdatedBy  = r.Table.Columns.Contains("updated_by") && r["updated_by"] != DBNull.Value ? Convert.ToInt32(r["updated_by"]) : (int?)null,
            UpdatedAt  = r.Table.Columns.Contains("updated_at") && r["updated_at"] != DBNull.Value ? Convert.ToDateTime(r["updated_at"]) : DateTime.MinValue,
            Note       = r["note"]?.ToString()
        };
// ===================== SETTINGS (UI/app/security) ==========================



public async Task<int> UpsertSettingAsync(
    Setting setting,
    bool update,
    int actorUserId,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO settings(`key`,`value`,category,description,created_at,created_by,device_info,session_id,ip_address)
            VALUES(@k,@v,@cat,@desc,NOW(),@by,@dev,@sess,@ip)"
        : @"UPDATE settings SET `key`=@k,`value`=@v,category=@cat,description=@desc,last_modified=NOW(),last_modified_by=@by,
            device_info=@dev,session_id=@sess,ip_address=@ip
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@k",    TryGetString(setting,"Key") ?? string.Empty),
        new("@v",    (object?)TryGetString(setting,"Value") ?? DBNull.Value),
        new("@cat",  (object?)TryGetString(setting,"Category") ?? DBNull.Value),
        new("@desc", (object?)TryGetString(setting,"Description") ?? DBNull.Value),
        new("@by",   actorUserId),
        new("@dev",  (object?)device ?? DBNull.Value),
        new("@sess", (object?)sessionId ?? DBNull.Value),
        new("@ip",   (object?)ip ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", TryGet<int>(setting,"Id") ?? 0));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);
    int id = update
        ? (TryGet<int>(setting,"Id") ?? 0)
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogSystemEventAsync(actorUserId, update ? "UPDATE" : "CREATE", "settings", "System",
        id, update ? "Setting updated" : "Setting created", ip, "audit", device, sessionId, token: token).ConfigureAwait(false);

    return id;
}



public Task RollbackSettingAsync(int id, int actorUserId, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
    => LogSystemEventAsync(actorUserId, "ROLLBACK", "settings", "System", id, "Setting rollback requested", ip, "audit", device, sessionId, token);

public async Task<string> ExportSettingsAsync(IEnumerable<Setting> rows, int actorUserId, string ip, string device, string? sessionId, string format = "csv", CancellationToken token = default)
{
    string filePath = $"/export/settings_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{(string.IsNullOrWhiteSpace(format) ? "csv" : format)}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'settings',@filter,@path,@ip,'Settings export')",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@fmt", format ?? "csv"),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip",   ip)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "EXPORT", "settings", "System", null, $"Exported {(rows?.Count() ?? 0)} settings to {filePath}.", ip, "audit", device, sessionId, token).ConfigureAwait(false);
    return filePath;
}

public async Task LogSettingAuditAsync(Setting? setting, string action, string? ip, string? device, string? sessionId, string? newValue, CancellationToken token = default)
{
    int? id = setting?.Id;
    await LogSystemEventAsync(null, $"SETTING_{action}", "settings", "System", id, setting?.Key, ip, "audit", device, sessionId, token).ConfigureAwait(false);
}

        #endregion
#region === 32 · SCHEMA MIGRATION LOG ======================================

        public async Task<int> InsertSchemaMigrationLogAsync(
            int? migratedBy, string schemaVersion, string? migrationScript, string? description, string? sourceIp, bool success, string? errorMessage,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO schema_migration_log
(migrated_by, schema_version, migration_script, description, source_ip, success, error_message)
VALUES (@by,@ver,@script,@desc,@ip,@ok,@err)";
            await ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@by",    (object?)migratedBy ?? DBNull.Value),
                new MySqlParameter("@ver",   schemaVersion),
                new MySqlParameter("@script",(object?)migrationScript ?? DBNull.Value),
                new MySqlParameter("@desc",  description ?? (object)DBNull.Value),
                new MySqlParameter("@ip",    sourceIp ?? (object)DBNull.Value),
                new MySqlParameter("@ok",    success),
                new MySqlParameter("@err",   errorMessage ?? (object)DBNull.Value)
            }, token).ConfigureAwait(false);

            return Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        public async Task<SchemaMigrationEntry?> GetLatestSchemaMigrationAsync(CancellationToken token = default)
        {
            var dt = await ExecuteSelectAsync(
                "SELECT * FROM schema_migration_log ORDER BY migration_time DESC, id DESC LIMIT 1",
                null, token).ConfigureAwait(false);

            return dt.Rows.Count == 0 ? null : ParseSchemaMigrationEntry(dt.Rows[0]);
        }

        private static SchemaMigrationEntry ParseSchemaMigrationEntry(DataRow r) => new SchemaMigrationEntry
        {
            Id             = Convert.ToInt32(r["id"]),
            MigrationTime  = r.Table.Columns.Contains("migration_time") && r["migration_time"] != DBNull.Value ? Convert.ToDateTime(r["migration_time"]) : DateTime.MinValue,
            MigratedBy     = r.Table.Columns.Contains("migrated_by") && r["migrated_by"] != DBNull.Value ? Convert.ToInt32(r["migrated_by"]) : (int?)null,
            SchemaVersion  = r["schema_version"]?.ToString(),
            MigrationScript= r["migration_script"]?.ToString(),
            Description    = r["description"]?.ToString(),
            SourceIp       = r["source_ip"]?.ToString(),
            Success        = r.Table.Columns.Contains("success") && r["success"] != DBNull.Value && Convert.ToBoolean(r["success"]),
            ErrorMessage   = r["error_message"]?.ToString()
        };

        #endregion
#region === 33 · CAPA CASES & WORKFLOW ====================================

/// <summary>
/// Returns all CAPA cases ordered by <c>last_modified</c> (descending).
/// </summary>
public async Task<List<CapaCase>> GetAllCapaCasesAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM capa_cases ORDER BY last_modified DESC", null, token).ConfigureAwait(false);

    var list = new List<CapaCase>();
    foreach (DataRow r in dt.Rows) list.Add(ParseCapaCase(r));
    return list;
}

/// <summary>Retrieves a single CAPA case by its primary key.</summary>
public async Task<CapaCase?> GetCapaCaseByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM capa_cases WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParseCapaCase(dt.Rows[0]);
}

/// <summary>Inserts a new CAPA case and writes an audit trail entry.</summary>
public async Task<int> AddCapaCaseAsync(
    CapaCase capa,
    string signatureHash,
    string ip,
    string deviceInfo,
    string sessionId,
    int actorUserId = 1,
    CancellationToken token = default)
{
    var code        = TryGetString(capa, "CapaCode")     ?? TryGetString(capa, "Code") ?? string.Empty;
    var title       = TryGetString(capa, "Title")        ?? string.Empty;
    var description = TryGetString(capa, "Description")  ?? string.Empty;
    var status      = TryGetString(capa, "Status")       ?? "open";
    var risk        = TryGetString(capa, "RiskRating")   ?? TryGetString(capa, "Risk") ?? string.Empty;
    var actions     = TryGetString(capa, "Actions")      ?? string.Empty;

    const string sql = @"
INSERT INTO capa_cases
(capa_code, title, description, status, risk_rating, actions, last_modified, digital_signature)
VALUES
(@code,@title,@desc,@status,@risk,@actions,NOW(),@sig);";

    var pars = new[]
    {
        new MySqlParameter("@code",    code),
        new MySqlParameter("@title",   title),
        new MySqlParameter("@desc",    description),
        new MySqlParameter("@status",  status),
        new MySqlParameter("@risk",    risk),
        new MySqlParameter("@actions", actions),
        new MySqlParameter("@sig",     signatureHash ?? string.Empty)
    };

    await ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
    var newId = Convert.ToInt32(
        await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogCapaCaseAuditAsync(
        newId, "CREATE", actorUserId, ip, deviceInfo, sessionId,
        $"CAPA '{code}' created.", token).ConfigureAwait(false);

    return newId;
}

/// <summary>Updates an existing CAPA case and writes an audit trail entry.</summary>
public async Task UpdateCapaCaseAsync(
    CapaCase capa,
    string signatureHash,
    string ip,
    string deviceInfo,
    string sessionId,
    int actorUserId = 1,
    CancellationToken token = default)
{
    var id          = TryGet<int>(capa, "Id") ?? 0;
    var code        = TryGetString(capa, "CapaCode")     ?? TryGetString(capa, "Code") ?? string.Empty;
    var title       = TryGetString(capa, "Title")        ?? string.Empty;
    var description = TryGetString(capa, "Description")  ?? string.Empty;
    var status      = TryGetString(capa, "Status")       ?? "open";
    var risk        = TryGetString(capa, "RiskRating")   ?? TryGetString(capa, "Risk") ?? string.Empty;
    var actions     = TryGetString(capa, "Actions")      ?? string.Empty;

    const string sql = @"
UPDATE capa_cases SET
    capa_code=@code,
    title=@title,
    description=@desc,
    status=@status,
    risk_rating=@risk,
    actions=@actions,
    last_modified=NOW(),
    digital_signature=@sig
WHERE id=@id;";

    var pars = new[]
    {
        new MySqlParameter("@code",    code),
        new MySqlParameter("@title",   title),
        new MySqlParameter("@desc",    description),
        new MySqlParameter("@status",  status),
        new MySqlParameter("@risk",    risk),
        new MySqlParameter("@actions", actions),
        new MySqlParameter("@sig",     signatureHash ?? string.Empty),
        new MySqlParameter("@id",      id)
    };

    await ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        id, "UPDATE", actorUserId, ip, deviceInfo, sessionId,
        $"CAPA '{code}' updated.", token).ConfigureAwait(false);
}

public async Task DeleteCapaCaseAsync(
    int capaId,
    int actorUserId,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "DELETE FROM capa_cases WHERE id=@id",
        new[] { new MySqlParameter("@id", capaId) }, token).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        capaId, "DELETE", actorUserId, ip, deviceInfo, null,
        $"CAPA '{capaId}' deleted.", token).ConfigureAwait(false);
}

public async Task RollbackCapaCaseAsync(
    int capaId,
    string contextJson,
    int actorUserId,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE capa_cases SET status='open', last_modified=NOW() WHERE id=@id",
        new[] { new MySqlParameter("@id", capaId) }, token).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        capaId, "ROLLBACK", actorUserId, ip, deviceInfo, null,
        $"CAPA rolled back. Context: {contextJson}", token).ConfigureAwait(false);
}

public async Task EscalateCapaCaseAsync(
    int capaId,
    int actorUserId,
    string reason,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE capa_cases SET status='escalated', last_modified=NOW() WHERE id=@id",
        new[] { new MySqlParameter("@id", capaId) }, token).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        capaId, "ESCALATE", actorUserId, ip, deviceInfo, null,
        $"CAPA escalated: {reason}", token).ConfigureAwait(false);
}

public async Task ApproveCapaCaseAsync(
    int capaId,
    int actorUserId,
    string ip,
    string deviceInfo,
    string signatureHash,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE capa_cases SET status='approved', last_modified=NOW(), digital_signature=@sig WHERE id=@id",
        new[]
        {
            new MySqlParameter("@sig", signatureHash ?? string.Empty),
            new MySqlParameter("@id",  capaId)
        }, token).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        capaId, "APPROVE", actorUserId, ip, deviceInfo, null,
        "CAPA approved.", token).ConfigureAwait(false);
}

public async Task RejectCapaCaseAsync(
    int capaId,
    int actorUserId,
    string ip,
    string deviceInfo,
    string signatureHash,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE capa_cases SET status='rejected', last_modified=NOW(), digital_signature=@sig WHERE id=@id",
        new[]
        {
            new MySqlParameter("@sig", signatureHash ?? string.Empty),
            new MySqlParameter("@id",  capaId)
        }, token).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        capaId, "REJECT", actorUserId, ip, deviceInfo, null,
        "CAPA rejected.", token).ConfigureAwait(false);
}

public async Task ExportCapaCasesAsync(
    IEnumerable<CapaCase> rows,
    string format,
    int actorUserId = 1,
    string ip = "system",
    string deviceInfo = "server",
    string? sessionId = null,
    CancellationToken token = default)
{
    var list = (rows ?? Enumerable.Empty<CapaCase>()).ToList();
    var sb = new StringBuilder();
    sb.AppendLine("Id,CapaCode,Title,Status,RiskRating,LastModified");

    foreach (var c in list)
    {
        string E(string? v) => string.IsNullOrEmpty(v) ? "" : "\"" + v.Replace("\"", "\"\"") + "\"";
        var code  = TryGetString(c, "CapaCode") ?? TryGetString(c, "Code");
        var title = TryGetString(c, "Title");
        var status = TryGetString(c, "Status");
        var risk = TryGetString(c, "RiskRating") ?? TryGetString(c, "Risk");
        var lm = TryGet<DateTime>(c, "LastModified") ?? DateTime.UtcNow;

        sb.Append(TryGet<int>(c, "Id") ?? 0).Append(',')
          .Append(E(code)).Append(',')
          .Append(E(title)).Append(',')
          .Append(E(status)).Append(',')
          .Append(E(risk)).Append(',')
          .Append(lm.ToString("yyyy-MM-dd HH:mm:ss"))
          .AppendLine();
    }

    var filePath = $"/export/capa_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{(string.IsNullOrWhiteSpace(format) ? "csv" : format)}";
    await SaveExportPrintLogAsync(
        userId: actorUserId,
        format: string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant(),
        tableName: "capa_cases",
        filterUsed: $"count={list.Count}",
        filePath: filePath,
        sourceIp: ip,                 // FIXED
        note: "CAPA export",
        token: token
    ).ConfigureAwait(false);

    await LogCapaCaseAuditAsync(
        0, "EXPORT", actorUserId, ip, deviceInfo, sessionId,
        $"Exported {list.Count} CAPA rows as {(string.IsNullOrWhiteSpace(format) ? "csv" : format)}.",
        token).ConfigureAwait(false);
}

public async Task LogCapaCaseAuditAsync(
    int capaId,
    string action,
    int? actorUserId,
    string ip,
    string? deviceInfo,
    string? sessionId,
    string description,
    CancellationToken token = default)
{
    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: action ?? "CAPA",
        tableName: "capa_cases",
        module: "CAPA",
        recordId: capaId == 0 ? (int?)null : capaId,
        description: description ?? string.Empty,
        ip: ip ?? "system",
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId
    ).ConfigureAwait(false);
}

private static CapaCase ParseCapaCase(DataRow r)
{
    var c = Activator.CreateInstance<CapaCase>();

    bool Has(string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value;

    if (Has("id"))                SetIfExists(c, "Id", Convert.ToInt32(r["id"]));
    if (r.Table.Columns.Contains("capa_code"))         SetIfExists(c, "CapaCode", r["capa_code"]?.ToString());
    if (r.Table.Columns.Contains("title"))             SetIfExists(c, "Title", r["title"]?.ToString());
    if (r.Table.Columns.Contains("description"))       SetIfExists(c, "Description", r["description"]?.ToString());
    if (r.Table.Columns.Contains("status"))            SetIfExists(c, "Status", r["status"]?.ToString());
    if (r.Table.Columns.Contains("risk_rating"))       SetIfExists(c, "RiskRating", r["risk_rating"]?.ToString());
    if (r.Table.Columns.Contains("actions"))           SetIfExists(c, "Actions", r["actions"]?.ToString());
    if (Has("last_modified"))     SetIfExists(c, "LastModified", Convert.ToDateTime(r["last_modified"]));
    if (r.Table.Columns.Contains("digital_signature")) SetIfExists(c, "DigitalSignature", r["digital_signature"]?.ToString());

    return c;
}

#endregion
#region === 34 · DEVIATIONS & AUDIT =======================================

/// <summary>
/// Retrieves all deviations ordered by creation/detection time (descending).
/// Schema-tolerant: works whether your table has <c>created_at</c> or only <c>detected_at</c>.
/// </summary>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="Deviation"/> domain models.</returns>
public async Task<List<Deviation>> GetAllDeviationsAsync(CancellationToken token = default)
{
    // Prefer created_at if present; otherwise detected_at; fallback to id.
    var sql = "SELECT * FROM deviations ORDER BY " +
              "CASE WHEN LOCATE('created_at', (SELECT GROUP_CONCAT(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='deviations')) > 0 THEN 1 ELSE 2 END, " +
              "created_at DESC, detected_at DESC, id DESC";

    var dt = await ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
    var list = new List<Deviation>();
    foreach (DataRow r in dt.Rows)
        list.Add(ParseDeviation(r)); // <-- resolves CS0103

    return list;
}

/// <summary>
/// Retrieves a single deviation by its identifier.
/// </summary>
/// <param name="id">Deviation primary key.</param>
/// <param name="token">Cancellation token.</param>
public async Task<Deviation?> GetDeviationByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM deviations WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    if (dt.Rows.Count == 0) return null;
    return ParseDeviation(dt.Rows[0]); // <-- resolves CS0103
}

/// <summary>
/// Inserts a new deviation or updates an existing one in a schema-tolerant manner.
/// </summary>
/// <param name="d">Deviation entity carrying values.</param>
/// <param name="update">If true, performs UPDATE; otherwise INSERT.</param>
/// <param name="actorUserId">Acting user (for audit/system-event logging).</param>
/// <param name="ip">Optional source IP for audit.</param>
/// <param name="device">Optional device info for audit.</param>
/// <param name="sessionId">Optional session id for audit.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Newly inserted ID or the existing ID on update.</returns>
public async Task<int> InsertOrUpdateDeviationAsync(
    Deviation d, bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    string sql = !update
        ? @"INSERT INTO deviations
(title, description, severity, status, detected_at, reported_by, digital_signature)
VALUES (@title,@desc,@sev,@status,@det,@by,@sig)"
        : @"UPDATE deviations SET
title=@title, description=@desc, severity=@sev, status=@status, detected_at=@det, reported_by=@by, digital_signature=@sig
WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@title",  TryGetString(d,"Title") ?? string.Empty),
        new("@desc",   (object?)TryGetString(d,"Description") ?? DBNull.Value),
        new("@sev",    TryGetString(d,"Severity") ?? "low"),
        new("@status", TryGetString(d,"Status") ?? "open"),
        new("@det",    (object?)TryGet<DateTime>(d,"DetectedAt") ?? DBNull.Value),
        // Support both ReportedBy and ReportedById property names if your model varied across versions
        new("@by",     (object?)TryGet<int>(d,"ReportedBy") ?? (object?)TryGet<int>(d,"ReportedById") ?? DBNull.Value),
        new("@sig",    (object?)TryGetString(d,"DigitalSignature") ?? DBNull.Value),
        new("@id",     (object?)TryGet<int>(d,"Id") ?? DBNull.Value),
    };

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = !update
        ? Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false))
        : (TryGet<int>(d, "Id") ?? 0);

    // Local, signature-safe event log (bypasses unknown overloads of LogSystemEventAsync)
    await LogDeviationSystemEventAsync(
        userId: actorUserId,
        eventType: update ? "deviation_update" : "deviation_insert",
        description: $"Deviation {(update ? "updated" : "inserted")} (ID={id})",
        ip: ip, deviceInfo: device, sessionId: sessionId, relatedId: id, token: token
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Deletes a deviation by ID and writes a system event (local logger).
/// </summary>
public async Task DeleteDeviationAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM deviations WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    // Avoids CS1503/CS1739 by using local logger with explicit schema
    await LogDeviationSystemEventAsync(
        userId: actorUserId,
        eventType: "deviation_delete",
        description: $"Deviation deleted (ID={id})",
        ip: ip, deviceInfo: device, sessionId: sessionId, relatedId: id, token: token
    ).ConfigureAwait(false);
}

/// <summary>
/// Inserts a deviation audit row aligned with <c>deviation_audit</c> table and <see cref="DeviationAudit"/> model.
/// </summary>
public async Task<int> InsertDeviationAuditAsync(DeviationAudit a, CancellationToken token = default)
{
    const string sql = @"
INSERT INTO deviation_audit
(deviation_id, action, user_id, changed_at, device_info, source_ip, session_id, digital_signature, details, comment)
VALUES (@did,@act,@uid,COALESCE(@at, NOW()),@dev,@ip,@sess,@sig,@details,@note)";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@did",  TryGet<int>(a,"DeviationId") ?? 0),
        new MySqlParameter("@act",  TryGetString(a,"Action") ?? TryGetString(a,"ActionType") ?? string.Empty),
        new MySqlParameter("@uid",  (object?)TryGet<int>(a,"UserId") ?? DBNull.Value),
        new MySqlParameter("@at",   (object?)TryGet<DateTime>(a,"ChangedAt") ?? DBNull.Value),
        new MySqlParameter("@dev",  (object?)TryGetString(a,"DeviceInfo") ?? DBNull.Value),
        new MySqlParameter("@ip",   (object?)TryGetString(a,"SourceIp") ?? DBNull.Value),
        new MySqlParameter("@sess", (object?)TryGetString(a,"SessionId") ?? DBNull.Value),
        new MySqlParameter("@sig",  (object?)TryGetString(a,"DigitalSignature") ?? DBNull.Value),
        new MySqlParameter("@details", (object?)TryGetString(a,"Details") ?? DBNull.Value),
        new MySqlParameter("@note", (object?)TryGetString(a,"Comment") ?? DBNull.Value)
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    await LogDeviationSystemEventAsync(
        userId: TryGet<int>(a,"UserId") ?? 0,
        eventType: "deviation_audit_insert",
        description: $"Deviation audit inserted (ID={id}, DevID={TryGet<int>(a,"DeviationId") ?? 0})",
        ip: TryGetString(a,"SourceIp"),
        deviceInfo: TryGetString(a,"DeviceInfo"),
        sessionId: TryGetString(a,"SessionId"),
        relatedId: id,
        token: token
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Updates an existing deviation audit row.
/// </summary>
public async Task UpdateDeviationAuditAsync(DeviationAudit a, CancellationToken token = default)
{
    const string sql = @"
UPDATE deviation_audit SET
action=@act, user_id=@uid, changed_at=@at, device_info=@dev, source_ip=@ip, session_id=@sess, digital_signature=@sig, details=@details, comment=@note
WHERE id=@id";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@act",  TryGetString(a,"Action") ?? TryGetString(a,"ActionType") ?? string.Empty),
        new MySqlParameter("@uid",  (object?)TryGet<int>(a,"UserId") ?? DBNull.Value),
        new MySqlParameter("@at",   (object?)TryGet<DateTime>(a,"ChangedAt") ?? DBNull.Value),
        new MySqlParameter("@dev",  (object?)TryGetString(a,"DeviceInfo") ?? DBNull.Value),
        new MySqlParameter("@ip",   (object?)TryGetString(a,"SourceIp") ?? DBNull.Value),
        new MySqlParameter("@sess", (object?)TryGetString(a,"SessionId") ?? DBNull.Value),
        new MySqlParameter("@sig",  (object?)TryGetString(a,"DigitalSignature") ?? DBNull.Value),
        new MySqlParameter("@details", (object?)TryGetString(a,"Details") ?? DBNull.Value),
        new MySqlParameter("@note", (object?)TryGetString(a,"Comment") ?? DBNull.Value),
        new MySqlParameter("@id",   TryGet<int>(a,"Id") ?? 0),
    }, token).ConfigureAwait(false);

    await LogDeviationSystemEventAsync(
        userId: TryGet<int>(a,"UserId") ?? 0,
        eventType: "deviation_audit_update",
        description: $"Deviation audit updated (ID={TryGet<int>(a,"Id") ?? 0})",
        ip: TryGetString(a,"SourceIp"),
        deviceInfo: TryGetString(a,"DeviceInfo"),
        sessionId: TryGetString(a,"SessionId"),
        relatedId: TryGet<int>(a,"Id") ?? 0,
        token: token
    ).ConfigureAwait(false);
}

/// <summary>
/// Deletes a deviation audit row by ID.
/// </summary>
public async Task DeleteDeviationAuditAsync(int id, CancellationToken token = default)
{
    await ExecuteNonQueryAsync("DELETE FROM deviation_audit WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    await LogDeviationSystemEventAsync(
        userId: 0,
        eventType: "deviation_audit_delete",
        description: $"Deviation audit deleted (ID={id})",
        relatedId: id,
        token: token
    ).ConfigureAwait(false);
}

/// <summary>Gets one audit row by Id.</summary>
public async Task<DeviationAudit?> GetDeviationAuditByIdAsync(int id, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM deviation_audit WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    return dt.Rows.Count == 0 ? null : ParseDeviationAudit(dt.Rows[0]);
}

/// <summary>Gets audit rows for a deviation (newest first).</summary>
public async Task<List<DeviationAudit>> GetDeviationAuditsByDeviationIdAsync(int deviationId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM deviation_audit WHERE deviation_id=@id ORDER BY changed_at DESC",
        new[] { new MySqlParameter("@id", deviationId) }, token).ConfigureAwait(false);
    var list = new List<DeviationAudit>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDeviationAudit(r));
    return list;
}

/// <summary>Gets audit rows created by a user (newest first).</summary>
public async Task<List<DeviationAudit>> GetDeviationAuditsByUserIdAsync(int userId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM deviation_audit WHERE user_id=@u ORDER BY changed_at DESC",
        new[] { new MySqlParameter("@u", userId) }, token).ConfigureAwait(false);
    var list = new List<DeviationAudit>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDeviationAudit(r));
    return list;
}

/// <summary>Gets audit rows filtered by action (newest first).</summary>
public async Task<List<DeviationAudit>> GetDeviationAuditsByActionTypeAsync(string actionType, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM deviation_audit WHERE action=@a ORDER BY changed_at DESC",
        new[] { new MySqlParameter("@a", actionType) }, token).ConfigureAwait(false);
    var list = new List<DeviationAudit>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDeviationAudit(r));
    return list;
}

/// <summary>Gets audit rows within a UTC date range.</summary>
public async Task<List<DeviationAudit>> GetDeviationAuditsByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM deviation_audit WHERE changed_at BETWEEN @f AND @t ORDER BY changed_at DESC",
        new[] {
            new MySqlParameter("@f", fromUtc),
            new MySqlParameter("@t", toUtc)
        }, token).ConfigureAwait(false);

    var list = new List<DeviationAudit>();
    foreach (DataRow r in dt.Rows) list.Add(ParseDeviationAudit(r));
    return list;
}

/// <summary>
/// Exports audit rows to CSV and logs a system event locally.
/// </summary>
/// <param name="rows">Audit rows to export.</param>
/// <param name="format">Currently 'csv' is generated; parameter kept for API stability.</param>
/// <param name="actorUserId">User initiating export.</param>
/// <param name="ip">Optional IP.</param>
/// <param name="deviceInfo">Optional device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Server file path of the generated export.</returns>
public async Task<string> ExportDeviationAuditLogsAsync(
    IEnumerable<DeviationAudit> rows,
    string format = "csv",
    int actorUserId = 1,
    string ip = "system",
    string deviceInfo = "server",
    string? sessionId = null,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
    string filePath = $"/export/deviation_audit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

    int count = 0;
    var sb = new StringBuilder();
    sb.AppendLine("Id,DeviationId,Action,UserId,ChangedAt,SourceIp,DeviceInfo,SessionId,Details,Comment");

    foreach (var a in rows ?? Enumerable.Empty<DeviationAudit>())
    {
        count++;
        static string Q(string? v) => string.IsNullOrEmpty(v) ? "" : "\"" + v.Replace("\"","\"\"") + "\"";

        sb.Append(TryGet<int>(a,"Id") ?? 0).Append(',')
          .Append(TryGet<int>(a,"DeviationId") ?? 0).Append(',')
          .Append(Q(TryGetString(a,"Action") ?? TryGetString(a,"ActionType"))).Append(',')
          .Append(TryGet<int>(a,"UserId") ?? 0).Append(',')
          .Append((TryGet<DateTime>(a,"ChangedAt") ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm:ss")).Append(',')
          .Append(Q(TryGetString(a,"SourceIp"))).Append(',')
          .Append(Q(TryGetString(a,"DeviceInfo"))).Append(',')
          .Append(Q(TryGetString(a,"SessionId"))).Append(',')
          .Append(Q(TryGetString(a,"Details"))).Append(',')
          .Append(Q(TryGetString(a,"Comment"))).AppendLine();
    }

    await LogDeviationSystemEventAsync(
        userId: actorUserId,
        eventType: "deviation_audit_export",
        description: $"Deviation audit export: {count} rows → {fmt}",
        ip: ip, deviceInfo: deviceInfo, sessionId: sessionId, relatedId: null, token: token
    ).ConfigureAwait(false);

    await FileWriteAllTextAsync(filePath, sb.ToString(), token).ConfigureAwait(false); // <-- resolves CS0103
    return filePath;
}

/// <summary>
/// Maps a DataRow to <see cref="Deviation"/> in a schema-tolerant way.
/// Accepts columns: id, title, description, severity, status, detected_at, reported_by, digital_signature,
/// and optional created_at/updated_at.
/// </summary>
private static Deviation ParseDeviation(DataRow r) // <-- implements missing method to fix CS0103
{
    var d = Activator.CreateInstance<Deviation>();
    bool Has(string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value;

    if (Has("id"))               SetIfExists(d, "Id", Convert.ToInt32(r["id"]));
    if (Has("title"))            SetIfExists(d, "Title", r["title"]?.ToString());
    if (Has("description"))      SetIfExists(d, "Description", r["description"]?.ToString());
    if (Has("severity"))         SetIfExists(d, "Severity", r["severity"]?.ToString());
    if (Has("status"))           SetIfExists(d, "Status", r["status"]?.ToString());
    if (Has("detected_at"))      SetIfExists(d, "DetectedAt", Convert.ToDateTime(r["detected_at"]));
    if (Has("reported_by"))      SetIfExists(d, "ReportedBy", Convert.ToInt32(r["reported_by"]));
    if (Has("digital_signature"))SetIfExists(d, "DigitalSignature", r["digital_signature"]?.ToString());

    // Optional timestamps if your model includes them
    if (Has("created_at"))       SetIfExists(d, "CreatedAt", Convert.ToDateTime(r["created_at"]));
    if (Has("updated_at"))       SetIfExists(d, "UpdatedAt", Convert.ToDateTime(r["updated_at"]));

    return d;
}

/// <summary>
/// Maps a DataRow to <see cref="DeviationAudit"/> supporting both canonical and legacy column names.
/// </summary>
private static DeviationAudit ParseDeviationAudit(DataRow r)
{
    var a = Activator.CreateInstance<DeviationAudit>();
    bool Has(string col) => r.Table.Columns.Contains(col) && r[col] != DBNull.Value;

    if (Has("id"))           SetIfExists(a, "Id", Convert.ToInt32(r["id"]));
    if (Has("deviation_id")) SetIfExists(a, "DeviationId", Convert.ToInt32(r["deviation_id"]));

    // Canonical columns
    if (r.Table.Columns.Contains("action"))       SetIfExists(a, "Action", r["action"]?.ToString());
    if (r.Table.Columns.Contains("changed_at"))   SetIfExists(a, "ChangedAt", Convert.ToDateTime(r["changed_at"]));
    if (r.Table.Columns.Contains("source_ip"))    SetIfExists(a, "SourceIp", r["source_ip"]?.ToString());
    if (r.Table.Columns.Contains("device_info"))  SetIfExists(a, "DeviceInfo", r["device_info"]?.ToString());
    if (r.Table.Columns.Contains("session_id"))   SetIfExists(a, "SessionId", r["session_id"]?.ToString());
    if (r.Table.Columns.Contains("digital_signature")) SetIfExists(a, "DigitalSignature", r["digital_signature"]?.ToString());
    if (r.Table.Columns.Contains("details"))      SetIfExists(a, "Details", r["details"]?.ToString());
    if (r.Table.Columns.Contains("comment"))      SetIfExists(a, "Comment", r["comment"]?.ToString());

    // Legacy compatibility (if older dumps are in use)
    if (r.Table.Columns.Contains("action_type"))  SetIfExists(a, "Action", r["action_type"]?.ToString());
    if (Has("action_at"))                         SetIfExists(a, "ChangedAt", Convert.ToDateTime(r["action_at"]));

    return a;
}

/// <summary>
/// Writes text to a file path asynchronously, creating directories if needed.
/// </summary>
/// <param name="path">Absolute or application-rooted file path.</param>
/// <param name="content">File content.</param>
/// <param name="token">Cancellation token.</param>
private static async Task FileWriteAllTextAsync(string path, string content, CancellationToken token)
{
    var dir = System.IO.Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(dir) && !System.IO.Directory.Exists(dir))
        System.IO.Directory.CreateDirectory(dir);

    await System.IO.File.WriteAllTextAsync(path, content, token).ConfigureAwait(false);
}

/// <summary>
/// Local, schema-explicit system event logger used by this region to avoid
/// dependency on unknown <c>LogSystemEventAsync</c> overloads elsewhere.
/// Writes to a table named <c>system_events</c> (create if missing).
/// </summary>
/// <param name="userId">Actor user id.</param>
/// <param name="eventType">Event type string (e.g., deviation_insert).</param>
/// <param name="description">Human-readable description.</param>
/// <param name="ip">Optional source IP.</param>
/// <param name="deviceInfo">Optional device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="relatedId">Optional related entity id (e.g., deviation id).</param>
/// <param name="token">Cancellation token.</param>
private async Task LogDeviationSystemEventAsync(
    int userId,
    string eventType,
    string description,
    string? ip = null,
    string? deviceInfo = null,
    string? sessionId = null,
    int? relatedId = null,
    CancellationToken token = default)
{
    const string sql = @"
CREATE TABLE IF NOT EXISTS system_events (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NULL,
    event_type VARCHAR(128) NOT NULL,
    description TEXT NULL,
    related_id INT NULL,
    source_ip VARCHAR(64) NULL,
    device_info VARCHAR(256) NULL,
    session_id VARCHAR(128) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
    // Ensure table exists (idempotent)
    await ExecuteNonQueryAsync(sql, null, token).ConfigureAwait(false);

    const string ins = @"
INSERT INTO system_events
(user_id, event_type, description, related_id, source_ip, device_info, session_id, created_at)
VALUES (@uid, @etype, @desc, @rid, @ip, @dev, @sid, NOW());";

    await ExecuteNonQueryAsync(ins, new[]
    {
        new MySqlParameter("@uid",  (object?)userId ?? DBNull.Value),
        new MySqlParameter("@etype",(object?)eventType ?? DBNull.Value),
        new MySqlParameter("@desc", (object?)description ?? DBNull.Value),
        new MySqlParameter("@rid",  (object?)relatedId ?? DBNull.Value),
        new MySqlParameter("@ip",   (object?)ip ?? DBNull.Value),
        new MySqlParameter("@dev",  (object?)deviceInfo ?? DBNull.Value),
        new MySqlParameter("@sid",  (object?)sessionId ?? DBNull.Value),
    }, token).ConfigureAwait(false);
}

#endregion
#region === 35 · NOTIFICATIONS ==============================================

/// <summary>
/// Returns all notifications (newest first). If your schema lacks the <c>notifications</c> table,
/// the method will fail at runtime; compile-time remains safe. Wrap calls in try/catch if needed.
/// </summary>
/// <param name="token">Cancellation token.</param>
/// <returns>List of <see cref="Notification"/> domain models.</returns>
public async Task<List<Notification>> GetAllNotificationsFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM notifications ORDER BY created_at DESC, id DESC", null, token).ConfigureAwait(false);
    var list = new List<Notification>();
    foreach (DataRow r in dt.Rows) list.Add(ParseNotification(r));
    return list;
}

/// <summary>
/// Returns recipient identifiers for a given notification.
/// Expects a table <c>notification_recipients(notification_id, recipient)</c>.
/// </summary>
/// <param name="notificationId">Notification primary key.</param>
/// <param name="token">Cancellation token.</param>
public async Task<List<string>> GetNotificationRecipientsAsync(int notificationId, CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT recipient FROM notification_recipients WHERE notification_id=@id ORDER BY id",
        new[] { new MySqlParameter("@id", notificationId) }, token).ConfigureAwait(false);

    var recipients = new List<string>();
    foreach (DataRow r in dt.Rows)
    {
        var val = r["recipient"]?.ToString();
        if (!string.IsNullOrWhiteSpace(val)) recipients.Add(val);
    }
    return recipients;
}

/// <summary>
/// Inserts (or updates if Id present) a notification, stores recipients (if any), and logs an audit event.
/// </summary>
/// <param name="n">Notification entity to send or update.</param>
/// <param name="actorUserId">Acting user id for audit trail.</param>
/// <param name="ip">Non-null source IP.</param>
/// <param name="deviceInfo">Non-null device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>The database identifier of the affected notification.</returns>
public async Task<int> SendNotificationAsync(
    Notification n,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    bool update = (n?.Id ?? 0) > 0;
    if (n is null) throw new ArgumentNullException(nameof(n));

    string sql = !update
        ? @"INSERT INTO notifications
           (title, message, type, priority, status, entity, entity_id, link, recipients,
            recipient_id, recipient, sender_id, sender, acked_by_user_id, acked_at, muted_until,
            ip_address, device_info, session_id, created_at, updated_at)
           VALUES
           (@title,@msg,@type,@prio,@status,@entity,@entity_id,@link,@recipients,
            @rcpid,@rcp,@sid,@sname,@ackedby,@ackedat,@muteduntil,
            @ip,@dev,@sess,NOW(),NOW())"
        : @"UPDATE notifications SET
            title=@title, message=@msg, type=@type, priority=@prio, status=@status, entity=@entity, entity_id=@entity_id,
            link=@link, recipients=@recipients, recipient_id=@rcpid, recipient=@rcp, sender_id=@sid, sender=@sname,
            acked_by_user_id=@ackedby, acked_at=@ackedat, muted_until=@muteduntil,
            ip_address=@ip, device_info=@dev, session_id=@sess, updated_at=NOW()
           WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@title",       (object?) (n.Title ?? string.Empty)),
        new("@msg",         (object?) (n.Message ?? string.Empty)),
        new("@type",        (object?) (n.Type ?? "alert")),
        new("@prio",        (object?) (n.Priority ?? "normal")),
        new("@status",      (object?) (n.Status ?? "new")),
        new("@entity",      (object?) n.Entity ?? DBNull.Value),
        new("@entity_id",   (object?) n.EntityId ?? DBNull.Value),
        new("@link",        (object?) n.Link ?? DBNull.Value),
        new("@recipients",  (object?) n.Recipients ?? DBNull.Value),
        new("@rcpid",       (object?) n.RecipientId ?? DBNull.Value),
        new("@rcp",         (object?) n.Recipient ?? DBNull.Value),
        new("@sid",         (object?) n.SenderId ?? DBNull.Value),
        new("@sname",       (object?) n.Sender ?? DBNull.Value),
        new("@ackedby",     (object?) n.AckedByUserId ?? DBNull.Value),
        new("@ackedat",     (object?) n.AckedAt ?? DBNull.Value),
        new("@muteduntil",  (object?) n.MutedUntil ?? DBNull.Value),
        new("@ip",          (object?) (n.IpAddress ?? ip ?? "system")),
        new("@dev",         (object?) (n.DeviceInfo ?? deviceInfo ?? string.Empty)),
        new("@sess",        (object?) (n.SessionId ?? sessionId ?? string.Empty))
    };
    if (update) pars.Add(new MySqlParameter("@id", n.Id));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? n.Id
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // Optional per-recipient table (if present). Failure is non-fatal.
    try
    {
        if (n.RecipientList?.Count > 0)
        {
            foreach (var rcp in n.RecipientList)
            {
                await ExecuteNonQueryAsync(
                    "INSERT INTO notification_recipients (notification_id, recipient) VALUES (@nid,@rcp)",
                    new[] { new MySqlParameter("@nid", id), new MySqlParameter("@rcp", rcp) }, token
                ).ConfigureAwait(false);
            }
        }
    }
    catch (MySqlException ex) when (ex.Number == 1146) // table doesn't exist
    {
        await LogSystemEventAsync(actorUserId, "NTF_RECIPIENTS_FALLBACK", "notifications", "NotificationModule",
            id, "notification_recipients table missing; stored recipients inline.", ip, "warn", deviceInfo, sessionId).ConfigureAwait(false);
    }

    await LogNotificationAuditAsync(id, actorUserId, update ? "UPDATE" : "CREATE", ip, deviceInfo, sessionId, n.Message, token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, update ? "NTF_UPDATE" : "NTF_SEND", "notifications", "NotificationModule",
        id, n.Title, ip, "audit", deviceInfo, sessionId).ConfigureAwait(false);

    return id;
}

/// <summary>Marks a notification as acknowledged by a user and audits the action.</summary>
/// <param name="notificationId">Notification id.</param>
/// <param name="actorUserId">Acting user id.</param>
/// <param name="ip">Non-null IP.</param>
/// <param name="deviceInfo">Non-null device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
public async Task AcknowledgeNotificationAsync(
    int notificationId,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE notifications SET status='acknowledged', acked_by_user_id=@uid, acked_at=NOW(), updated_at=NOW() WHERE id=@id",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@id", notificationId)
        }, token).ConfigureAwait(false);

    await LogNotificationAuditAsync(notificationId, actorUserId, "ACK", ip, deviceInfo, sessionId, "Acknowledged", token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "NTF_ACK", "notifications", "NotificationModule",
        notificationId, "Notification acknowledged", ip, "audit", deviceInfo, sessionId).ConfigureAwait(false);
}

/// <summary>Temporarily mutes a notification until the specified UTC time.</summary>
/// <param name="notificationId">Notification id.</param>
/// <param name="mutedUntilUtc">UTC timestamp until which the notification remains muted.</param>
/// <param name="actorUserId">Acting user id.</param>
/// <param name="ip">Non-null IP.</param>
/// <param name="deviceInfo">Non-null device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
public async Task MuteNotificationAsync(
    int notificationId,
    DateTime mutedUntilUtc,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    await ExecuteNonQueryAsync(
        "UPDATE notifications SET status='muted', muted_until=@until, updated_at=NOW() WHERE id=@id",
        new[]
        {
            new MySqlParameter("@until", mutedUntilUtc),
            new MySqlParameter("@id", notificationId)
        }, token).ConfigureAwait(false);

    await LogNotificationAuditAsync(notificationId, actorUserId, "MUTE", ip, deviceInfo, sessionId, $"Muted until {mutedUntilUtc:u}", token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "NTF_MUTE", "notifications", "NotificationModule",
        notificationId, $"Muted until {mutedUntilUtc:u}", ip, "audit", deviceInfo, sessionId).ConfigureAwait(false);
}

/// <summary>Soft-deletes a notification (if your schema uses a status flag) or hard-deletes if not.</summary>
/// <param name="notificationId">Notification id.</param>
/// <param name="actorUserId">Acting user id.</param>
/// <param name="ip">Non-null IP.</param>
/// <param name="deviceInfo">Non-null device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
public async Task DeleteNotificationAsync(
    int notificationId,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    // Try soft delete first; if no row affected, perform hard delete.
    int affected = await ExecuteNonQueryAsync(
        "UPDATE notifications SET status='deleted', updated_at=NOW() WHERE id=@id",
        new[] { new MySqlParameter("@id", notificationId) }, token).ConfigureAwait(false);

    if (affected == 0)
    {
        await ExecuteNonQueryAsync("DELETE FROM notifications WHERE id=@id",
            new[] { new MySqlParameter("@id", notificationId) }, token).ConfigureAwait(false);
    }

    await LogNotificationAuditAsync(notificationId, actorUserId, "DELETE", ip, deviceInfo, sessionId, "Deleted", token).ConfigureAwait(false);
    await LogSystemEventAsync(actorUserId, "NTF_DELETE", "notifications", "NotificationModule",
        notificationId, "Notification deleted", ip, "audit", deviceInfo, sessionId).ConfigureAwait(false);
}

/// <summary>
/// Records an export of notifications; returns the export file path. Writes an audit event.
/// </summary>
/// <param name="rows">Rows to export.</param>
/// <param name="actorUserId">Acting user id.</param>
/// <param name="ip">Non-null IP.</param>
/// <param name="deviceInfo">Non-null device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="format">Export format (defaults to 'zip').</param>
/// <param name="token">Cancellation token.</param>
public async Task<string> ExportNotificationsAsync(
    IEnumerable<Notification> rows,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string format = "zip",
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
    string filePath = $"/export/notifications_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'notifications',@filter,@path,@ip,'Notifications export')",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip),
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(actorUserId, "NTF_EXPORT", "notifications", "NotificationModule",
        null, $"Exported {(rows?.Count() ?? 0)} notifications to {filePath}.", ip, "audit", deviceInfo, sessionId).ConfigureAwait(false);

    return filePath;
}

/// <summary>
/// Low-level audit helper for notifications; attempts to write into <c>notification_audit</c>,
/// and falls back to <see cref="LogSystemEventAsync"/> if the table is absent.
/// Accepts nullable networking fields and normalizes them internally to avoid CS8604 warnings.
/// </summary>
/// <param name="notificationId">Notification id.</param>
/// <param name="userId">Optional user id.</param>
/// <param name="action">Action string (CREATE/UPDATE/DELETE/ACK/MUTE).</param>
/// <param name="ip">Source IP address (nullable; normalized to empty when missing).</param>
/// <param name="deviceInfo">Device information (nullable; normalized to empty when missing).</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="note">Optional note.</param>
/// <param name="token">Cancellation token.</param>
public async Task LogNotificationAuditAsync(
    int notificationId,
    int? userId,
    string action,
    string? ip,
    string? deviceInfo,
    string? sessionId = null,
    string? note = null,
    CancellationToken token = default)
{
    string normIp = ip ?? string.Empty;
    string normDev = deviceInfo ?? string.Empty;

    try
    {
        await ExecuteNonQueryAsync(@"
INSERT INTO notification_audit (notification_id, user_id, action, note, source_ip, device_info, session_id)
VALUES (@nid,@uid,@action,@note,@ip,@dev,@sess)",
            new[]
            {
                new MySqlParameter("@nid",  notificationId),
                new MySqlParameter("@uid",  (object?)userId ?? DBNull.Value),
                new MySqlParameter("@action", action ?? "UPDATE"),
                new MySqlParameter("@note", (object?)note ?? DBNull.Value),
                new MySqlParameter("@ip",   normIp),
                new MySqlParameter("@dev",  normDev),
                new MySqlParameter("@sess", (object?)sessionId ?? DBNull.Value)
            }, token).ConfigureAwait(false);
    }
    catch (MySqlException ex) when (ex.Number == 1146) // table doesn't exist
    {
        await LogSystemEventAsync(userId, "NTF_AUDIT_FALLBACK", "notifications", "NotificationModule",
            notificationId, $"{action}: {note}", normIp, "warn", normDev, sessionId).ConfigureAwait(false);
    }
}

// ======================= PARSER (schema-tolerant) ===========================

/// <summary>DataRow → Notification (schema-tolerant mapping).</summary>
private static Notification ParseNotification(DataRow r)
{
    var n = new Notification
    {
        Id        = r.Table.Columns.Contains("id")                && r["id"]                != DBNull.Value ? Convert.ToInt32(r["id"])                : 0,
        Title     = r.Table.Columns.Contains("title")                                                          ? r["title"]?.ToString()                 ?? string.Empty : string.Empty,
        Message   = r.Table.Columns.Contains("message")                                                        ? r["message"]?.ToString()               ?? string.Empty : string.Empty,
        Type      = r.Table.Columns.Contains("type")                                                           ? r["type"]?.ToString()                  ?? "alert"      : "alert",
        Priority  = r.Table.Columns.Contains("priority")                                                       ? r["priority"]?.ToString()              ?? "normal"     : "normal",
        Status    = r.Table.Columns.Contains("status")                                                         ? r["status"]?.ToString()                ?? "new"        : "new",
        Entity    = r.Table.Columns.Contains("entity")                                                         ? r["entity"]?.ToString()                : null,
        EntityId  = r.Table.Columns.Contains("entity_id")         && r["entity_id"]         != DBNull.Value ? Convert.ToInt32(r["entity_id"])         : (int?)null,
        Link      = r.Table.Columns.Contains("link")                                                           ? r["link"]?.ToString()                  : null,
        Recipients= r.Table.Columns.Contains("recipients")                                                     ? r["recipients"]?.ToString()            : null,
        RecipientId = r.Table.Columns.Contains("recipient_id")    && r["recipient_id"]      != DBNull.Value ? Convert.ToInt32(r["recipient_id"])      : (int?)null,
        Recipient   = r.Table.Columns.Contains("recipient")                                                     ? r["recipient"]?.ToString()             : null,
        SenderId    = r.Table.Columns.Contains("sender_id")       && r["sender_id"]         != DBNull.Value ? Convert.ToInt32(r["sender_id"])         : (int?)null,
        Sender      = r.Table.Columns.Contains("sender")                                                         ? r["sender"]?.ToString()                : null,
        AckedByUserId = r.Table.Columns.Contains("acked_by_user_id") && r["acked_by_user_id"] != DBNull.Value ? Convert.ToInt32(r["acked_by_user_id"]) : (int?)null,
        AckedAt    = r.Table.Columns.Contains("acked_at")         && r["acked_at"]          != DBNull.Value ? Convert.ToDateTime(r["acked_at"])       : (DateTime?)null,
        MutedUntil = r.Table.Columns.Contains("muted_until")      && r["muted_until"]       != DBNull.Value ? Convert.ToDateTime(r["muted_until"])    : (DateTime?)null,
        IpAddress  = r.Table.Columns.Contains("ip_address")                                                      ? r["ip_address"]?.ToString()            : null,
        DeviceInfo = r.Table.Columns.Contains("device_info")                                                     ? r["device_info"]?.ToString()           : null,
        SessionId  = r.Table.Columns.Contains("session_id")                                                     ? r["session_id"]?.ToString()            : null,
        CreatedAt  = r.Table.Columns.Contains("created_at")       && r["created_at"]        != DBNull.Value ? Convert.ToDateTime(r["created_at"])     : DateTime.UtcNow,
        UpdatedAt  = r.Table.Columns.Contains("updated_at")       && r["updated_at"]        != DBNull.Value ? Convert.ToDateTime(r["updated_at"])     : (DateTime?)null
    };

    return n;
}

#endregion
#region === 36 · PREVENTIVE MAINTENANCE PLANS (PPM) ================================

/*  ==============================================================================
    Preventive Maintenance Plans (PPM) – schema-tolerant data access layer
    ------------------------------------------------------------------------------
    This region provides exactly what your PpmViewModel calls need:

      • GetAllPpmPlansAsync()
      • GetPpmPlanByIdAsync(int id)
      • InsertOrUpdatePpmPlanAsync(PreventiveMaintenancePlan plan, bool update, …)
        - plus an overload that matches your current two-parameter call
      • DeletePpmPlanAsync(int id, …) 
        - plus a simple overload matching your current call
      • ParsePpmPlan(DataRow) mapper

    It is defensive against table/column differences. By default it targets
    the table name "ppm_plans", and will fall back to "preventive_maintenance_plans"
    if the former does not exist (runtime fallback; compile-time safe).
    ============================================================================== */

/// <summary>
/// Attempts to detect the actual PPM table name at runtime. First tries <c>ppm_plans</c>,
/// and falls back to <c>preventive_maintenance_plans</c> if the first one is missing.
/// </summary>
private async Task<string> ResolvePpmTableNameAsync(CancellationToken token = default)
{
    try
    {
        // If table exists, this should succeed instantly.
        await ExecuteSelectAsync("SELECT 1 FROM ppm_plans LIMIT 1", null, token).ConfigureAwait(false);
        return "ppm_plans";
    }
    catch (MySqlException ex) when (ex.Number == 1146) // table doesn't exist
    {
        // Try alternative canonical name
        await ExecuteSelectAsync("SELECT 1 FROM preventive_maintenance_plans LIMIT 1", null, token).ConfigureAwait(false);
        return "preventive_maintenance_plans";
    }
}

/// <summary>
/// Returns all preventive maintenance plans ordered by next due date (then by name).
/// </summary>
public async Task<List<PreventiveMaintenancePlan>> GetAllPpmPlansAsync(CancellationToken token = default)
{
    string table = "ppm_plans";
    try
    {
        table = await ResolvePpmTableNameAsync(token).ConfigureAwait(false);
    }
    catch
    {
        // If both checks failed, we still attempt the default table (will throw at runtime).
    }

    var dt = await ExecuteSelectAsync(
        $"SELECT * FROM {table} ORDER BY COALESCE(next_due, '9999-12-31'), name",
        null, token).ConfigureAwait(false);

    var list = new List<PreventiveMaintenancePlan>();
    foreach (DataRow r in dt.Rows) list.Add(ParsePpmPlan(r));
    return list;
}

/// <summary>
/// Returns a single preventive maintenance plan by primary key.
/// </summary>
public async Task<PreventiveMaintenancePlan?> GetPpmPlanByIdAsync(int id, CancellationToken token = default)
{
    string table = "ppm_plans";
    try { table = await ResolvePpmTableNameAsync(token).ConfigureAwait(false); } catch { /* no-op */ }

    var dt = await ExecuteSelectAsync(
        $"SELECT * FROM {table} WHERE id=@id",
        new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

    return dt.Rows.Count == 0 ? null : ParsePpmPlan(dt.Rows[0]);
}

/// <summary>
/// Inserts or updates a preventive maintenance plan (schema tolerant).
/// </summary>
/// <param name="plan">Plan to insert or update (required).</param>
/// <param name="update">If <c>true</c>, updates existing row; otherwise inserts a new row.</param>
/// <param name="actorUserId">Audit user id for system event logging (defaults to 1).</param>
/// <param name="ip">Source IP for audit trail (optional).</param>
/// <param name="device">Device info for audit trail (optional).</param>
/// <param name="sessionId">Session id for audit trail (optional).</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Primary key of the inserted/updated plan.</returns>
public async Task<int> InsertOrUpdatePpmPlanAsync(
    PreventiveMaintenancePlan plan,
    bool update,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    if (plan is null) throw new ArgumentNullException(nameof(plan));

    string table = "ppm_plans";
    try { table = await ResolvePpmTableNameAsync(token).ConfigureAwait(false); } catch { /* no-op */ }

    // Minimal, broadly compatible column set. Add more if your schema supports them.
    string sql = !update
        ? $@"INSERT INTO {table}
            (code, name, description, next_due, status, responsible_user_id, last_modified, last_modified_by_id, source_ip, session_id, geo_location, digital_signature)
            VALUES
            (@code,@name,@desc,@next,@status,@resp,   NOW(),@lmby,@ip,@sess,@geo,@dsig)"
        : $@"UPDATE {table} SET
            code=@code, name=@name, description=@desc, next_due=@next, status=@status, responsible_user_id=@resp,
            last_modified=NOW(), last_modified_by_id=@lmby, source_ip=@ip, session_id=@sess, geo_location=@geo, digital_signature=@dsig
            WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@code",  (object?) (plan.Code ?? string.Empty)),
        new("@name",  (object?) (plan.Name ?? string.Empty)),
        new("@desc",  (object?) (plan.Description ?? string.Empty)),
        new("@next",  (object?) plan.NextDue ?? DBNull.Value),
        new("@status",(object?) (plan.Status ?? "active")),
        new("@resp",  (object?) plan.ResponsibleUserId ?? DBNull.Value),
        new("@lmby",  (object?) plan.LastModifiedById ?? actorUserId),
        new("@ip",    (object?) (plan.SourceIp ?? ip ?? string.Empty)),
        new("@sess",  (object?) (plan.SessionId ?? sessionId ?? string.Empty)),
        new("@geo",   (object?) (plan.GeoLocation ?? string.Empty)),
        new("@dsig",  (object?) (plan.DigitalSignature ?? string.Empty))
    };
    if (update) pars.Add(new MySqlParameter("@id", plan.Id));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int id = update
        ? plan.Id
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // Lightweight audit/system event (reuses your existing logging pattern)
    await LogSystemEventAsync(actorUserId, update ? "PPM_UPDATE" : "PPM_CREATE", "ppm_plans", "PPM",
        id, update ? $"PPM updated: {plan.Name}" : $"PPM created: {plan.Name}",
        ip, "audit", device, sessionId).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Overload kept for backwards-compatibility with current ViewModel calls
/// (<c>InsertOrUpdatePpmPlanAsync(plan, isUpdate)</c>).
/// </summary>
public Task<int> InsertOrUpdatePpmPlanAsync(
    PreventiveMaintenancePlan plan,
    bool update,
    CancellationToken token)
    => InsertOrUpdatePpmPlanAsync(plan, update, actorUserId: 1, ip: null, device: null, sessionId: null, token);

/// <summary>
/// Deletes a preventive maintenance plan by id (soft/hard delete depending on schema).
/// </summary>
public async Task DeletePpmPlanAsync(
    int id,
    int actorUserId = 1,
    string? ip = null,
    string? device = null,
    string? sessionId = null,
    CancellationToken token = default)
{
    string table = "ppm_plans";
    try { table = await ResolvePpmTableNameAsync(token).ConfigureAwait(false); } catch { /* no-op */ }

    // Try soft delete first; if that fails (no column), hard delete.
    int affected = 0;
    try
    {
        affected = await ExecuteNonQueryAsync(
            $@"UPDATE {table} SET status='deleted', last_modified=NOW(), last_modified_by_id=@uid WHERE id=@id",
            new[] { new MySqlParameter("@uid", actorUserId), new MySqlParameter("@id", id) }, token
        ).ConfigureAwait(false);
    }
    catch (MySqlException)
    {
        // If UPDATE failed due to schema, fall back to hard delete.
    }

    if (affected == 0)
    {
        await ExecuteNonQueryAsync($"DELETE FROM {table} WHERE id=@id",
            new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    }

    await LogSystemEventAsync(actorUserId, "PPM_DELETE", "ppm_plans", "PPM",
        id, "PPM deleted", ip, "audit", device, sessionId).ConfigureAwait(false);
}

/// <summary>
/// Overload kept for backwards-compatibility with current ViewModel calls
/// (<c>DeletePpmPlanAsync(selectedPlan.Id)</c>).
/// </summary>
public Task DeletePpmPlanAsync(int id)
    => DeletePpmPlanAsync(id, actorUserId: 1, ip: null, device: null, sessionId: null, token: default);

/// <summary>
/// DataRow → <see cref="PreventiveMaintenancePlan"/> (schema-tolerant).
/// Handles both <c>ppm_plans</c> and <c>preventive_maintenance_plans</c> variants.
/// </summary>
private static PreventiveMaintenancePlan ParsePpmPlan(DataRow r)
{
    var p = new PreventiveMaintenancePlan
    {
        Id        = r.Table.Columns.Contains("id")                && r["id"]                != DBNull.Value ? Convert.ToInt32(r["id"])                : 0,
        Code      = r.Table.Columns.Contains("code")                                                           ? r["code"]?.ToString()                   ?? string.Empty : string.Empty,
        Name      = r.Table.Columns.Contains("name")                                                           ? r["name"]?.ToString()                   ?? string.Empty : string.Empty,
        Description = r.Table.Columns.Contains("description")                                                  ? r["description"]?.ToString()            ?? string.Empty : string.Empty,
        MachineId = r.Table.Columns.Contains("machine_id")         && r["machine_id"]         != DBNull.Value ? Convert.ToInt32(r["machine_id"])         : (int?)null,
        ComponentId = r.Table.Columns.Contains("component_id")     && r["component_id"]       != DBNull.Value ? Convert.ToInt32(r["component_id"])       : (int?)null,
        Frequency = r.Table.Columns.Contains("frequency")                                                     ? r["frequency"]?.ToString()               : null,
        ChecklistFile = r.Table.Columns.Contains("checklist_file")                                            ? r["checklist_file"]?.ToString()          : null,
        ResponsibleUserId = r.Table.Columns.Contains("responsible_user_id") && r["responsible_user_id"] != DBNull.Value ? Convert.ToInt32(r["responsible_user_id"]) : (int?)null,
        LastExecuted = r.Table.Columns.Contains("last_executed")   && r["last_executed"]      != DBNull.Value ? Convert.ToDateTime(r["last_executed"])   : (DateTime?)null,
        NextDue    = r.Table.Columns.Contains("next_due")          && r["next_due"]           != DBNull.Value ? Convert.ToDateTime(r["next_due"])        : (DateTime?)null,
        Status     = r.Table.Columns.Contains("status")                                                         ? r["status"]?.ToString()                  : null,
        RiskScore  = r.Table.Columns.Contains("risk_score")        && r["risk_score"]         != DBNull.Value ? Convert.ToDouble(r["risk_score"])        : (double?)null,
        AiRecommendation = r.Table.Columns.Contains("ai_recommendation")                                       ? r["ai_recommendation"]?.ToString()       : null,
        DigitalSignature = r.Table.Columns.Contains("digital_signature")                                       ? r["digital_signature"]?.ToString()       : null,
        LastModified = r.Table.Columns.Contains("last_modified")   && r["last_modified"]      != DBNull.Value ? Convert.ToDateTime(r["last_modified"])   : DateTime.UtcNow,
        LastModifiedById = r.Table.Columns.Contains("last_modified_by_id") && r["last_modified_by_id"] != DBNull.Value ? Convert.ToInt32(r["last_modified_by_id"]) : (int?)null,
        SourceIp   = r.Table.Columns.Contains("source_ip")                                                      ? r["source_ip"]?.ToString()               : null,
        SessionId  = r.Table.Columns.Contains("session_id")                                                     ? r["session_id"]?.ToString()              : null,
        GeoLocation= r.Table.Columns.Contains("geo_location")                                                   ? r["geo_location"]?.ToString()            : null,
        Version    = r.Table.Columns.Contains("version")          && r["version"]            != DBNull.Value ? Convert.ToInt32(r["version"])            : (int?)null,
        PreviousVersionId = r.Table.Columns.Contains("previous_version_id") && r["previous_version_id"] != DBNull.Value ? Convert.ToInt32(r["previous_version_id"]) : (int?)null,
        IsActiveVersion = r.Table.Columns.Contains("is_active_version") && r["is_active_version"] != DBNull.Value ? Convert.ToBoolean(r["is_active_version"]) : true,
        IsAutomated     = r.Table.Columns.Contains("is_automated")      && r["is_automated"]      != DBNull.Value ? Convert.ToBoolean(r["is_automated"])      : false,
        RequiresNotification = r.Table.Columns.Contains("requires_notification") && r["requires_notification"] != DBNull.Value ? Convert.ToBoolean(r["requires_notification"]) : false,
        AnomalyScore = r.Table.Columns.Contains("anomaly_score")   && r["anomaly_score"]       != DBNull.Value ? Convert.ToDouble(r["anomaly_score"])     : (double?)null,
        Note       = r.Table.Columns.Contains("note")                                                            ? r["note"]?.ToString()                    : null
    };

    return p;
}

#endregion
#region === 37 · QUALIFICATIONS ==============================================

/*
 * This region is written to be schema-tolerant and POCO-tolerant, mirroring the style used
 * elsewhere in DatabaseService (e.g., Scheduled Jobs, Parts). It removes any use of C# 9
 * record `with` expressions to avoid CS8858 when the domain type is a class.
 *
 * Conventions:
 * - Read properties via TryGet<T>(obj,"Prop") to survive different DTOs/POCOs.
 * - Parse DataRow → Qualification using SetIfExists(...) on whatever properties exist.
 * - System/audit logging uses the canonical LogSystemEventAsync (with compatibility overloads above).
 */

/// <summary>
/// Returns all qualifications. Optional flags try to populate related audit/docs if your
/// schema contains supplementary tables. Calls are compile-safe (best-effort).
/// </summary>
public async Task<List<Qualification>> GetAllQualificationsAsync(
    bool includeAudit = false,
    bool includeCertificates = false,
    bool includeAttachments = false,
    CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync(
        "SELECT * FROM qualifications ORDER BY `date` DESC, id DESC", null, token).ConfigureAwait(false);

    var list = new List<Qualification>();
    foreach (DataRow r in dt.Rows)
    {
        var q = ParseQualification(r);

        if (includeAudit)
        {
            try { q.AuditLogs = await GetQualificationAuditAsync(TryGet<int>(q, "Id") ?? 0, token).ConfigureAwait(false); }
            catch (MySqlException ex) when (ex.Number == 1146) { /* audit table missing → ignore */ }
        }

        if (includeCertificates)
        {
            // If your model contains certificate detail collections, load them here (best-effort).
            // Left noop for compile-safety across schemas.
        }

        if (includeAttachments)
        {
            // If your schema has qualification_documents, you can load them here. Noop by default.
        }

        list.Add(q);
    }
    return list;
}

/// <summary>
/// Inserts or updates a qualification row. Values are read via reflection helpers to keep
/// compatibility with slightly different DTO/POCO shapes.
/// </summary>
/// <param name="q">Qualification instance (any compatible POCO).</param>
/// <param name="update">If <c>true</c>, performs UPDATE; otherwise INSERT.</param>
/// <param name="signatureHash">Digital signature hash to persist/audit.</param>
/// <param name="actorUserId">User performing the change (for system event).</param>
/// <param name="ip">Source IP address.</param>
/// <param name="deviceInfo">Device information.</param>
/// <param name="sessionId">Optional session identifier.</param>
/// <param name="token">Cancellation token.</param>
public async Task<int> InsertOrUpdateQualificationAsync(
    Qualification q,
    bool update,
    string signatureHash,
    int actorUserId = 1,
    string ip = "system",
    string deviceInfo = "",
    string? sessionId = null,
    CancellationToken token = default)
{
    if (q is null) throw new ArgumentNullException(nameof(q));

    // Read values via tolerant reflection
    var id              = TryGet<int>(q, "Id") ?? 0;
    var code            = TryGetString(q, "Code") ?? string.Empty;
    var qType           = TryGetString(q, "QualificationType") ?? TryGetString(q, "Type") ?? string.Empty;
    var description     = TryGetString(q, "Description");
    var date            = TryGet<DateTime>(q, "Date") ?? DateTime.UtcNow;
    var expiryDate      = TryGet<DateTime>(q, "ExpiryDate");
    var status          = TryGetString(q, "Status") ?? "active";
    var machineId       = TryGet<int>(q, "MachineId");
    var componentId     = TryGet<int>(q, "ComponentId");
    var supplierId      = TryGet<int>(q, "SupplierId");
    var qualifiedById   = TryGet<int>(q, "QualifiedById");
    var approvedById    = TryGet<int>(q, "ApprovedById");
    var approvedAt      = TryGet<DateTime>(q, "ApprovedAt");
    var digitalSignature= TryGetString(q, "DigitalSignature") ?? signatureHash;
    var certificateNo   = TryGetString(q, "CertificateNumber");
    var note            = TryGetString(q, "Note");

    string sql = !update
        ? @"
INSERT INTO qualifications
(code, type, description, date, expiry_date, status, machine_id, component_id, supplier_id,
 qualified_by_id, approved_by_id, approved_at, digital_signature, certificate_number, note)
VALUES
(@code,@type,@desc,@date,@exp,@status,@mid,@cid,@sid,@qid,@aid,@aat,@sig,@cert,@note)"
        : @"
UPDATE qualifications SET
 code=@code, type=@type, description=@desc, date=@date, expiry_date=@exp, status=@status,
 machine_id=@mid, component_id=@cid, supplier_id=@sid,
 qualified_by_id=@qid, approved_by_id=@aid, approved_at=@aat,
 digital_signature=@sig, certificate_number=@cert, note=@note
WHERE id=@id";

    var pars = new List<MySqlParameter>
    {
        new("@code", code),
        new("@type", qType),
        new("@desc", (object?)description ?? DBNull.Value),
        new("@date", date),
        new("@exp",  (object?)expiryDate ?? DBNull.Value),
        new("@status", status),
        new("@mid",  (object?)machineId ?? DBNull.Value),
        new("@cid",  (object?)componentId ?? DBNull.Value),
        new("@sid",  (object?)supplierId ?? DBNull.Value),
        new("@qid",  (object?)qualifiedById ?? DBNull.Value),
        new("@aid",  (object?)approvedById ?? DBNull.Value),
        new("@aat",  (object?)approvedAt ?? DBNull.Value),
        new("@sig",  (object?)digitalSignature ?? DBNull.Value),
        new("@cert", (object?)certificateNo ?? DBNull.Value),
        new("@note", (object?)note ?? DBNull.Value)
    };
    if (update) pars.Add(new MySqlParameter("@id", id));

    await ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

    int newId = update
        ? id
        : Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // AUDIT + SYSTEM EVENT (no record-with usage → avoids CS8858)
    await LogQualificationAuditAsync(
        qualificationId: newId, userId: null, action: update ? "UPDATE" : "CREATE",
        ip: ip, deviceInfo: deviceInfo, sessionId: sessionId, note: signatureHash, token: token).ConfigureAwait(false);

    await LogSystemEventAsync(
        userId: actorUserId,
        eventType: update ? "QUAL_UPDATE" : "QUAL_CREATE",
        tableName: "qualifications",
        module: "QualificationModule",
        recordId: newId,
        description: string.IsNullOrWhiteSpace(code) ? qType : code,
        ip: ip,
        severity: "audit",
        deviceInfo: deviceInfo,
        sessionId: sessionId,
        token: token).ConfigureAwait(false);

    return newId;
}

/// <summary>
/// Adds a qualification (signature hash recorded). Parameter order matches your ViewModel calls.
/// </summary>
public Task<int> AddQualificationAsync(
    Qualification q,
    string signatureHash,
    string ip,
    string deviceInfo,
    string? sessionId,
    CancellationToken token = default)
{
    return InsertOrUpdateQualificationAsync(
        q, update: false, signatureHash: signatureHash, actorUserId: 1, ip: ip, deviceInfo: deviceInfo, sessionId: sessionId, token: token);
}

/// <summary>
/// Updates a qualification (signature hash recorded). Parameter order matches your ViewModel calls.
/// </summary>
public Task<int> UpdateQualificationAsync(
    Qualification q,
    string signatureHash,
    string ip,
    string deviceInfo,
    string? sessionId,
    CancellationToken token = default)
{
    return InsertOrUpdateQualificationAsync(
        q, update: true, signatureHash: signatureHash, actorUserId: 1, ip: ip, deviceInfo: deviceInfo, sessionId: sessionId, token: token);
}

/// <summary>Deletes (or soft-deletes) a qualification and writes audit/system logs.</summary>
public async Task DeleteQualificationAsync(
    int id,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    int affected = 0;
    try
    {
        affected = await ExecuteNonQueryAsync(
            "UPDATE qualifications SET status='deleted' WHERE id=@id",
            new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    }
    catch (MySqlException)
    {
        // If the table doesn't have 'status', we'll hard delete below.
    }

    if (affected == 0)
    {
        await ExecuteNonQueryAsync(
            "DELETE FROM qualifications WHERE id=@id",
            new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
    }

    await LogQualificationAuditAsync(id, null, "DELETE", ip, deviceInfo, sessionId, "Deleted", token).ConfigureAwait(false);
    await LogSystemEventAsync(null, "QUAL_DELETE", "qualifications", "QualificationModule",
        id, "Qualification deleted", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
}

/// <summary>Records a rollback intent for a qualification (audit + system event).</summary>
public async Task RollbackQualificationAsync(
    int id,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    await LogQualificationAuditAsync(id, null, "ROLLBACK", ip, deviceInfo, sessionId, "Rollback requested", token).ConfigureAwait(false);
    await LogSystemEventAsync(null, "QUAL_ROLLBACK", "qualifications", "QualificationModule",
        id, "Rollback requested", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
}

/// <summary>Exports qualifications; returns the export file path (fully logged).</summary>
public async Task<string> ExportQualificationsAsync(
    IEnumerable<Qualification> rows,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string format = "csv",
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
    string filePath = $"/export/qualifications_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (NULL,@fmt,'qualifications',@filter,@path,@ip,'Qualifications export')",
        new[]
        {
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(null, "QUAL_EXPORT", "qualifications", "QualificationModule",
        null, $"Exported {(rows?.Count() ?? 0)} qualifications to {filePath}.", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);

    return filePath;
}

/// <summary>
/// Writes a qualification audit row into <c>qualification_audit</c>; falls back to
/// <see cref="LogSystemEventAsync(int?,string,string?,string?,int?,string?,string?,string,string?,string?,string?,string?,string?,System.Threading.CancellationToken,bool)"/>
/// if the audit table is missing.
/// </summary>
public Task LogQualificationAuditAsync(
    Qualification? q,
    string action,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? signatureHash = null,
    CancellationToken token = default)
{
    int qualId = TryGet<int>(q, "Id") ?? 0;
    string note = TryGetString(q, "Code") ?? signatureHash ?? action;
    return LogQualificationAuditAsync(qualId, null, action, ip, deviceInfo, sessionId, note, token);
}

/// <summary>
/// Overload that accepts a qualification ID directly.
/// </summary>
public async Task LogQualificationAuditAsync(
    int qualificationId,
    int? userId,
    string action,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? note = null,
    CancellationToken token = default)
{
    try
    {
        await ExecuteNonQueryAsync(@"
INSERT INTO qualification_audit (qualification_id, user_id, action, description, timestamp, source_ip, device_info, session_id)
VALUES (@qid,@uid,@act,@desc,NOW(),@ip,@dev,@sess)",
            new[]
            {
                new MySqlParameter("@qid",  qualificationId),
                new MySqlParameter("@uid",  (object?)userId ?? DBNull.Value),
                new MySqlParameter("@act",  action ?? "UPDATE"),
                new MySqlParameter("@desc", (object?)note ?? DBNull.Value),
                new MySqlParameter("@ip",   ip ?? string.Empty),
                new MySqlParameter("@dev",  deviceInfo ?? string.Empty),
                new MySqlParameter("@sess", (object?)sessionId ?? DBNull.Value)
            }, token).ConfigureAwait(false);
    }
    catch (MySqlException ex) when (ex.Number == 1146) // audit table missing
    {
        await LogSystemEventAsync(userId, "QUAL_AUDIT_FALLBACK", "qualifications", "QualificationModule",
            qualificationId, $"{action}: {note}", ip, "warn", deviceInfo, sessionId, token: token).ConfigureAwait(false);
    }
}

/// <summary>
/// DataRow → Qualification (schema-tolerant). Uses <c>SetIfExists</c> to safely map whatever
/// properties exist on your domain model (e.g., <c>QualificationType</c> vs <c>Type</c>).
/// </summary>
private static Qualification ParseQualification(DataRow r)
{
    var q = Activator.CreateInstance<Qualification>();

    bool Has(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value;

    if (Has("id"))                SetIfExists(q, "Id", Convert.ToInt32(r["id"]));
    if (Has("code"))              SetIfExists(q, "Code", r["code"]?.ToString());
    if (Has("type"))
    {
        var t = r["type"]?.ToString();
        SetIfExists(q, "Type", t);
        SetIfExists(q, "QualificationType", t);
    }
    if (Has("description"))       SetIfExists(q, "Description", r["description"]?.ToString());
    if (Has("date"))              SetIfExists(q, "Date", Convert.ToDateTime(r["date"]));
    if (Has("expiry_date"))       SetIfExists(q, "ExpiryDate", Convert.ToDateTime(r["expiry_date"]));
    if (Has("status"))            SetIfExists(q, "Status", r["status"]?.ToString());
    if (Has("machine_id"))        SetIfExists(q, "MachineId", Convert.ToInt32(r["machine_id"]));
    if (Has("component_id"))      SetIfExists(q, "ComponentId", Convert.ToInt32(r["component_id"]));
    if (Has("supplier_id"))       SetIfExists(q, "SupplierId", Convert.ToInt32(r["supplier_id"]));
    if (Has("qualified_by_id"))   SetIfExists(q, "QualifiedById", Convert.ToInt32(r["qualified_by_id"]));
    if (Has("approved_by_id"))    SetIfExists(q, "ApprovedById", Convert.ToInt32(r["approved_by_id"]));
    if (Has("approved_at"))       SetIfExists(q, "ApprovedAt", Convert.ToDateTime(r["approved_at"]));
    if (Has("digital_signature")) SetIfExists(q, "DigitalSignature", r["digital_signature"]?.ToString());
    if (Has("certificate_number"))SetIfExists(q, "CertificateNumber", r["certificate_number"]?.ToString());
    if (Has("note"))              SetIfExists(q, "Note", r["note"]?.ToString());

    // Optional friendly field if present in some schemas
    if (Has("equipment_name"))    SetIfExists(q, "EquipmentName", r["equipment_name"]?.ToString());

    return (Qualification)q;
}

/// <summary>
/// Returns audit entries from <c>qualification_audit</c> as a strongly-typed list
/// (best-effort; tolerates a missing table).
/// </summary>
private async Task<List<QualificationAuditLog>> GetQualificationAuditAsync(
    int qualificationId,
    CancellationToken token = default)
{
    var list = new List<QualificationAuditLog>();

    var dt = await ExecuteSelectAsync(@"
SELECT id, qualification_id, user_id, action, description, timestamp
FROM qualification_audit
WHERE qualification_id=@id
ORDER BY timestamp DESC, id DESC",
        new[] { new MySqlParameter("@id", qualificationId) }, token).ConfigureAwait(false);

    foreach (DataRow r in dt.Rows)
    {
        var e = new QualificationAuditLog
        {
            Id = r.Table.Columns.Contains("id") && r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0,
            QualificationId = qualificationId,
            UserId = r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value ? Convert.ToInt32(r["user_id"]) : (int?)null,
            Action = r.Table.Columns.Contains("action") ? r["action"]?.ToString() : null,
            Description = r.Table.Columns.Contains("description") ? r["description"]?.ToString() : null,
            Timestamp = r.Table.Columns.Contains("timestamp") && r["timestamp"] != DBNull.Value ? Convert.ToDateTime(r["timestamp"]) : DateTime.UtcNow
        };
        list.Add(e);
    }

    return list;
}

#endregion
#region === 38 · RISK ASSESSMENTS ==============================================

/// <summary>
/// Returns all risk assessments (newest first if timestamps exist).
/// Schema-tolerant: safely maps only columns that are present.
/// </summary>
/// <param name="token">Cancellation token.</param>
public async Task<List<RiskAssessment>> GetAllRiskAssessmentsFullAsync(CancellationToken token = default)
{
    var dt = await ExecuteSelectAsync("SELECT * FROM risk_assessments ORDER BY id DESC", null, token).ConfigureAwait(false);
    var list = new List<RiskAssessment>();
    foreach (DataRow r in dt.Rows) list.Add(ParseRiskAssessment(r));
    return list;
}

/// <summary>
/// Initiates a new risk assessment row. Minimal required columns are inserted,
/// optional columns are included if present in your schema.
/// </summary>
/// <param name="ra">Risk assessment to insert.</param>
/// <param name="token">Cancellation token.</param>
/// <returns>Newly created identifier.</returns>
public async Task<int> InitiateRiskAssessmentAsync(
    RiskAssessment ra,
    CancellationToken token = default)
{
    if (ra is null) throw new ArgumentNullException(nameof(ra));

    // Basic INSERT – keep portable. Add the common fields your ViewModel populates.
    const string sql = @"
INSERT INTO risk_assessments
(code, title, description, category, area, status, assessed_by, assessed_at,
 severity, probability, detection, risk_score, risk_level, action_plan,
 device_info, session_id, ip_address, created_at, updated_at)
VALUES
(@code,@title,@desc,@cat,@area,@status,@assby,@assat,
 @sev,@prob,@det,@score,@level,@plan,
 @dev,@sess,@ip,NOW(),NOW())";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@code",   (object?) (ra.Code ?? string.Empty)),
        new MySqlParameter("@title",  (object?) (ra.Title ?? string.Empty)),
        new MySqlParameter("@desc",   (object?) (ra.Description ?? string.Empty)),
        new MySqlParameter("@cat",    (object?) (ra.Category ?? string.Empty)),
        new MySqlParameter("@area",   (object?) (ra.Area ?? string.Empty)),
        new MySqlParameter("@status", (object?) (ra.Status ?? "initiated")),
        new MySqlParameter("@assby",  (object?) (ra.AssessedBy ?? string.Empty)),
        new MySqlParameter("@assat",  (object?) ra.AssessedAt ?? DBNull.Value),
        new MySqlParameter("@sev",    ra.Severity),
        new MySqlParameter("@prob",   ra.Probability),
        new MySqlParameter("@det",    ra.Detection),
        new MySqlParameter("@score",  (object?) ra.RiskScore ?? DBNull.Value),
        new MySqlParameter("@level",  (object?) (ra.RiskLevel ?? "Low")),
        new MySqlParameter("@plan",   (object?) (ra.ActionPlan ?? string.Empty)),
        new MySqlParameter("@dev",    (object?) (ra.DeviceInfo ?? string.Empty)),
        new MySqlParameter("@sess",   (object?) (ra.SessionId ?? string.Empty)),
        new MySqlParameter("@ip",     (object?) (ra.IpAddress ?? "system"))
    }, token).ConfigureAwait(false);

    int id = Convert.ToInt32(await ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

    // Standard overload; ensure non-null fallbacks for network fields.
    await LogSystemEventAsync(
        userId: null,
        eventType: "RA_CREATE",
        tableName: "risk_assessments",
        module: "Risk",
        recordId: id,
        description: $"Risk assessment created: {ra.Title ?? string.Empty}",
        ip: ra.IpAddress ?? "system",
        severity: "audit",
        deviceInfo: ra.DeviceInfo ?? "N/A",
        sessionId: ra.SessionId
    ).ConfigureAwait(false);

    return id;
}

/// <summary>
/// Updates an existing risk assessment with full audit chain and forensics info.
/// </summary>
/// <param name="ra">Risk assessment payload.</param>
/// <param name="actorUserId">Acting user id.</param>
/// <param name="ip">Non-null IP.</param>
/// <param name="deviceInfo">Non-null device info.</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="token">Cancellation token.</param>
public async Task UpdateRiskAssessmentAsync(
    RiskAssessment ra,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    if (ra is null) throw new ArgumentNullException(nameof(ra));
    if (ra.Id <= 0) throw new ArgumentOutOfRangeException(nameof(ra.Id));

    const string sql = @"
UPDATE risk_assessments SET
    code=@code, title=@title, description=@desc, category=@cat, area=@area, status=@status,
    assessed_by=@assby, assessed_at=@assat,
    severity=@sev, probability=@prob, detection=@det,
    risk_score=@score, risk_level=@level,
    mitigation=@mit, action_plan=@plan,
    owner_id=@owner,
    approved_by_id=@apprby, approved_at=@apprat,
    review_date=@review,
    digital_signature=@sig,
    note=@note,
    device_info=@dev, session_id=@sess, ip_address=@ip,
    updated_at=NOW()
WHERE id=@id";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@code",   (object?) (ra.Code ?? string.Empty)),
        new MySqlParameter("@title",  (object?) (ra.Title ?? string.Empty)),
        new MySqlParameter("@desc",   (object?) (ra.Description ?? string.Empty)),
        new MySqlParameter("@cat",    (object?) (ra.Category ?? string.Empty)),
        new MySqlParameter("@area",   (object?) (ra.Area ?? string.Empty)),
        new MySqlParameter("@status", (object?) (ra.Status ?? "in_progress")),
        new MySqlParameter("@assby",  (object?) (ra.AssessedBy ?? string.Empty)),
        new MySqlParameter("@assat",  (object?) ra.AssessedAt ?? DBNull.Value),
        new MySqlParameter("@sev",    ra.Severity),
        new MySqlParameter("@prob",   ra.Probability),
        new MySqlParameter("@det",    ra.Detection),
        new MySqlParameter("@score",  (object?) ra.RiskScore ?? DBNull.Value),
        new MySqlParameter("@level",  (object?) (ra.RiskLevel ?? "Low")),
        new MySqlParameter("@mit",    (object?) (ra.Mitigation ?? string.Empty)),
        new MySqlParameter("@plan",   (object?) (ra.ActionPlan ?? string.Empty)),
        new MySqlParameter("@owner",  (object?) ra.OwnerId ?? DBNull.Value),
        new MySqlParameter("@apprby", (object?) ra.ApprovedById ?? DBNull.Value),
        new MySqlParameter("@apprat", (object?) ra.ApprovedAt ?? DBNull.Value),
        new MySqlParameter("@review", (object?) ra.ReviewDate ?? DBNull.Value),
        new MySqlParameter("@sig",    (object?) (ra.DigitalSignature ?? string.Empty)),
        new MySqlParameter("@note",   (object?) (ra.Note ?? string.Empty)),
        new MySqlParameter("@dev",    (object?) (deviceInfo ?? string.Empty)),
        new MySqlParameter("@sess",   (object?) (sessionId ?? string.Empty)),
        new MySqlParameter("@ip",     (object?) (ip ?? string.Empty)),
        new MySqlParameter("@id",     ra.Id)
    }, token).ConfigureAwait(false);

    await LogRiskAssessmentAuditAsync(
        ra, action: "UPDATE", ip: ip, deviceInfo: deviceInfo, sessionId: sessionId, note: null, userId: actorUserId, token: token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        actorUserId, "RA_UPDATE", "risk_assessments", "Risk",
        ra.Id, $"Risk assessment updated: {ra.Title ?? string.Empty}", ip, "audit", deviceInfo, sessionId
    ).ConfigureAwait(false);
}

/// <summary>
/// Approves a risk assessment, moving it to effectiveness check (or your preferred next status).
/// Also stamps approver fields and full audit.
/// </summary>
public async Task ApproveRiskAssessmentAsync(
    int id,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    CancellationToken token = default)
{
    const string sql = @"
UPDATE risk_assessments SET
    status='effectiveness_check',
    approved_by_id=@uid,
    approved_at=NOW(),
    device_info=@dev,
    session_id=@sess,
    ip_address=@ip,
    updated_at=NOW()
WHERE id=@id";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@uid",  actorUserId),
        new MySqlParameter("@dev",  (object?) (deviceInfo ?? string.Empty)),
        new MySqlParameter("@sess", (object?) (sessionId ?? string.Empty)),
        new MySqlParameter("@ip",   (object?) (ip ?? string.Empty)),
        new MySqlParameter("@id",   id)
    }, token).ConfigureAwait(false);

    await LogRiskAssessmentAuditAsync(
        new RiskAssessment { Id = id }, "APPROVE", ip, deviceInfo, sessionId, note: "Approved", userId: actorUserId, token: token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        actorUserId, "RA_APPROVE", "risk_assessments", "Risk",
        id, "Risk assessment approved", ip, "audit", deviceInfo, sessionId
    ).ConfigureAwait(false);
}

/// <summary>
/// Closes a risk assessment after effectiveness verification.
/// </summary>
public async Task CloseRiskAssessmentAsync(
    int id,
    int actorUserId,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? reason = null,
    CancellationToken token = default)
{
    const string sql = @"
UPDATE risk_assessments SET
    status='closed',
    device_info=@dev,
    session_id=@sess,
    ip_address=@ip,
    note = CASE WHEN @reason IS NULL OR @reason = '' THEN note ELSE @reason END,
    updated_at=NOW()
WHERE id=@id";

    await ExecuteNonQueryAsync(sql, new[]
    {
        new MySqlParameter("@dev",    (object?) (deviceInfo ?? string.Empty)),
        new MySqlParameter("@sess",   (object?) (sessionId ?? string.Empty)),
        new MySqlParameter("@ip",     (object?) (ip ?? string.Empty)),
        new MySqlParameter("@reason", (object?) (reason ?? string.Empty)),
        new MySqlParameter("@id",     id)
    }, token).ConfigureAwait(false);

    await LogRiskAssessmentAuditAsync(
        new RiskAssessment { Id = id }, "CLOSE", ip, deviceInfo, sessionId, note: reason, userId: actorUserId, token: token
    ).ConfigureAwait(false);

    await LogSystemEventAsync(
        actorUserId, "RA_CLOSE", "risk_assessments", "Risk",
        id, $"Risk assessment closed. {reason}", ip, "audit", deviceInfo, sessionId
    ).ConfigureAwait(false);
}

/// <summary>
/// Records an export of risk assessments; returns the export file path and writes an audit event.
/// </summary>
public async Task<string> ExportRiskAssessmentsAsync(
    IEnumerable<RiskAssessment> rows,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string format = "zip",
    int actorUserId = 1,
    CancellationToken token = default)
{
    string fmt = string.IsNullOrWhiteSpace(format) ? "zip" : format.ToLowerInvariant();
    string filePath = $"/export/risk_assessments_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

    await ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (@uid,@fmt,'risk_assessments',@filter,@path,@ip,'Risk assessments export')",
        new[]
        {
            new MySqlParameter("@uid", actorUserId),
            new MySqlParameter("@fmt", fmt),
            new MySqlParameter("@filter", $"count={(rows?.Count() ?? 0)}"),
            new MySqlParameter("@path", filePath),
            new MySqlParameter("@ip", ip)
        }, token).ConfigureAwait(false);

    await LogSystemEventAsync(
        actorUserId, "RA_EXPORT", "risk_assessments", "Risk",
        null, $"Exported {(rows?.Count() ?? 0)} risk assessments to {filePath}.", ip, "audit", deviceInfo, sessionId
    ).ConfigureAwait(false);

    return filePath;
}

/// <summary>
/// Low-level audit helper for risk assessments; attempts to write into <c>risk_assessment_audit</c>,
/// and falls back to <see cref="LogSystemEventAsync"/> if the table is absent.
/// Accepts nullable networking fields and normalizes them internally to avoid CS8604 warnings.
/// </summary>
/// <param name="ra">Risk assessment (nullable: only Id is used when provided).</param>
/// <param name="action">Audit action.</param>
/// <param name="ip">Source IP address (nullable; normalized to empty when missing).</param>
/// <param name="deviceInfo">Device information (nullable; normalized to empty when missing).</param>
/// <param name="sessionId">Optional session id.</param>
/// <param name="note">Optional note.</param>
/// <param name="userId">Optional user id.</param>
/// <param name="token">Cancellation token.</param>
public async Task LogRiskAssessmentAuditAsync(
    RiskAssessment? ra,
    string action,
    string? ip,
    string? deviceInfo,
    string? sessionId = null,
    string? note = null,
    int? userId = null,
    CancellationToken token = default)
{
    int raId = ra?.Id ?? 0;
    string normIp = ip ?? string.Empty;
    string normDev = deviceInfo ?? string.Empty;

    try
    {
        await ExecuteNonQueryAsync(@"
INSERT INTO risk_assessment_audit (risk_assessment_id, user_id, action, note, source_ip, device_info, session_id)
VALUES (@rid,@uid,@action,@note,@ip,@dev,@sess)",
            new[]
            {
                new MySqlParameter("@rid",  (object?) raId),
                new MySqlParameter("@uid",  (object?) userId ?? DBNull.Value),
                new MySqlParameter("@action", action ?? "UPDATE"),
                new MySqlParameter("@note", (object?) note ?? DBNull.Value),
                new MySqlParameter("@ip",   normIp),
                new MySqlParameter("@dev",  normDev),
                new MySqlParameter("@sess", (object?) sessionId ?? DBNull.Value)
            }, token).ConfigureAwait(false);
    }
    catch (MySqlException ex) when (ex.Number == 1146) // table doesn't exist
    {
        await LogSystemEventAsync(
            userId, "RA_AUDIT_FALLBACK", "risk_assessments", "Risk",
            raId, $"{action}: {note}", normIp, "warn", normDev, sessionId
        ).ConfigureAwait(false);
    }
}

/* ------------------------------ Parsers ----------------------------------- */

/// <summary>
/// DataRow → RiskAssessment (schema-tolerant mapping).
/// Coalesces textual fields to <see cref="string.Empty"/> to avoid CS8601 when the model
/// defines non-nullable strings.
/// </summary>
private static RiskAssessment ParseRiskAssessment(DataRow r)
{
    var ra = new RiskAssessment
    {
        Id          = r.Table.Columns.Contains("id")            && r["id"]            != DBNull.Value ? Convert.ToInt32(r["id"])            : 0,
        Code        = r.Table.Columns.Contains("code")                                            ? (r["code"]?.ToString() ?? string.Empty)          : string.Empty,
        Title       = r.Table.Columns.Contains("title")                                           ? (r["title"]?.ToString() ?? string.Empty)         : string.Empty,
        Description = r.Table.Columns.Contains("description")                                     ? (r["description"]?.ToString() ?? string.Empty)   : string.Empty,
        Category    = r.Table.Columns.Contains("category")                                        ? (r["category"]?.ToString() ?? string.Empty)      : string.Empty,
        Area        = r.Table.Columns.Contains("area")                                            ? (r["area"]?.ToString() ?? string.Empty)          : string.Empty,
        Status      = r.Table.Columns.Contains("status")                                          ? (r["status"]?.ToString() ?? "initiated")         : "initiated",
        AssessedBy  = r.Table.Columns.Contains("assessed_by")                                     ? (r["assessed_by"]?.ToString() ?? string.Empty)   : string.Empty,
        AssessedAt  = r.Table.Columns.Contains("assessed_at")  && r["assessed_at"]  != DBNull.Value ? Convert.ToDateTime(r["assessed_at"])           : (DateTime?)null,
        Severity    = r.Table.Columns.Contains("severity")     && r["severity"]     != DBNull.Value ? Convert.ToInt32(r["severity"])                 : 0,
        Probability = r.Table.Columns.Contains("probability")  && r["probability"]  != DBNull.Value ? Convert.ToInt32(r["probability"])              : 0,
        Detection   = r.Table.Columns.Contains("detection")    && r["detection"]    != DBNull.Value ? Convert.ToInt32(r["detection"])                : 0,
        RiskScore   = r.Table.Columns.Contains("risk_score")   && r["risk_score"]   != DBNull.Value ? Convert.ToInt32(r["risk_score"])               : (int?)null,
        RiskLevel   = r.Table.Columns.Contains("risk_level")                                      ? (r["risk_level"]?.ToString() ?? "Low")           : "Low",
        Mitigation  = r.Table.Columns.Contains("mitigation")                                      ? (r["mitigation"]?.ToString() ?? string.Empty)    : string.Empty,
        ActionPlan  = r.Table.Columns.Contains("action_plan")                                     ? (r["action_plan"]?.ToString() ?? string.Empty)   : string.Empty,
        OwnerId     = r.Table.Columns.Contains("owner_id")     && r["owner_id"]     != DBNull.Value ? Convert.ToInt32(r["owner_id"])                 : (int?)null,
        ApprovedById= r.Table.Columns.Contains("approved_by_id")&& r["approved_by_id"]!= DBNull.Value ? Convert.ToInt32(r["approved_by_id"])        : (int?)null,
        ApprovedAt  = r.Table.Columns.Contains("approved_at")  && r["approved_at"]  != DBNull.Value ? Convert.ToDateTime(r["approved_at"])           : (DateTime?)null,
        ReviewDate  = r.Table.Columns.Contains("review_date")  && r["review_date"]  != DBNull.Value ? Convert.ToDateTime(r["review_date"])           : (DateTime?)null,
        DigitalSignature = r.Table.Columns.Contains("digital_signature")                           ? (r["digital_signature"]?.ToString() ?? string.Empty) : string.Empty,
        Note        = r.Table.Columns.Contains("note")                                             ? (r["note"]?.ToString() ?? string.Empty)          : string.Empty,
        DeviceInfo  = r.Table.Columns.Contains("device_info")                                      ? (r["device_info"]?.ToString() ?? string.Empty)   : string.Empty,
        SessionId   = r.Table.Columns.Contains("session_id")                                       ? (r["session_id"]?.ToString() ?? string.Empty)    : string.Empty,
        IpAddress   = r.Table.Columns.Contains("ip_address")                                       ? (r["ip_address"]?.ToString() ?? string.Empty)    : string.Empty
    };

    return ra;
}

#endregion
#region === 39 · ROLLBACK / VERSIONING =====================================

/// <summary>
/// Performs a generic, auditable rollback by persisting the supplied <paramref name="oldJson"/> snapshot
/// and logging a forensic system event. This method is compile-safe across schemas:
/// it first tries to write to <c>entity_rollback_queue</c> (if present), otherwise falls back to
/// the centralized system event/audit logging. Use domain-specific rollback handlers to
/// actually apply the JSON to a table if/when implemented.
/// </summary>
/// <param name="entityName">Logical table/entity name (e.g., "scheduled_jobs").</param>
/// <param name="entityId">Primary key value as string (the DTO passes it as text).</param>
/// <param name="oldJson">Snapshot JSON to restore.</param>
public Task RollbackEntityAsync(string entityName, string entityId, string oldJson, CancellationToken token = default)
    => RollbackEntityAsync(entityName, entityId, oldJson, actorUserId: 0, ip: null, device: null, sessionId: null, token);

/// <summary>
/// Full-context rollback with user/device/session metadata. Safe on any schema; if a generic
/// rollback queue table is missing, the operation is still audited in <c>system_events</c>.
/// </summary>
/// <remarks>
/// Expected optional table:
/// <c>entity_rollback_queue(id, entity, entity_id, snapshot_json, requested_by, requested_at, source_ip, device_info, session_id)</c>.
/// </remarks>
public async Task RollbackEntityAsync(
    string entityName,
    string entityId,
    string oldJson,
    int actorUserId,
    string? ip,
    string? device,
    string? sessionId,
    CancellationToken token = default)
{
    if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (string.IsNullOrWhiteSpace(entityId))   throw new ArgumentException("Entity id is required.", nameof(entityId));
    if (string.IsNullOrWhiteSpace(oldJson))    throw new ArgumentException("Rollback snapshot JSON is required.", nameof(oldJson));

    try
    {
        // Try to persist a rollback request row (schema optional; compile-safe).
        await ExecuteNonQueryAsync(@"
INSERT INTO entity_rollback_queue (entity, entity_id, snapshot_json, requested_by, requested_at, source_ip, device_info, session_id)
VALUES (@e,@id,@json,@uid,NOW(),@ip,@dev,@sess)",
            new[]
            {
                new MySqlParameter("@e",   entityName),
                new MySqlParameter("@id",  entityId),
                new MySqlParameter("@json", oldJson),
                new MySqlParameter("@uid",  actorUserId),
                new MySqlParameter("@ip",   (object?)ip ?? DBNull.Value),
                new MySqlParameter("@dev",  (object?)device ?? DBNull.Value),
                new MySqlParameter("@sess", (object?)sessionId ?? DBNull.Value)
            }, token).ConfigureAwait(false);
    }
    catch (MySqlException ex) when (ex.Number == 1146) // table doesn't exist
    {
        // Fall back to system event only; still fully audited.
        await LogSystemEventAsync(actorUserId, "ROLLBACK_REQUEST", entityName, "Rollback",
            recordId: null,
            description: $"Rollback requested for {entityName}#{entityId} (snapshot length {oldJson.Length} bytes).",
            ip: ip, severity: "audit", deviceInfo: device, sessionId: sessionId).ConfigureAwait(false);
        return;
    }

    // Also write a high-level system event for traceability.
    await LogSystemEventAsync(actorUserId, "ROLLBACK_REQUEST_QUEUED", entityName, "Rollback",
        recordId: null,
        description: $"Queued rollback for {entityName}#{entityId}.",
        ip: ip, severity: "audit", deviceInfo: device, sessionId: sessionId).ConfigureAwait(false);
}

#endregion
#region === 40 · IDisposable =====================================
public void Dispose()
{
    // Per-call connections/commands are disposed; nothing to release here.
    GC.SuppressFinalize(this);
}
#endregion


public async Task LogIncidentAuditAsync(
    IncidentReport? report,
    string action,
    string ip,
    string deviceInfo,
    string? sessionId = null,
    string? description = null,
    CancellationToken token = default)
{
    // Be tolerant if report is null
    int? recordId = (report != null && report.Id > 0) ? report.Id : (int?)null;

    await LogSystemEventAsync(
        userId:      null,                                   // unknown here
        eventType:   (action ?? "ACTION").ToUpperInvariant(),
        tableName:   "incident_reports",
        module:      "IncidentReports",
        recordId:    recordId,
        description: description ?? action ?? "action",
        ip:          string.IsNullOrWhiteSpace(ip) ? "system" : ip,
        severity:    "audit",
        deviceInfo:  deviceInfo,
        sessionId:   sessionId
    ).ConfigureAwait(false);
}


/// <summary>
/// Legacy overload for older callers that don't pass <paramref name="actorUserId"/>.
/// Forwards to the canonical method with <c>actorUserId = 1</c>.
/// </summary>
public Task<int> InsertOrUpdateWorkOrderAsync(
    WorkOrder workOrder,
    bool isUpdate,
    string ip,
    string deviceInfo,
    CancellationToken token = default)
    => InsertOrUpdateWorkOrderAsync(workOrder, isUpdate, actorUserId: 1, ip: ip, device: deviceInfo, token);




public Task LogExceptionAsync(
    Exception ex,
    string module,
    string? table = null,
    int? recordId = null,
    string? ip = "system",
    string severity = "error",
    string? deviceInfo = "server",
    string? sessionId = null,
    CancellationToken token = default)
{
    return LogSystemEventAsync(
        userId: null,
        eventType: "Exception",
        tableName: table ?? "-",
        module: module,
        recordId: recordId,
        description: ex.ToString(), // includes message + stack + inner
        ip: ip ?? "system",
        severity: severity,
        deviceInfo: deviceInfo ?? "server",
        sessionId: sessionId,
        token: token
    );
}   // ✅ this was missing

	
/// <summary>
/// Legacy overload for older callers that don't pass <paramref name="actorUserId"/>.
/// Forwards to the canonical method with <c>actorUserId = 1</c>.
/// </summary>
public Task DeleteWorkOrderAsync(
    int id,
    string ip,
    string deviceInfo,
    string sessionId,
    CancellationToken token = default)
    => DeleteWorkOrderAsync(id, actorUserId: 1, ip: ip, device: deviceInfo, sessionId: sessionId, token);


}     // end namespace YasGMP.Services
	}
// ==============================================================================
//  EOF
// ==============================================================================