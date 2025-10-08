# YasGMP

YasGMP is a cross-platform MAUI application for GMP-compliant manufacturing operations. This repository
contains the mobile/desktop client, common domain services, and diagnostics tooling used during validation.

## Maintainer resources

- [Change Control Assignment QA Checklist](QA/ChangeControlAssignment.md) – manual flows and harness guidance for
  verifying change-control assignments and confirming `system_event_log` auditing.
- Diagnostics self-tests: launch **Debug → Diagnostics Hub → Run Self Tests** to execute the built-in harnesses
  (including the change-control assignment exercise) before shipping.

## Status documentation workflow

The status artifacts under `docs/` (`STATUS.md`, `EXECUTION_LOG.md`, and `tasks.yaml`) are generated from a single
source of truth. To regenerate them locally install PyYAML once and run the helper script:

```bash
python -m pip install pyyaml  # first-time setup
python scripts/update_status_docs.py
```

The script is idempotent. To verify that the checked-in files are up to date (e.g., before pushing) run:

```bash
python scripts/update_status_docs.py --check
```

To record a new execution session without hand-editing Markdown, supply `--append-log` along with the session
metadata and table rows. Example:

```bash
python scripts/update_status_docs.py \
  --append-log \
  --log-date 2024-05-03 \
  --log-author "Luka Marin" \
  --log-summary "Closed reporting gaps" \
  --log-entry "08:30|Rebuilt analytics snapshot|Green diff" \
  --log-entry "10:15|Published release notes|Shared with QA"
```

## Building the app

Install the required .NET workload (see `global.json`) and run:

```bash
dotnet restore
dotnet build
```

Refer to platform-specific documentation for signing, deployment, and device setup.

### Test matrix

All contributors should execute the full regression suite before opening a pull request:

```bash
dotnet test YasGMP.Wpf.Smoke/YasGMP.Wpf.Smoke.csproj -c Release --logger trx
```

The smoke harness launches the WPF shell, drives the FlaUI automation flows, and emits a `.trx` log that can be attached to PRs
and inspected in CI artifacts. Set `YASGMP_SMOKE=0` only when diagnosing local infrastructure issues that prevent UI automation
from launching.

## WPF desktop shell

`YasGMP.Wpf` targets `net9.0-windows10.0.19041.0` (see `YasGMP.Wpf/YasGMP.Wpf.csproj`) and serves as the Windows-only desktop shell. Its `App.xaml.cs` bootstraps the same generic host, configuration, and DI container as the MAUI client, reusing the shared services that now live in the `YasGMP.AppCore` library. Refer to [README_WPF_SHELL.md](README_WPF_SHELL.md) for detailed setup, layout management, and docking workflows. Shared services together with the dock layout persistence service (`YasGMP.Wpf/Services/DockLayoutPersistenceService.cs`) ensure that saved layouts travel between the WPF shell and the MAUI app so both environments stay aligned.
