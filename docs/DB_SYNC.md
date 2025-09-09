DB ↔ Code Sync: Analyzer, Snapshots, and CI

Overview
- The analyzer scans Services/ and Views/ for SQL, aggregates table/column usage, and compares it to the DB schema snapshot.
- Outputs are written to reports/, including mismatch inventory, per‑table diffs, and a consolidated SQL patch file.
- CI runs the analyzer on push/PR and can refresh the schema snapshot from a live DB.

Key Scripts/Tools
- scripts/analyze_db_sync.ps1 – runs the analyzer and writes reports.
- tools/SchemaSnapshot – .NET tool to refresh tools/schema/snapshots/schema.json from the live DB.
- tools/DbPatchApplier – .NET tool to apply SQL from reports/recommended_patches.sql.
- scripts/apply_recommended_patches.ps1 – builds and runs DbPatchApplier.

Common Commands
- Run analyzer: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/analyze_db_sync.ps1
- Apply patches: powershell -NoProfile -File scripts/apply_recommended_patches.ps1
- Refresh snapshot: dotnet run --project tools/SchemaSnapshot/SchemaSnapshot.csproj -c Release

CI Workflows
- .github/workflows/db-sync-analyzer.yml – runs analyzer on push/PR; uploads reports; fails on drift.
- .github/workflows/schema-snapshot.yml – guarded snapshot refresher; requires secrets.MYSQL_CS; runs analyzer after refresh; can commit if allowed.

Allowlist (optional)
- tools/schema/allowlist.json – ignore known, intentional differences.
- Example:
  {
    "IgnoreTables": ["example_table"],
    "IgnoreColumns": { "example_table": ["example_col"] }
  }

Playbook to Reach Zero Drift
1) Run analyzer → inspect reports/mismatch-matrix.csv.
2) Review diffs → adjust code or apply reports/recommended_patches.sql.
3) Apply patches → refresh snapshot → rerun analyzer.
4) Confirm SYNC_REPORT.md shows “Mismatches: 0”.

