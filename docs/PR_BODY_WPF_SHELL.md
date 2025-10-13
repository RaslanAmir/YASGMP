# WPF Shell (.NET 9) + Full YasGMP Integration (B1-style Docking/Ribbon)

This PR tracks the Windows-only WPF shell with Fluent.Ribbon + AvalonDock, SAP Business One style FormModes, and cross-cutting services (attachments, e-signatures, audit). Increment 2 focuses on toolbar i18n/a11y (Work Orders + Warehouse), CFL/Golden Arrow verification, and editor/dialog a11y.

## Acceptance Checklist

- [x] WPF project builds/runs (Ribbon + AvalonDock + layout persistence)
- [x] MAUI builds/runs unaffected (Windows target)
- [x] ModulesPane lists all modules and opens editors
- [x] ModuleRegistry uses EN↔HR resources for titles/categories/descriptions
- [x] Ribbon/Backstage/Home groups/buttons bind to resources; tooltips localized
- [x] Views expose AutomationProperties.Name and ToolTips for a11y/smoke hooks
- [x] B1 FormModes & command enablement wired across editors
- [x] CFL pickers + Golden Arrow navigation operational
- [x] Attachments DB-backed with upload/preview/download (hash, retention)
- [x] E-signature prompts on regulated saves; audit surfaced in Audit views
- [x] Smoke harness toggled via YASGMP_SMOKE and logs to %LOCALAPPDATA%/YasGMP/logs

### Increment 2 (2025-10-13)
- [x] Toolbar i18n/a11y pattern (CaptionKey/ToolTipKey/AutomationNameKey via ResourceStringConverter) applied to Work Orders + Warehouse.
- [x] Work Orders editor inputs expose AutomationProperties.Name; CFL dialog and Electronic Signature dialog buttons/fields carry a11y names.
- [x] Localized Work Orders ViewModel status/validation strings (EN/HR) via Loc helper + new resource keys.
- [x] Smoke harness adds tolerant checks for CFL dialog and Golden Arrow from Work Orders.

## Notes
- Target framework: net9.0-windows10.0.19041.0
- NuGets pinned: Fluent.Ribbon 11.x, AvalonDock 4.72.x, CTK.Mvvm 8.4.x
- Layout persistence via DockLayoutPersistenceService (MySQL-backed)

## Logs
- Build (WPF): logs/dotnet-build-wpf.txt
- Build (MAUI Windows): logs/dotnet-build-maui.txt
- Smoke: logs/dotnet-test-wpf-smoke.txt

