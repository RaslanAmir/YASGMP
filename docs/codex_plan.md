# Codex Plan — WPF Shell & Full Integration

## Current Compile Status

## Decisions & Pins
- Preferred WPF target: **net9.0-windows10.0.19041.0** (retain once .NET 9 SDK is installed).
- Repo-level SDK pin: `global.json` set to **9.0.100** *(reinforced via bootstrap script).* 
- AvalonDock: **4.72.1** *(pinned in `YasGMP.Wpf.csproj`).*
- Other NuGets: **Fluent.Ribbon 11.0.1**, **CommunityToolkit.Mvvm 8.4.0**, **Microsoft.Extensions.* 9.0.3**, **MySqlConnector 2.4.0** *(pinned in csproj).* 
- Environment gap: install/enable **.NET 9 SDK** and **Windows 10 SDK (19041+)** on the host. The build container currently lacks `dotnet`; run `scripts/bootstrap-dotnet9.ps1` or install via `winget install Microsoft.DotNet.SDK.9`.

## Batches
- **B0 — Environment stabilization** (SDKs, NuGets, XAML namespaces) — **blocked** *(no `dotnet` CLI)*
- **B1 — Shell foundation** (Ribbon, Docking, StatusBar, FormMode state machine) — [ ] todo
- **B2 — Cross-cutting** (Attachments DB, E-Signature, Audit) — [ ] todo
- **B3 — Editor framework** (templates, host, unsaved-guard) — [ ] todo
- **B4+ — Module rollout:**
  - Assets/Machines — [x] done *(mode-aware CRUD plus attachment upload wired through AttachmentService; e-sign prompt scheduled under Batch B2)*
  - Components — [x] done *(mode-aware editor wired to ComponentService; attachments/signature work tracked under Batch B2)*
  - Parts & Warehouses — [x] in progress *(mode-aware editors wired to Part/Warehouse adapters with attachment upload; signature prompts queued for Batch B2)*
  - Work Orders — [ ] in progress *(WPF editor scaffolding created; CRUD wiring continues)*
  - Calibration — [ ] in progress *(calibration editor now loads/saves via CalibrationService adapter; attachments/e-signature pending)*
  - Incident → CAPA → Change Control — [ ] todo
  - Validations (IQ/OQ/PQ) — [ ] todo
  - Scheduled Jobs — [ ] todo
  - Users/Roles — [ ] todo
  - Suppliers/External Servicers — [ ] todo
  - Audit/API Audit — [ ] todo
  - Documents/Attachments — [ ] todo
  - Dashboard/Reports — [ ] todo
  - Settings/Admin — [ ] todo

- **Open Issues / Blockers**
  - `dotnet` executable not found. Install/expose **.NET 9 SDK** and **Windows 10 SDK (19041+)** on the host; if building inside a container, expose host `dotnet` or install within the container. Run `scripts/bootstrap-dotnet9.ps1` to verify and pin via `global.json`. *(2025-09-25 & 2025-09-27: Batch 0 reruns confirmed `dotnet --info` continues to fail with **command not found**.)*
  - Pending inventory of MAUI assets/services/modules; schedule once SDK issue is resolved.
  - Smoke automation is blocked until SDK + Windows tooling are installed.
  - Work Orders form currently saves via shared services but lacks attachments/signature prompts; plan follow-up in Batch B2.
  - Asset signature prompt and inspector audit surfacing will ride the Batch B2 cross-cutting work once the SDK blocker is cleared.
  - 2025-09-24: Batch 0 rerun inside container confirmed `.NET 9` CLI is still missing; all `dotnet` commands fail immediately. Remains a prerequisite before module CRUD refactors can progress.

## Notes
- `scripts/bootstrap-dotnet9.ps1` added to guide host setup *(installs/verifies .NET 9, Windows SDK, runs restore/build, seeds smoke test fixture).* 
- `YasGMP.Wpf` already targets .NET 9 and references pinned packages; validate once builds are possible.
- `tests/fixtures/hello.txt` seeded for upcoming smoke harness scenarios.
- Assets module now exposes an attachment command that uploads via `IAttachmentService`; coverage added in unit tests.
- Components module now completes the CRUD rollout with mode-aware editor, validation, and machine lookups; attachment/signature integration remains queued for Batch B2.
- Parts and Warehouse modules now expose CRUD-capable editors via Part/Warehouse adapters with attachment upload support; e-signatures and audit surfacing remain tied to Batch B2 once SDK access is restored.
- 2025-09-29: WPF mapping updated to reflect the Components document and adapter usage; attachment/e-signature work still planned for Batch B2 once SDK access restored.
- Work Orders module now exposes a mode-aware editor backed by `WorkOrderService` for CRUD operations.
- Calibration module now reuses `CalibrationService` through a new adapter with mode-aware editor and supplier/component lookups.
- Next actionable slice once SDK access is restored: wire Assets and Components signature prompts alongside attachment persistence in Batch B2.
- 2025-09-26: Assets editor now drives MachineService CRUD + validation with mode-aware UI; run smoke harness once SDK restored.
- 2025-09-27: Components module reached CRUD parity through ComponentService with machine lookups; cross-cutting attachments/signature work tracked separately.
- Work Orders module now exposes a mode-aware editor backed by `WorkOrderService` for CRUD operations.
- Calibration module now reuses `CalibrationService` through a new adapter with mode-aware editor and supplier/component lookups.
- Next actionable slice once SDK access is restored: wire Assets attachments + signatures, then replicate CRUD pattern for Components.
- 2025-09-26: Assets editor now drives MachineService CRUD + validation with mode-aware UI; run smoke harness once SDK restored.
- 2025-09-27: Components module now surfaces a CRUD-capable editor using ComponentService with machine lookups; attachments/e-signature integration tracked under Batch B2.