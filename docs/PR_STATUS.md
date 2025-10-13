# PR Status — WPF Shell (.NET 9) + Full YasGMP Integration (B1-style Docking/Ribbon)

Updated: 2025-10-10

- E2E smoke scaffold wired (skip-by-default; respects `YASGMP_SMOKE`; CI-guarded) and logs when environment allows.
  - Enable with: `RUN_WPF_SMOKE=1` and `YASGMP_SMOKE=1`.
  - Builds required: `YasGMP.Wpf` Release before running the smoke test project.
  - Localized button/tab names handled (EN/HR) and basic module tree navigation + editor open is exercised.

- Extended i18n/a11y across core editors: Incidents, CAPA, Validations, Scheduling, Security, Suppliers.
  - Replaced hard-coded XAML labels with DynamicResource keys.
  - Added missing resource keys for details titles and Scheduling fields (Job Type, Cron Expression, Entity Type/Id, Recurrence, etc.).

- AppCore EF test fix: pending; can filter in CI if desired.
  - Option A (CI filter): `dotnet test yasgmp.sln -c Release --filter FullyQualifiedName!~ValidationServiceTests`
  - Option B (props switch): introduce a conditional in `Directory.Build.props` to exclude selected tests when `$(CI)` is true.

- WPF/MAUI builds green (net9.0-windows).
  - Both `YasGMP.Wpf` and `yasgmp` (MAUI Windows) build in Release; WPF smoke is excluded from MAUI compile to avoid duplicate assembly attributes.

