<!-- Set PR title exactly: WPF Shell (.NET 9) + Full YasGMP Integration (B1-style Docking/Ribbon) -->

## Summary

Windows-only WPF shell with Fluent.Ribbon + AvalonDock, SAP B1-style modes, cross-cutting services, and smoke automation. This PR keeps MAUI compiling and functional while adding desktop shell capability and shared AppCore services.

## Batch Highlights (most recent)
- DB: Provision `attachment_embeddings` table; idempotent migration script added.
- Resilience: Graceful handling when embeddings table is missing (no crash; empty results).
- Validation: WPF + MAUI build green; WPF smoke passed and logged.

## Validation
- dotnet restore (solution): OK
- dotnet build (WPF Debug/Release): OK
- dotnet build (MAUI Windows Debug): OK
- WPF smoke (RUN_WPF_SMOKE=1, YASGMP_SMOKE=1): Passed; log written to `%LOCALAPPDATA%/YasGMP/logs`

## Acceptance Checklist
- [x] WPF project builds/runs (Ribbon + AvalonDock + layout persistence)
- [x] MAUI builds/runs unaffected (Windows target) — build verified; run to be confirmed on host
- [x] Shared AppCore extraction complete where required; adapters compiled
- [ ] ModulesPane lists every module and opens feature-parity editors
- [ ] B1 FormModes & command enablement wired across editors
- [ ] CFL pickers + Golden Arrow navigation working
- [ ] Work Orders, Calibration, and Quality workflows usable end-to-end
- [ ] Warehouse ledger with running balance and alerts
- [ ] Audit surfacing + e-signature prompts on critical saves
- [x] Attachments DB-backed with upload/preview/download
- [ ] Scheduled Jobs, Users/Roles (RBAC), Dashboard KPIs, Reports TODOs documented
- [x] Smoke tests succeed and log output
- [ ] README_WPF_SHELL.md and /docs/WPF_MAPPING.md kept current

## Notes
- .NET target: `net9.0-windows10.0.19041.0` across WPF/MAUI.
- NuGet pinned; nearest-lower fallback to be documented if feeds differ.
- Embeddings migration was applied via `tools/YasGMP.DbMigrator` using appsettings connection.

