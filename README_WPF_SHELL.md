# YasGMP WPF shell

The YasGMP WPF shell hosts the desktop docking workspace used to surface cockpit modules on Windows. This guide documents the environment requirements, build steps, and extension points that differ from the cross-platform MAUI client.

## Prerequisites

- **Supported OS:** The WPF project targets `net9.0-windows10.0.19041.0`, so the shell requires Windows 10 build 19041 or later.
- **.NET SDK:** Install the .NET SDK pinned in `global.json` (`9.0.100`) to match the repo’s toolset.【F:global.json†L1-L6】
- **Configuration:** The shell reads configuration from `appsettings.json`. Override the defaults for per-user layouts and database access via:
  - `Shell:UserId` – numeric user identifier (defaults to `1`).【F:YasGMP.Wpf/Services/UserSession.cs†L17-L41】
  - `Shell:Username` / `Shell:FullName` – fall-back identity values surfaced before login succeeds (default user is `"wpf-shell"`).【F:YasGMP.Wpf/Services/UserSession.cs†L17-L41】
  - `ConnectionStrings:MySqlDb` / `MySqlDb` – MySQL connection string consumed during host bootstrapping. When absent the app falls back to the hard-coded development string defined in `App.xaml.cs`.【F:YasGMP.Wpf/App.xaml.cs†L23-L55】
- **UI automation surface:** FlaUI smoke tests require an interactive desktop. On GitHub-hosted runners (`windows-latest`) the job starts with an unlocked desktop, but self-hosted agents must enable auto-logon for the service account, disable screen savers/lock (`powercfg /change standby-timeout-ac 0` and `rundll32 user32.dll,LockWorkStation /disable` policies), and ensure the `UIAutomationCore` components are present so FlaUI can drive the WPF window.

## Building and running the shell

Run the usual .NET CLI workflow from the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project YasGMP.Wpf
```

For deployment or trimming validation you can publish a self-contained build:

```bash
dotnet publish YasGMP.Wpf -c Release -r win-x64
```

Visual Studio and MSBuild follow the same steps: open `yasgmp.sln`, set **YasGMP.Wpf** as the startup project, and use **Build → Build Solution** (or `msbuild YasGMP.Wpf/YasGMP.Wpf.csproj /t:Build`).【F:YasGMP.Wpf/YasGMP.Wpf.csproj†L1-L28】

### DatabaseService test hooks & fixture workflow

Local WPF runs can now skip the MySQL dependency by enabling the DatabaseService test hooks. The `scripts/wpf-run-with-testhooks.ps1` helper sets the opt-in flag, optionally wires fixture data, and then launches the command you provide (defaults to `dotnet run --project YasGMP.Wpf`).【F:scripts/wpf-run-with-testhooks.ps1†L1-L61】 The runtime looks for the following environment variables when the flag is present:

- `YASGMP_WPF_TESTHOOKS` — opt-in toggle automatically set to `1` by the helper.【F:YasGMP.Wpf/Runtime/DatabaseTestHookBootstrapper.cs†L20-L70】
- `YASGMP_WPF_FIXTURE_PATH` — absolute/relative path to a JSON fixture file (see `scripts/fixtures/wpf-demo.json` for the supported shape).【F:YasGMP.Wpf/Runtime/DatabaseTestHookBootstrapper.cs†L225-L273】【F:scripts/fixtures/wpf-demo.json†L1-L64】
- `YASGMP_WPF_FIXTURE_JSON` — inline JSON payload (useful for ad-hoc datasets in CI). Inline content takes precedence over `..._PATH`.【F:YasGMP.Wpf/Runtime/DatabaseTestHookBootstrapper.cs†L213-L236】

At startup `App.xaml.cs` detects the flag, assigns stub delegates to `DatabaseService.ExecuteNonQueryOverride`, `ExecuteScalarOverride`, and `ExecuteSelectOverride`, and sources canned results from the active fixture so the host never touches MySQL.【F:YasGMP.Wpf/App.xaml.cs†L54-L78】【F:YasGMP.Wpf/Runtime/DatabaseTestHookBootstrapper.cs†L24-L208】 Misses are logged at `DiagLevel.Warning` (first occurrence per SQL snippet) so you can extend the fixture iteratively.【F:YasGMP.Wpf/Runtime/DatabaseTestHookBootstrapper.cs†L80-L141】【F:YasGMP.Wpf/Runtime/DatabaseTestHookBootstrapper.cs†L296-L377】

#### Usage examples

Run the shell with the baked-in demo fixture:

```powershell
pwsh ./scripts/wpf-run-with-testhooks.ps1 -FixturePath ./scripts/fixtures/wpf-demo.json
```

Execute WPF smoke tests with an inline stub (note the escaped quotes):

```powershell
pwsh ./scripts/wpf-run-with-testhooks.ps1 -Command @("dotnet","test","YasGMP.Wpf.Smoke") -FixtureJson '{"select":[{"match":"FROM users","rows":[{"Id":1,"Username":"smoke"}]}]}'
```

To revert back to a live database simply run your command without the helper (or clear `YASGMP_WPF_TESTHOOKS`, `YASGMP_WPF_FIXTURE_PATH`, and `YASGMP_WPF_FIXTURE_JSON` from the environment). The helper restores the previous environment variables on exit, so closing the PowerShell session resets the overrides automatically.【F:scripts/wpf-run-with-testhooks.ps1†L37-L61】

## Authentication and re-authentication

- **Startup gating:** The WPF shell now displays a modal [LoginView](YasGMP.Wpf/Views/LoginView.xaml) before AvalonDock initialises. The dialog is driven by [LoginViewModel](YasGMP.Wpf/ViewModels/LoginViewModel.cs) which authenticates through [AuthenticationDialogService](YasGMP.Wpf/Services/AuthenticationDialogService.cs) and the shared [AuthService](YasGMP.AppCore/Services/AuthService.cs). Successful sign-in applies the operator to the shared [UserSession](YasGMP.Wpf/Services/UserSession.cs) and refreshes the status bar via `MainWindowViewModel.RefreshShellContext`.【F:YasGMP.Wpf/App.xaml.cs†L117-L137】【F:YasGMP.Wpf/ViewModels/LoginViewModel.cs†L13-L86】
- **Session-aware context:** `UserSession` now tracks the authenticated user, exposes a `SessionChanged` event, and feeds `WpfAuthContext` so downstream services (audit logging, signature capture, layout persistence) receive the correct user/session/device metadata.【F:YasGMP.Wpf/Services/UserSession.cs†L17-L59】【F:YasGMP.Wpf/Services/WpfAuthContext.cs†L11-L47】
- **Re-authentication prompt:** Sensitive flows can call `AuthenticationDialogService.PromptReauthentication()` to display the WPF [ReauthenticationDialog](YasGMP.Wpf/Views/Dialogs/ReauthenticationDialog.xaml). The dialog mirrors MAUI’s credential capture (username, password, optional MFA, GMP reason codes) via [ReauthenticationDialogViewModel](YasGMP.Wpf/ViewModels/Dialogs/ReauthenticationDialogViewModel.cs).【F:YasGMP.Wpf/Services/AuthenticationDialogService.cs†L11-L66】【F:YasGMP.Wpf/ViewModels/Dialogs/ReauthenticationDialogViewModel.cs†L1-L113】

## User administration & impersonation

- **Security module workflow:** The desktop [SecurityModuleViewModel](YasGMP.Wpf/ViewModels/Modules/SecurityModuleViewModel.cs) now orchestrates add/edit flows through dialog factories, exposes inspector-friendly `Create`, `Edit`, `Save`, `BeginImpersonation`, and `EndImpersonation` commands, and keeps the inspector context in sync with the selected user record so toolbar and Golden Arrow actions share a single pipeline.【F:YasGMP.Wpf/ViewModels/Modules/SecurityModuleViewModel.cs†L20-L196】
- **Modal editor parity:** [UserEditDialogWindow](YasGMP.Wpf/Dialogs/UserEditDialogWindow.xaml) hosts the full editor grid with password fields, multi-role checklists, validation output, and dedicated buttons for saving, cancelling, and toggling impersonation so the WPF shell mirrors the MAUI UX.【F:YasGMP.Wpf/Dialogs/UserEditDialogWindow.xaml†L1-L238】
- **Impersonation pipeline:** [UserEditDialogViewModel](YasGMP.Wpf/ViewModels/Dialogs/UserEditDialogViewModel.cs) composes [UserCrudServiceAdapter](YasGMP.Wpf/Services/UserCrudServiceAdapter.cs) and [SecurityImpersonationWorkflowService](YasGMP.Wpf/Services/SecurityImpersonationWorkflowService.cs) to load role/impersonation lookups, capture electronic signatures, request begin/end impersonation, and propagate status or validation messages back to the shell.【F:YasGMP.Wpf/ViewModels/Dialogs/UserEditDialogViewModel.cs†L17-L260】【F:YasGMP.Wpf/Services/UserCrudServiceAdapter.cs†L12-L122】【F:YasGMP.Wpf/Services/SecurityImpersonationWorkflowService.cs†L9-L62】

## Continuous integration

The `WPF Smoke Tests` GitHub Actions workflow (`.github/workflows/wpf-tests.yml`) provisions the .NET 9 SDK when available, falls back to .NET 8 when necessary, and executes:

1. `dotnet restore yasgmp.sln`
2. `dotnet build yasgmp.sln -c Release --no-restore`
3. `dotnet test YasGMP.Wpf.Smoke/YasGMP.Wpf.Smoke.csproj -c Release --no-build`

Smoke logs are uploaded as build artifacts (`wpf-smoke-test-results`) so failures include the FlaUI trace and TRX output. Set the `YASGMP_SMOKE` environment variable to `0` only when a diagnostic build requires skipping UI automation; the workflow enforces `1` by default to guarantee end-to-end coverage.

## Diagnostics dashboard & telemetry feed

- **Feed lifecycle:** During application startup the shell resolves a singleton [`DiagnosticsFeedService`](YasGMP.Wpf/Services/DiagnosticsFeedService.cs) and begins its background loops before showing the main window. The feed tails the rolling log file, captures telemetry snapshots from the shared `DiagnosticContext`, and periodically emits `HealthReport` payloads until shutdown, where the service is stopped and disposed gracefully.【F:YasGMP.Wpf/App.xaml.cs†L327-L381】【F:YasGMP.Wpf/Services/DiagnosticsFeedService.cs†L40-L191】【F:YasGMP.Wpf/Services/DiagnosticsFeedService.cs†L193-L277】
- **Diagnostics workspace:** [`DiagnosticsModuleViewModel`](YasGMP.Wpf/ViewModels/Modules/DiagnosticsModuleViewModel.cs) subscribes to the feed for telemetry, log, and health channels, projecting the latest payloads into observable collections that drive the dashboard cards, live log viewer, and inspector previews. Status banners and module records refresh automatically as new data arrives, while design-time fallbacks keep the view populated when the feed is unavailable (e.g., during XAML design).【F:YasGMP.Wpf/ViewModels/Modules/DiagnosticsModuleViewModel.cs†L68-L219】
- **Log tail + auto-scroll:** The live log panel buffers up to 200 lines delivered by the feed, trimming older entries while keeping the viewport anchored through the shared auto-scroll behavior. The feed reads the rolling log asynchronously using [`FileLogService.CurrentLogFilePath`](YasGMP.AppCore/Services/Logging/FileLogService.cs†L18-L78) so the viewer always points at the newest active segment.【F:YasGMP.Wpf/Services/DiagnosticsFeedService.cs†L193-L277】

### Telemetry configuration

Diagnostics settings live under the `Diagnostics` section of `appsettings.json` (or equivalent environment overrides). Use the `Diagnostics:Enabled`, `Diagnostics:Level`, and `Diagnostics:Sinks` keys to control whether the feed emits data, the verbosity (`trace` → `fatal`), and which sinks (e.g., `file`, `stdout`, `elastic`) are active. Retention tuning flows through `Diagnostics:RollingFiles:MaxMB`/`MaxDays`, and the same values can be supplied via `YAS_DIAG_*` environment variables for container deployments.【F:Diagnostics/DiagnosticsConstants.cs†L12-L55】 The feed reads from the rolling file sink managed by `FileLogService`, so ensure the service's base directory (defaulting to the platform app-data path) is writable on operator workstations.【F:YasGMP.AppCore/Services/Logging/FileLogService.cs†L18-L78】

## Key dependencies

`YasGMP.Wpf` references the shared `YasGMP.AppCore` library, which exposes the `AddYasGmpCoreServices` extension and the `YasGMP.Services` namespace consumed across the MAUI and WPF clients. The project file also pins the following packages critical to the shell experience.【F:YasGMP.Wpf/YasGMP.Wpf.csproj†L9-L28】

- **Dirkster.AvalonDock** and **Dirkster.AvalonDock.Themes.VS2013** – provide the docking layout manager and Visual Studio themed chrome for document/anchorable panes.
- **CommunityToolkit.Mvvm** – supplies `ObservableObject`, commands, and source generators used across view-models.
- **Microsoft.Extensions.Configuration.*** (`Binder`, `Json`), **DependencyInjection**, and **Hosting** – compose the generic host, configuration pipeline, and DI container used to bootstrap the shell.
- **YasGMP.AppCore DatabaseService** – shared database abstraction that exposes layout persistence extensions consumed by `DockLayoutPersistenceService`.

## Layout persistence lifecycle

Layout state flows through two services:

1. **`ShellLayoutController`** bridges AvalonDock with persistence. It captures the default layout once the window is loaded, restores any serialized layout (invoking `MainWindowViewModel.PrepareForLayoutImport` first), and reapplies saved window bounds.【F:YasGMP.Wpf/MainWindow.xaml.cs†L18-L50】【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L21-L175】
2. **`DockLayoutPersistenceService`** interacts with the shared `user_window_layouts` table through `DatabaseServiceLayoutsExtensions`. It serializes the AvalonDock XML plus window geometry per user (`IUserSession`) and layout key (`YasGmp.Wpf.Shell`). On save it upserts `layout_xml`, `pos_x`, `pos_y`, `width`, `height`, and `saved_at`; on load it returns a `LayoutSnapshot` used to hydrate the UI; `ResetAsync` now calls the shared delete helper to clear the persisted layout.【F:YasGMP.Wpf/Services/DockLayoutPersistenceService.cs†L10-L100】【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L18-L175】【F:YasGMP.Wpf/Services/UserSession.cs†L12-L22】

During startup the shell attaches the controller, initializes workspace tabs, captures the default layout, and attempts to restore any previously persisted state. Layouts are saved when the window closes or when the user invokes **Window → Save Layout**, and reset either through **Window → Reset Layout** or programmatically via `ResetLayoutAsync`, which also persists the fresh default snapshot.【F:YasGMP.Wpf/MainWindow.xaml†L16-L68】【F:YasGMP.Wpf/MainWindow.xaml.cs†L18-L50】【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L32-L112】【F:YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs†L12-L43】

## Adding new dockable documents

1. **Create the view-model:** Derive from `DocumentViewModel`, assign a `Title` and stable `ContentId`, and expose any document-specific state.【F:YasGMP.Wpf/ViewModels/DockItemViewModel.cs†L5-L33】
2. **Build the view:** Implement a corresponding WPF `UserControl` and register a `DataTemplate` in `App.xaml` so AvalonDock can resolve the view for the new view-model.【F:YasGMP.Wpf/App.xaml†L1-L16】
3. **Expose commands/navigation:** Update `MainWindowViewModel` or `WindowMenuViewModel` to open and activate the document, mirroring the existing machines commands.【F:YasGMP.Wpf/ViewModels/MainWindowViewModel.cs†L20-L126】【F:YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs†L12-L43】
4. **Support layout restore:** If the document needs a deterministic `ContentId` (e.g., for pinned tools), extend `ShellLayoutController.OnLayoutSerializationCallback` and `MainWindowViewModel.EnsureDocumentForId` so deserialization can recreate or locate the instance.【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L114-L135】【F:YasGMP.Wpf/ViewModels/MainWindowViewModel.cs†L91-L109】

Following these steps keeps layout serialization stable and ensures new documents participate in the save/restore cycle shared with the rest of the shell.

## Rollback preview & admin restore

- **Audit rollback preview:** The Audit toolbar launches `RollbackPreviewDocumentViewModel`, which renders formatted JSON before/after payloads, validates SHA-256 signature manifests, and pushes localized status updates through the shell while issuing rollback requests via `DatabaseService.RollbackEntityAsync`.【F:YasGMP.Wpf/ViewModels/Modules/RollbackPreviewDocumentViewModel.cs†L30-L238】【F:YasGMP.AppCore/Services/DatabaseService.Rollback.Extensions.cs†L14-L21】 The document is registered in `App.xaml` so AvalonDock resolves `RollbackPreviewDocument` automatically when the module instantiates the view-model.【F:YasGMP.Wpf/App.xaml†L69-L76】
- **Admin restore workflow:** `AdminModuleViewModel` exposes `RestoreSettingCommand`, prompting operators for confirmation and an electronic signature before calling `DatabaseService.RollbackSettingByKeyAsync`, persisting the captured signature, refreshing the grid, and surfacing success/failure/cancellation messaging through the alert service and status bar.【F:YasGMP.Wpf/ViewModels/Modules/AdminModuleViewModel.cs†L314-L432】【F:YasGMP.AppCore/Services/DatabaseService.Settings.Extensions.cs†L210-L241】 The view binds the command to a localized button alongside a signature status indicator so QA can verify rollback provenance at a glance.【F:YasGMP.Wpf/Views/AdminModuleView.xaml†L48-L86】【F:YasGMP.Wpf/Resources/ShellStrings.resx†L777-L807】

## Alerts & notification surfaces

The shell now projects MAUI-style alerts through a dedicated `IShellAlertService`, allowing shared view-models to raise status messages without knowing about WPF plumbing. The concrete [`AlertService`](YasGMP.Wpf/Services/AlertService.cs) fans messages out to the status bar via `IShellInteractionService`, maintains a bounded toast collection, and honours operator preferences so each surface can be toggled independently.【F:YasGMP.Wpf/Services/AlertService.cs†L13-L155】 The root [`MainWindowViewModel`](YasGMP.Wpf/ViewModels/MainWindowViewModel.cs) exposes the read-only toast collection to XAML, and [`MainWindow.xaml`](YasGMP.Wpf/MainWindow.xaml) renders a Fluent-themed overlay in the upper-right corner while binding severity-specific colours and dismiss commands from [`ToastNotificationViewModel`](YasGMP.Wpf/ViewModels/ToastNotificationViewModel.cs).【F:YasGMP.Wpf/ViewModels/MainWindowViewModel.cs†L24-L67】【F:YasGMP.Wpf/MainWindow.xaml†L328-L360】【F:YasGMP.Wpf/ViewModels/ToastNotificationViewModel.cs†L9-L66】

Operator preferences are persisted through [`NotificationPreferenceService`](YasGMP.Wpf/Services/NotificationPreferenceService.cs), which stores boolean flags in the shared settings catalog and emits `PreferencesChanged` events consumed by the alert pipeline.【F:YasGMP.Wpf/Services/NotificationPreferenceService.cs†L10-L120】 The [`NotificationPreferences`](YasGMP.AppCore/Models/NotificationPreferences.cs) model lives in AppCore so the MAUI client can share the same contract.【F:YasGMP.AppCore/Models/NotificationPreferences.cs†L1-L23】 The Admin module now exposes status bar and toast toggles plus a save action wired to those services; the view-model publishes success/error states through the alert service so operators receive parity between MAUI popups and the WPF status/ toast surfaces.【F:YasGMP.Wpf/ViewModels/Modules/AdminModuleViewModel.cs†L25-L209】【F:YasGMP.Wpf/Views/AdminModuleView.xaml†L48-L75】

## Quality & Compliance modules

- **Document Control:** [`DocumentControlModuleView`](YasGMP.Wpf/Views/DocumentControlModuleView.xaml) hosts the desktop document workspace driven by [`DocumentControlModuleViewModel`](YasGMP.Wpf/ViewModels/Modules/DocumentControlModuleViewModel.cs). The module surfaces initiate/revise/approve/publish/expire commands, routes saves through [`DocumentControlServiceAdapter`](YasGMP.Wpf/Services/DocumentControlServiceAdapter.cs) to persist attachments with retention metadata, prompts for electronic signatures via the shared dialog pipeline, and links change controls using [`DocumentControlViewModel`](YasGMP.ViewModels/DocumentControlViewModel.cs) so inspectors, status messaging, and manifest hashes match the MAUI workflow.【F:YasGMP.Wpf/Views/DocumentControlModuleView.xaml†L1-L103】【F:YasGMP.Wpf/ViewModels/Modules/DocumentControlModuleViewModel.cs†L30-L348】【F:YasGMP.Wpf/Services/DocumentControlServiceAdapter.cs†L19-L377】【F:ViewModels/DocumentControlViewModel.cs†L26-L907】
- **Training Records:** [`TrainingRecordsModuleView`](YasGMP.Wpf/Views/TrainingRecordsModuleView.xaml) opens from the Quality ribbon/module tree via [`OpenTrainingRecordsCommand`](YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs) and wraps the shared [`TrainingRecordsModuleViewModel`](YasGMP.Wpf/ViewModels/Modules/TrainingRecordsModuleViewModel.cs). The document surfaces Golden Arrow/CFL lookups plus initiate/assign/approve/complete/close/export toolbar actions, mirrors MAUI status/type filters, and keeps the inspector editor synchronized with [`TrainingRecordViewModel`](ViewModels/TrainingRecordViewModel.cs) for SAP B1 form-mode transitions.【F:YasGMP.Wpf/Views/TrainingRecordsModuleView.xaml†L19-L221】【F:YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs†L27-L74】【F:YasGMP.Wpf/ViewModels/Modules/TrainingRecordsModuleViewModel.cs†L24-L149】
- **SOP Governance:** [`SopGovernanceModuleView`](YasGMP.Wpf/Views/SopGovernanceModuleView.xaml) is reachable from the same Quality surfaces through [`OpenSopGovernanceCommand`](YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs) and projects the shared [`SopGovernanceModuleViewModel`](YasGMP.Wpf/ViewModels/Modules/SopGovernanceModuleViewModel.cs). Operators work through search/status/process/date filters, active-only toggle, and create/update/delete toolbar actions while the inspector stays bound to [`SopViewModel`](ViewModels/SopViewModel.cs) for persistence, busy messaging, and Golden Arrow/CFL links to related change controls.【F:YasGMP.Wpf/Views/SopGovernanceModuleView.xaml†L19-L140】【F:YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs†L27-L76】【F:YasGMP.Wpf/ViewModels/Modules/SopGovernanceModuleViewModel.cs†L24-L139】
