# Agent Operating Guide — YasGMP

**Scope:** Deliver and maintain the Windows-only WPF shell (Fluent.Ribbon + AvalonDock) with SAP Business One style form modes, DB-backed attachments, 21 CFR Part 11 e-signatures, immutable audit surfacing, and smoke automation — while keeping the existing MAUI app compiling and functional.

## Branch & PR Workflow
- Always work on the branch `feature/wpf-shell-full-yasgmp`.
- Prepare a PR titled **"WPF Shell (.NET 9) + Full YasGMP Integration (B1-style Docking/Ribbon)"** and keep its acceptance checklist updated.
- Commit in small, traceable batches (prefix: `feat:`, `fix:`, `chore:`, `docs:`) that leave the solution buildable.

## Batch Cadence (must follow)
After every batch:
1. `dotnet restore` (solution).
2. `dotnet build` for MAUI Windows and WPF targets.
3. Run the WPF smoke harness when available (fall back to logging TODO if environment prevents execution).
4. Update `docs/codex_plan.md` and `docs/codex_progress.json`.
5. Commit the changes.

## Persistent Memory Artifacts
- `docs/codex_plan.md`: human-readable plan, decisions, open issues, checklist.
- `docs/codex_progress.json`: machine-readable status for batches/modules/files.
- Update both after each batch without exception.

## Technical Guardrails
- Prefer **.NET 9** (`net9.0-windows`). If the SDK is unavailable, fall back to `net8.0-windows` **and document the reason**.
- Keep MAUI code intact; extract shared logic into `YasGMP.AppCore` or adapters as needed without breaking APIs.
- Use pinned NuGet versions. If a requested package version is not available, pick the nearest lower stable version and record the decision.
- For AvalonDock XAML use `xmlns:ad="http://schemas.xceed.com/wpf/xaml/avalondock"` and ensure the corresponding assemblies are referenced.

## Shell & Editor Expectations
- Ribbon tabs: Home / View / Tools (Fluent.Ribbon).
- Layout: Modules tree (left), Document host (center tabs), Inspector (right), Status bar (bottom).
- FormMode state machine matching SAP B1 semantics with commands: Find/Add/View/Update (plus OK/Cancel, navigation, attachments, signature).
- Replace read-only DataGrids with editable forms honoring FormMode enabling/disabling logic.
- Golden Arrow navigation and Choose-From-List pickers must be wired between related records.

## Cross-Cutting Services
- Implement reusable services for attachments (DB BLOB + SHA-256 + retention), e-signatures (re-auth + reason + manifest), audit logging (who/when/why, old→new, record hash, IP/host), and a debug smoke harness that exercises each module end-to-end.

## Module Rollout Pattern
For every module (Assets, Components, Parts, Warehouses, Work Orders, Calibration, Incident → CAPA → Change Control, Validations, Scheduled Jobs, Users/Roles, Suppliers, External Servicers, Audit/API Audit, Documents/Attachments, Dashboard/Reports, Settings/Admin):
1. Provide List + Editor views bound to `B1FormViewModel` derivatives.
2. Wire CRUD through existing domain services (extend without breaking MAUI).
3. Integrate attachments, e-signature, audit, CFL pickers, and Golden Arrow navigation.
4. Register module in `ModulesPane` and add to the smoke automation.

## Smoke Testing
- Project: `YasGMP.Wpf.Smoke` (xUnit + FlaUI.UIA3 if available).
- Automate login (darko/111), open each module, perform safe add/find/update cycles with attachments and signatures.
- Log results to `%LOCALAPPDATA%/YasGMP/logs/smoke-YYYYMMdd-HHmm.txt` and surface status in the Status bar.
- Respect `YASGMP_SMOKE=1` env var toggle; if DB unavailable, run in demo mode and record the limitation in `docs/codex_plan.md`.

## Acceptance Checklist (mirror in PR)
- WPF project builds/runs (Ribbon + AvalonDock + layout persistence).
- MAUI builds/runs unaffected.
- Shared AppCore extraction complete where required; adapters compiled.
- ModulesPane lists every module and opens feature-parity editors.
- B1 FormModes & command enablement wired across editors.
- CFL pickers + Golden Arrow navigation working.
- Work Orders, Calibration, and Quality workflows usable end-to-end.
- Warehouse ledger with running balance and alerts.
- Audit surfacing + e-signature prompts on critical saves.
- Attachments DB-backed with upload/preview/download.
- Scheduled Jobs, Users/Roles (RBAC), Dashboard KPIs, Reports TODOs documented.
- Smoke tests succeed and log output.
- `README_WPF_SHELL.md` and `/docs/WPF_MAPPING.md` kept current.

Adhere to this guide for all future modifications within this repository.
