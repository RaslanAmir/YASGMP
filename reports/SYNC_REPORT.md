# OBS-005 Sync Report

- Added DatabaseService command timeouts via CommandTimeoutSeconds (default 15s).
- Replaced SELECT * with explicit column lists in:
  - Views/CalibrationsPage.xaml.cs
  - ViewModels/CalibrationsViewModel.cs
  - Services/DatabaseService.Assets.Extensions.cs
  - Services/DatabaseService.Attachments.Extensions.cs
  - Services/DatabaseService.Calibrations.Extensions.cs
  - Services/DatabaseService.Components.QueryExtensions.cs
  - Services/DatabaseService.ContractorInterventions.Extensions.cs
  - Services/DatabaseService.DeviationAudit.Extensions.cs
  - Services/DatabaseService.Deviations.Extensions.cs
  - Services/DatabaseService.DigitalSignatures.Extensions.cs
  - Services/DatabaseService.Documents.Extensions.cs
  - Services/DatabaseService.IncidentAudits.Extensions.cs
  - Services/DatabaseService.Incidents.Extensions.cs
- Ensured parameterized queries maintained throughout; added aliases to handle schema mismatches:
  - machine_components.io_tdevice_id AS iot_device_id.
- Added cancellation tokens/timeouts in high-latency UI paths:
  - CalibrationsPage and CalibrationsViewModel use 5s CTS around DB calls.
- Preserved DatabaseService regions and telemetry integration.

Artifacts:
- Patch file: patches/code/.patch (git-apply ready)
- Progress log: eports/PROGRESS.log

