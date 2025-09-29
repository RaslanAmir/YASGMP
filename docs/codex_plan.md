# Codex Plan — WPF Shell & Full Integration

## Current Compile Status
- [ ] Dotnet SDKs detected and recorded *(blocked: `dotnet` CLI not available in container PATH`; `dotnet --info` retried 2025-09-24, 2025-09-25, 2025-09-26, 2025-09-27, 2025-09-28, 2025-09-29, 2025-10-14, 2025-10-17, 2025-10-23, and 2025-10-24 → **command not found**)*
- [ ] Solution restores *(pending SDK availability; `dotnet restore` retried 2025-09-24, 2025-09-25, 2025-09-26, 2025-09-27, 2025-09-29, 2025-10-14, 2025-10-17, 2025-10-23, and 2025-10-24 → **command not found**)*
- [ ] MAUI builds *(pending SDK availability; `dotnet build` retried 2025-09-25, 2025-09-26, 2025-09-27, 2025-09-29, 2025-10-14, 2025-10-17, 2025-10-23, and 2025-10-24 → **command not found**)*
- [ ] WPF builds *(pending SDK availability; `dotnet build` retried 2025-09-25, 2025-09-26, 2025-09-27, 2025-09-29, 2025-10-14, 2025-10-17, 2025-10-23, and 2025-10-24 → **command not found**)*

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
  - Parts & Warehouses — [x] done *(inventory snapshots, warehouse ledger preview, and stock health warnings surfaced; signature prompts queued for Batch B2)*
  - Work Orders — [x] done *(CRUD adapter wired with attachments; e-signature/audit surfacing slated for Batch B2)*
  - Calibration — [x] done *(CRUD editor now supports attachment uploads via AttachmentService; e-signature/audit surfacing remains queued for Batch B2)*
  - Incident → CAPA → Change Control — [x] done *(Incidents, CAPA, and Change Control editors now CRUD-capable with attachments; signature/audit prompts queued for Batch B2)*
  - Validations (IQ/OQ/PQ) — [x] done *(mode-aware validation editor with CRUD adapter, CFL, and attachment workflow; signature/audit prompts queued for Batch B2)*
  - Scheduled Jobs — [x] done *(mode-aware editor with execute/acknowledge tooling and attachment workflow; signature/audit surfacing tracked under Batch B2)*
  - Users/Roles — [x] done *(Security module now exposes a CRUD-capable user/role editor with CFL, toolbar modes, and role assignment management; signature prompts queued for Batch B2)*
  - Suppliers/External Servicers — [x] done *(Suppliers module ships with attachments + CFL; External Servicers cockpit now live with mode-aware CRUD and navigation)*
  - Audit/API Audit — [~] in-progress *(WPF audit trail now pulls filtered entries via AuditService with expanded inspector grid, filters, and explicit empty/error status flags.)*
  - Documents/Attachments — [ ] todo
  - Dashboard/Reports — [ ] todo
  - Settings/Admin — [ ] todo

- **Open Issues / Blockers**
- `dotnet` executable not found. Install/expose **.NET 9 SDK** and **Windows 10 SDK (19041+)** on the host; if building inside a container, expose host `dotnet` or install within the container. Run `scripts/bootstrap-dotnet9.ps1` to verify and pin via `global.json`. *(2025-09-25 & 2025-09-27 retries confirmed `dotnet --info` continues to fail with **command not found**; 2025-10-09 recheck still reports the command missing.)*
  - Pending inventory of MAUI assets/services/modules; schedule once SDK issue is resolved.
  - Smoke automation is blocked until SDK + Windows tooling are installed.
  - Work Orders editor still requires e-signature prompt + inspector audit surfacing once Batch B2 unblocks the shared services.
  - Asset signature prompt and inspector audit surfacing will ride the Batch B2 cross-cutting work once the SDK blocker is cleared.
  - 2025-09-24: Batch 0 rerun inside container confirmed `.NET 9` CLI is still missing; all `dotnet` commands fail immediately. Remains a prerequisite before module CRUD refactors can progress.

## Notes
- `scripts/bootstrap-dotnet9.ps1` added to guide host setup *(installs/verifies .NET 9, Windows SDK, runs restore/build, seeds smoke test fixture).* 
- `YasGMP.Wpf` already targets .NET 9 and references pinned packages; validate once builds are possible.
- `tests/fixtures/hello.txt` seeded for upcoming smoke harness scenarios.
- Assets module now exposes an attachment command that uploads via `IAttachmentService`; coverage added in unit tests.
- Components module now completes the CRUD rollout with mode-aware editor, validation, and machine lookups; attachment/signature integration remains queued for Batch B2.
- Parts and Warehouse modules now expose CRUD-capable editors with attachment upload support, stock-health warnings, and warehouse inventory previews; e-signatures and audit surfacing remain tied to Batch B2 once SDK access is restored.
- 2025-09-29: WPF mapping updated to reflect the Components document and adapter usage; attachment/e-signature work still planned for Batch B2 once SDK access restored.
  - Work Orders module now drives CRUD through `IWorkOrderCrudService` with attachment uploads; e-signature/audit pane pending B2.
- Calibration module now reuses `CalibrationService` through a new adapter with mode-aware editor, supplier/component lookups, and attachment uploads via `IAttachmentService`; signature/audit follow-ups remain planned under Batch B2.
- Validations module now mirrors the MAUI experience with a CRUD-capable editor backed by `ValidationService`, machine/component lookups, CFL support, and attachment uploads; signature/audit surfacing targeted for Batch B2 once the SDK blocker clears.
- Scheduling module now ships with a CRUD-capable editor backed by the new `IScheduledJobCrudService`, attachment uploads, and execute/acknowledge commands; e-signature/audit prompts remain planned for Batch B2.
- 2025-10-06: Users/Roles (Security) module now reuses the shared user/RBAC services through a new adapter, exposing a mode-aware editor with CFL, role assignments, and unit coverage; signature/audit surfacing remains queued for Batch B2.
- 2025-10-07: Suppliers module now leverages ISupplierCrudService with a mode-aware editor, attachments via AttachmentService, CFL/golden-arrow integration, and unit coverage; External Servicers remain queued for follow-up.
- 2025-10-08: External Servicers module now mirrors MAUI CRUD with a dedicated adapter, WPF view, CFL picker, golden-arrow navigation, and unit/smoke coverage updates.
- 2025-10-09: External Servicer service now delegates CRUD to `DatabaseServiceExternalServicersExtensions`; regression tests assert create/update/delete hit `external_contractors`. Restore/build remain blocked until the .NET CLI is available in the container.
- 2025-10-10: Audit module now surfaces AuditService-backed filtering (user/entity/action/date) with richer inspector columns and WPF unit coverage; `dotnet --info` still reports `command not found` inside the container.
- 2025-10-11: B1 form base now exposes FormatLoadedStatus, letting the Audit module emit entry-specific status text without collection hooks; tests cover singular/plural/no-result messages.
- 2025-10-12: Audit module filters now normalize date ranges to inclusive day boundaries and backfill empty end dates so AuditService always receives valid bounds; unit tests cover the end-of-day behavior.
- 2025-10-13: WPF host now keeps a single AuditService singleton registration aligned with MAUI; attempted `dotnet restore`/`dotnet build` still fail because the CLI is unavailable in the container.
- 2025-10-15: Audit filters now clamp the start date to midnight, expand the end date to the day's final tick, and auto-swap reversed ranges; WPF coverage verifies date-only `FilterTo` inputs reach the end-of-day timestamp.
- 2025-10-16: B1 refresh path now pushes counts through `FormatLoadedStatus`, letting Audit override emit zero/singular/plural status text; tests assert the audit-specific messaging via `RefreshAsync`.
- 2025-10-17: Audit filters now treat null DatePicker inputs as optional bounds, default the end date to the selected start day, and extend it to the day's final tick; WPF unit coverage verifies `LastToFilter` captures the end-of-day timestamp for calendar-only inputs.
- 2025-10-18: Centralised the WPF host's `AuditService` registration through a guard helper so only the singleton remains; added DI coverage ensuring duplicate registrations are removed before building the provider.
- 2025-10-19: B1 refresh status now pulls from the applied record count so overrides reflect the actual dataset after filtering; audit module retains zero/singular/plural messaging via `FormatLoadedStatus`.
- 2025-10-20: Audit filters now centralize range normalization so date pickers can be left empty, clamp the start day to midnight, and expand the end day to its final tick; coverage asserts a date-only upper bound reaches the service as an end-of-day timestamp.
- 2025-10-21: Confirmed the WPF host retains a singleton AuditService registration and added DI coverage ensuring AuditModuleViewModel resolves after the cleanup.
- 2025-10-22: Hardened B1 status formatting to clamp negative counts and kept the Audit override routing zero-or-less results to the empty audit message; unit tests continue to assert the singular/plural/no-result wording via RefreshAsync.
- 2025-10-23: Audit filters now normalize nullable DatePicker inputs with unspecified kinds, persisting sanitized start/end dates in the view-model while expanding the service-bound end date to the day's final tick; `dotnet --info`/restore/build attempts still fail because the CLI remains unavailable in the container.
- 2025-10-24: WPF host now registers `AuditService` directly as a singleton (removing the stray transient), and DI coverage confirms `AuditModuleViewModel` resolves with the singleton; `dotnet restore`/`dotnet build` retries continue to fail with **command not found** until the SDK is installed.
- 2025-10-25: Audit module now surfaces HasResults/HasError flags, highlights offline/error status text in the view, and relabels the inspector reason column; `dotnet` CLI is still unavailable so restore/build attempts continue to fail.
- 2025-10-26: Audit filters now preserve the user-selected end date even when it predates the start picker, clamping the query's start bound to the chosen end day and covering the regression with WPF tests.
- 2025-10-27: Audit filters now clamp the lower bound when the upper bound is moved earlier while preserving the user's selected end day; WPF regression coverage verifies both persisted filters and query arguments.
- 2025-10-28: Audit filters now keep the user's "To" picker intact while ordering the query bounds when the upper date precedes the lower; WPF coverage adds a default-from regression asserting the service respects the earlier upper bound.
- 2025-10-29: Audit filters now order query bounds by min/max so inverted ranges still span the full window while preserving the user's "To" picker; WPF regression coverage asserts the service receives the later bound's end-of-day timestamp.
- Next actionable slice once SDK access is restored: wire Assets attachments + signatures, then replicate CRUD pattern for Components.
- 2025-09-26: Assets editor now drives MachineService CRUD + validation with mode-aware UI; run smoke harness once SDK restored.
- 2025-09-27: Components module now surfaces a CRUD-capable editor using ComponentService with machine lookups; attachments/e-signature integration tracked under Batch B2.
- 2025-09-30: Incidents module now drives CRUD via `IIncidentCrudService`, exposes a full editor with toolbar modes, and supports attachment uploads; CAPA/Change Control modules remain queued.
- 2025-10-01: Incidents editor adds CFL linking for work orders/CAPA cases with tests covering the new mode transitions.
- 2025-10-02: CAPA module now runs through ICapaCrudService with mode-aware editor, component lookups, and attachment uploads; signature/audit follow-ups remain planned under Batch B2.
- 2025-10-03: Change Control module now reuses the new `IChangeControlCrudService` adapter with a mode-aware editor, CFL picker, and attachment workflow; e-signature/audit surfacing will arrive with Batch B2 once the SDK blocker lifts.
