using System;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// High‑level overview and usage guide for <see cref="DatabaseService"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>DatabaseService</c> is the single, centralized data‑access component for YasGMP.
    /// It encapsulates safe MySQL access (via MySqlConnector), parameterized SQL execution,
    /// resilient retry patterns, schema‑tolerant parsing, and canonical audit logging to
    /// <c>system_event_log</c>. It is intentionally written as a partial class and organized in
    /// regions so the codebase can evolve by functional area (CAPA, Deviations, Documents,
    /// Incidents, Inventory, IoT, etc.) without losing discoverability.
    /// </para>
    ///
    /// <para>
    /// Key design principles:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Safety first</b> — all commands are parameterized; no string interpolation of user data.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Schema tolerance</b> — data mappers check column presence before assignment, allowing
    /// the service to run against newer/older dumps while alignment is in progress.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Canonical audit</b> — sensitive actions log to <c>system_event_log</c> with rich context
    /// (table, record id, IP, device info, session id, severity) for 21 CFR Part 11 / Annex 11.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Transactions</b> — helpers for batched SQL and delegate‑based work ensure atomicity with
    /// commit/rollback and uniform tracing.
    /// </description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Thread‑safety: Each operation opens its own <see cref="MySqlConnector.MySqlConnection"/> and
    /// disposes it deterministically; the service itself is stateless and safe to register as
    /// a singleton in DI.
    /// </para>
    ///
    /// <para>
    /// Configuration: the constructor accepts a full ADO.NET connection string. For development,
    /// it can be injected from configuration key <c>ConnectionStrings:MySqlDb</c>. Example:
    /// </para>
    /// <code language="json">
    /// {
    ///   "ConnectionStrings": {
    ///     "MySqlDb": "Server=localhost;Database=yasgmp;User=root;Password=Jasenka1;Charset=utf8mb4;"
    ///   }
    /// }
    /// </code>
    ///
    /// <para>
    /// Typical usage from a ViewModel or service:
    /// </para>
    /// <code language="csharp">
    /// // Injected via DI
    /// private readonly DatabaseService _db;
    ///
    /// // Query
    /// var docs = await _db.GetAllDocumentsFullAsync();
    ///
    /// // Command with audit
    /// await _db.LogSystemEventAsync(
    ///     userId: currentUserId,
    ///     eventType: "EXPORT",
    ///     tableName: "sop_documents",
    ///     module: "DocControl",
    ///     recordId: null,
    ///     description: "Exported documents",
    ///     ip: clientIp,
    ///     severity: "audit",
    ///     deviceInfo: deviceInfo,
    ///     sessionId: sessionId);
    /// </code>
    ///
    /// <para>
    /// Error handling: all low‑level helpers (<c>Execute*</c>) write structured traces, wrap and
    /// rethrow errors, and also emit a diagnostic system event (DbError/TransactionFailed) with
    /// SQL and parameter fingerprints to aid forensics.
    /// </para>
    ///
    /// <para>
    /// Performance: reads stream through <see cref="System.Data.DataTable"/> for simplicity and
    /// robustness; heavy exports provide CSV paths instead of loading entire files in memory.
    /// </para>
    /// </remarks>
    public sealed partial class DatabaseService
    {
        // Documentation‑only partial. See other partial files for implementation.
    }
}


