# Codex Plan — WPF Shell & Full Integration

## Current Compile Status
- [ ] Dotnet SDKs detected and recorded *(blocked: `dotnet` CLI not available in container PATH` — host must install .NET 9 SDK via `winget install Microsoft.DotNet.SDK.9` or equivalent)*
- [ ] Solution restores *(pending SDK availability in execution environment)*
- [ ] MAUI builds *(pending SDK availability)*
- [ ] WPF builds *(pending SDK availability)*

## Decisions & Pins
- Preferred WPF target: **net9.0-windows10.0.19041.0** (existing project already targets .NET 9; retain once SDK accessible).
- Repo-level SDK pin: `global.json` set to **9.0.100** (reinforced via bootstrap script).
- AvalonDock version: **4.72.1** (already pinned in `YasGMP.Wpf.csproj`).
- Other NuGets: Fluent.Ribbon 11.0.1, CommunityToolkit.Mvvm 8.4.0, Microsoft.Extensions.* 9.0.3, MySqlConnector 2.4.0 (all pinned in csproj).
- Environment gap: install/enable .NET 9 SDK and Windows 10 SDK (19041+) on host before any build/test can occur.

## Batches
- B0 — Environment stabilization (SDKs, NuGets, XAML namespaces) — **blocked** (no `dotnet` CLI)
- B1 — Shell foundation (Ribbon, Docking, StatusBar, FormMode state machine) — [ ] todo
- B2 — Cross-cutting (Attachments DB, E-Signature, Audit) — [ ] todo
- B3 — Editor framework (templates, host, unsaved-guard) — [ ] todo
- B4+ — Module rollout:
  - Assets/Machines — [ ] todo
  - Components — [ ] todo
  - Parts & Warehouses — [ ] todo
  - Work Orders — [ ] todo
  - Calibration — [ ] todo
  - Incident → CAPA → Change Control — [ ] todo
  - Validations (IQ/OQ/PQ) — [ ] todo
  - Scheduled Jobs — [ ] todo
  - Users/Roles — [ ] todo
  - Suppliers/External Servicers — [ ] todo
  - Audit/API Audit — [ ] todo
  - Documents/Attachments — [ ] todo
  - Dashboard/Reports — [ ] todo
  - Settings/Admin — [ ] todo

## Open Issues / Blockers
- `dotnet` executable not found in Linux container. Host must run `scripts/bootstrap-dotnet9.ps1` (requires Windows PowerShell + elevated privileges) to install/configure SDKs before restore/build/test steps can continue.
- Pending inventory of MAUI assets/services/modules; schedule once SDK issue resolved.
- Smoke automation blocked until SDK + Windows tooling installed.

## Notes
- Added `scripts/bootstrap-dotnet9.ps1` to guide host setup (installs/verifies .NET 9, Windows SDK, restores/builds, seeds smoke test fixture).
- Smoke automation not runnable until SDK/tooling installed.
- Existing `YasGMP.Wpf` project already targets .NET 9 and references pinned packages; validate once builds are possible.
