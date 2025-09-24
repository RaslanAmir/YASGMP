# Codex Plan — WPF Shell & Full Integration

## Current Compile Status
- [ ] Dotnet SDKs detected and recorded *(blocked: `dotnet` CLI not available in container PATH)*
- [ ] Solution restores *(pending SDK availability)*
- [ ] MAUI builds *(pending SDK availability)*
- [ ] WPF builds *(pending SDK availability)*

## Decisions & Pins
- Preferred WPF target: **net9.0-windows10.0.19041.0** (existing project already targets .NET 9; retain once SDK accessible).
- AvalonDock version: **4.72.1** (already pinned in `YasGMP.Wpf.csproj`).
- Other NuGets: Fluent.Ribbon 11.0.1, CommunityToolkit.Mvvm 8.4.0, Microsoft.Extensions.* 9.0.3, MySqlConnector 2.4.0 (all pinned in csproj).
- Environment gap: install/enable .NET SDK tooling inside container before any build/test can occur.

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
- `dotnet` executable not found. Need to install or expose .NET 9 SDK in the build container before proceeding with restore/build/test steps.
- Pending inventory of MAUI assets/services/modules; schedule once SDK issue resolved.

## Notes
- Smoke automation not runnable until SDK/tooling installed.
- Existing `YasGMP.Wpf` project already targets .NET 9 and references pinned packages; validate once builds are possible.
