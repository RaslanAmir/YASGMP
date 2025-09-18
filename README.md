# YasGMP

YasGMP is a cross-platform MAUI application for GMP-compliant manufacturing operations. This repository
contains the mobile/desktop client, common domain services, and diagnostics tooling used during validation.

## Maintainer resources

- [Change Control Assignment QA Checklist](QA/ChangeControlAssignment.md) – manual flows and harness guidance for
  verifying change-control assignments and confirming `system_event_log` auditing.
- Diagnostics self-tests: launch **Debug → Diagnostics Hub → Run Self Tests** to execute the built-in harnesses
  (including the change-control assignment exercise) before shipping.

## Building the app

Install the required .NET workload (see `global.json`) and run:

```bash
dotnet restore
dotnet build
```

Refer to platform-specific documentation for signing, deployment, and device setup.
