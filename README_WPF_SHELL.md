# YasGMP WPF shell

The YasGMP WPF shell hosts the desktop docking workspace used to surface cockpit modules on Windows. This guide documents the environment requirements, build steps, and extension points that differ from the cross-platform MAUI client.

## Prerequisites

- **Supported OS:** The WPF project targets `net9.0-windows10.0.19041.0`, so the shell requires Windows 10 build 19041 or later.
- **.NET SDK:** Install the .NET SDK pinned in `global.json` (`9.0.100`) to match the repo’s toolset.【F:global.json†L1-L6】
- **Configuration:** The shell reads configuration from `appsettings.json`. Override the defaults for per-user layouts and database access via:
  - `Shell:UserId` – numeric user identifier (defaults to `1`).【F:YasGMP.Wpf/Services/UserSession.cs†L12-L22】
  - `Shell:Username` – display name used when persisting layouts (defaults to `"wpf-shell"`).【F:YasGMP.Wpf/Services/UserSession.cs†L12-L22】
  - `ConnectionStrings:MySqlDb` / `MySqlDb` – MySQL connection string consumed during host bootstrapping. When absent the app falls back to the hard-coded development string defined in `App.xaml.cs`.【F:YasGMP.Wpf/App.xaml.cs†L23-L55】

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

## Key dependencies

`YasGMP.Wpf` references the shared `YasGMP.AppCore` library, which exposes the `AddYasGmpCoreServices` extension and the `YasGMP.Services` namespace consumed across the MAUI and WPF clients. The project file also pins the following packages critical to the shell experience.【F:YasGMP.Wpf/YasGMP.Wpf.csproj†L9-L28】

- **Dirkster.AvalonDock** and **Dirkster.AvalonDock.Themes.VS2013** – provide the docking layout manager and Visual Studio themed chrome for document/anchorable panes.
- **CommunityToolkit.Mvvm** – supplies `ObservableObject`, commands, and source generators used across view-models.
- **Microsoft.Extensions.Configuration.*** (`Binder`, `Json`), **DependencyInjection**, and **Hosting** – compose the generic host, configuration pipeline, and DI container used to bootstrap the shell.
- **MySqlConnector** – lightweight MySQL client used by `DockLayoutPersistenceService` to load and save layouts.

## Layout persistence lifecycle

Layout state flows through two services:

1. **`ShellLayoutController`** bridges AvalonDock with persistence. It captures the default layout once the window is loaded, restores any serialized layout (invoking `MainWindowViewModel.PrepareForLayoutImport` first), and reapplies saved window bounds.【F:YasGMP.Wpf/MainWindow.xaml.cs†L18-L50】【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L21-L175】
2. **`DockLayoutPersistenceService`** interacts with the shared `user_window_layouts` table. It serializes the AvalonDock XML plus window geometry per user (`IUserSession`) and layout key (`YasGmp.Wpf.Shell`). On save it upserts `layout_xml`, `pos_x`, `pos_y`, `width`, `height`, and `saved_at`; on load it returns a `LayoutSnapshot` used to hydrate the UI.【F:YasGMP.Wpf/Services/DockLayoutPersistenceService.cs†L10-L100】【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L18-L175】【F:YasGMP.Wpf/Services/UserSession.cs†L12-L22】

During startup the shell attaches the controller, initializes workspace tabs, captures the default layout, and attempts to restore any previously persisted state. Layouts are saved when the window closes or when the user invokes **Window → Save Layout**, and reset either through **Window → Reset Layout** or programmatically via `ResetLayoutAsync`, which also persists the fresh default snapshot.【F:YasGMP.Wpf/MainWindow.xaml†L16-L68】【F:YasGMP.Wpf/MainWindow.xaml.cs†L18-L50】【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L32-L112】【F:YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs†L12-L43】

## Adding new dockable documents

1. **Create the view-model:** Derive from `DocumentViewModel`, assign a `Title` and stable `ContentId`, and expose any document-specific state.【F:YasGMP.Wpf/ViewModels/DockItemViewModel.cs†L5-L33】
2. **Build the view:** Implement a corresponding WPF `UserControl` and register a `DataTemplate` in `App.xaml` so AvalonDock can resolve the view for the new view-model.【F:YasGMP.Wpf/App.xaml†L1-L16】
3. **Expose commands/navigation:** Update `MainWindowViewModel` or `WindowMenuViewModel` to open and activate the document, mirroring the existing machines commands.【F:YasGMP.Wpf/ViewModels/MainWindowViewModel.cs†L20-L126】【F:YasGMP.Wpf/ViewModels/WindowMenuViewModel.cs†L12-L43】
4. **Support layout restore:** If the document needs a deterministic `ContentId` (e.g., for pinned tools), extend `ShellLayoutController.OnLayoutSerializationCallback` and `MainWindowViewModel.EnsureDocumentForId` so deserialization can recreate or locate the instance.【F:YasGMP.Wpf/Services/ShellLayoutController.cs†L114-L135】【F:YasGMP.Wpf/ViewModels/MainWindowViewModel.cs†L91-L109】

Following these steps keeps layout serialization stable and ensures new documents participate in the save/restore cycle shared with the rest of the shell.


## Acceptance Checklist

- WPF project builds and runs (Ribbon + AvalonDock + layout persistence)
- MAUI builds/runs unaffected (Windows target)
- ModulesPane lists all modules and opens editors
- ModuleRegistry uses EN↔HR resources for titles/categories/descriptions
- Ribbon/Backstage/Home groups/buttons bind to resources; tooltips localized
- Views expose AutomationProperties.Name and ToolTips for a11y/smoke hooks
- B1 FormModes & command enablement wired across editors
- CFL pickers + Golden Arrow navigation operational
- Attachments DB-backed with upload/preview/download (hash, retention)
- E-signature prompts on regulated saves; audit surfaced in Audit views
- Smoke harness toggled via YASGMP_SMOKE and logs to %LOCALAPPDATA%/YasGMP/logs


## AI Assistant (ChatGPT)

- Configure `OPENAI_API_KEY` in your user environment, or set `Ai:OpenAI:ApiKey` in `appsettings.json`.
- Optional: `Ai:OpenAI:BaseUrl` for Azure/OpenAI-compatible gateways; `Ai:OpenAI:Model` defaults to `gpt-4o-mini`.
- Launch the WPF shell and use Tools → "Ask AI Assistant" to open the chat dialog, or open the docked "AI Assistant" module from the Modules pane for summaries.
- In Audit and Work Orders modules, click "Summarize (AI)" to get one‑click summaries.
- In Suppliers and Incidents modules, use "Summarize (AI)" to summarize the selected record.
- In CAPA and External Servicers modules, use "Summarize (AI)" for fast overviews.
 - In Assets, Components, Parts, Warehouse, Security (Users/Roles), Scheduling, Dashboard, and Administration modules, "Summarize (AI)" is available.
- The AI service lives in `YasGMP.AppCore` (`IAiAssistantService`), so MAUI can also use it via DI.

### Optional RAG (Embeddings)
- The app can store embeddings for attachments in `attachment_embeddings` via `AttachmentEmbeddingService`.
- In the Attachments module, use "Index AI Embedding" to index the selected file’s metadata.
- Use "Find Similar (AI)" to suggest attachments with similar meaning based on embeddings.
  A small panel in the right details view lists top matches and scores.
  Clicking "Open" on a similar item selects it and, when applicable, jumps to the related module (e.g., Work Order or Calibration) via Golden Arrow navigation.
