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

## Authentication and re-authentication

- **Startup gating:** The WPF shell now displays a modal [LoginView](YasGMP.Wpf/Views/LoginView.xaml) before AvalonDock initialises. The dialog is driven by [LoginViewModel](YasGMP.Wpf/ViewModels/LoginViewModel.cs) which authenticates through [AuthenticationDialogService](YasGMP.Wpf/Services/AuthenticationDialogService.cs) and the shared [AuthService](YasGMP.AppCore/Services/AuthService.cs). Successful sign-in applies the operator to the shared [UserSession](YasGMP.Wpf/Services/UserSession.cs) and refreshes the status bar via `MainWindowViewModel.RefreshShellContext`.【F:YasGMP.Wpf/App.xaml.cs†L117-L137】【F:YasGMP.Wpf/ViewModels/LoginViewModel.cs†L13-L86】
- **Session-aware context:** `UserSession` now tracks the authenticated user, exposes a `SessionChanged` event, and feeds `WpfAuthContext` so downstream services (audit logging, signature capture, layout persistence) receive the correct user/session/device metadata.【F:YasGMP.Wpf/Services/UserSession.cs†L17-L59】【F:YasGMP.Wpf/Services/WpfAuthContext.cs†L11-L47】
- **Re-authentication prompt:** Sensitive flows can call `AuthenticationDialogService.PromptReauthentication()` to display the WPF [ReauthenticationDialog](YasGMP.Wpf/Views/Dialogs/ReauthenticationDialog.xaml). The dialog mirrors MAUI’s credential capture (username, password, optional MFA, GMP reason codes) via [ReauthenticationDialogViewModel](YasGMP.Wpf/ViewModels/Dialogs/ReauthenticationDialogViewModel.cs).【F:YasGMP.Wpf/Services/AuthenticationDialogService.cs†L11-L66】【F:YasGMP.Wpf/ViewModels/Dialogs/ReauthenticationDialogViewModel.cs†L1-L113】

## Continuous integration

The `WPF Smoke Tests` GitHub Actions workflow (`.github/workflows/wpf-tests.yml`) provisions the .NET 9 SDK when available, falls back to .NET 8 when necessary, and executes:

1. `dotnet restore yasgmp.sln`
2. `dotnet build yasgmp.sln -c Release --no-restore`
3. `dotnet test YasGMP.Wpf.Smoke/YasGMP.Wpf.Smoke.csproj -c Release --no-build`

Smoke logs are uploaded as build artifacts (`wpf-smoke-test-results`) so failures include the FlaUI trace and TRX output. Set the `YASGMP_SMOKE` environment variable to `0` only when a diagnostic build requires skipping UI automation; the workflow enforces `1` by default to guarantee end-to-end coverage.

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
