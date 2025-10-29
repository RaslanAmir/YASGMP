# Project Overview

YasGMP centralizes GMP-compliant manufacturing maintenance, calibration, and quality workflows across the MAUI client, shared services, and diagnostics tooling.

# Current Status

- Merge the change control assignment UI, workflow, and QA checklist pull requests (PRs #26, #28, #29).
- Verify the calibration self-referencing EF mappings provided in PR #53 before release.

# Blockers

- The Linux container still lacks the .NET SDK/Windows 10 SDK workloads, so
  `dotnet restore`, Windows builds, and smoke tests cannot run locally.

# Next Steps

- Exercise the new change control diagnostics harness once the open PRs land.
- Run an end-to-end build after the EF mapping fixes merge on a Windows host
  with the .NET 9 workload installed.
