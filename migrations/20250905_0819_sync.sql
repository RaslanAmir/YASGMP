-- YASGMP sync migration (idempotent)
-- Generated at (UTC): 2025-09-05T08:19:00Z
-- Context: OBS-003 — Decide source of truth per mismatch
-- Summary: No MySQL schema changes required. 'logs' mismatch is SQLite-only.

-- Rationale:
-- - Code reference to `logs` appears only in Diagnostics/LogSinks/SQLiteLogSink.cs
--   and is created locally via SQLite `CREATE TABLE IF NOT EXISTS logs (...)`.
-- - No occurrences of `logs` in application MySQL SQL patterns.
-- - Table diffs show no missing columns across existing tables.

START TRANSACTION;
-- No-op. Kept to ensure migration pipelines record this decision.
COMMIT;

