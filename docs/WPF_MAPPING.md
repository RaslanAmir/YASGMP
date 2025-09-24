# MAUI ↔ WPF Parity Map

This document inventories the YasGMP MAUI experience and tracks the corresponding WPF shell implementations. Each table groups functionality by module so future desktop parity work has an authoritative in-repo checklist.

## Legend
- **MAUI Page/View-Model/Service**: Hyperlinks into the existing MAUI implementation.
- **WPF Document**: Dockable tab (AvalonDock `DocumentContent`) implemented in the WPF shell.
- **WPF Pane**: Anchorable tool window hosted by the WPF shell.
- **WPF Adapter**: Service or adapter class in the WPF project that bridges shell infrastructure or domain data.
- **Notes / TODO**: Status call-outs. Items prefixed with `**TODO:**` highlight missing WPF counterparts to build.

## Home & Shell

### Pages & View-Models
| Feature | MAUI Page(s) | MAUI View-Model(s) | WPF Document | WPF Pane | Notes / TODO |
| --- | --- | --- | --- | --- | --- |
| Dashboard KPIs | [DashboardPage](../Views/DashboardPage.xaml) | [DashboardViewModel](../ViewModels/DashboardViewModel.cs) | **TODO:** None yet | [CockpitView](../YasGMP.Wpf/Views/CockpitView.xaml) | Align cockpit metrics with MAUI dashboard surface.
| Launchpad navigation | [MainPage](../Views/MainPage.xaml) | [MainPageViewModel](../ViewModels/MainPageViewModel.cs) | **TODO:** None yet | [ModuleTreeView](../YasGMP.Wpf/Views/ModuleTreeView.xaml) | WPF module tree should mirror Shell routes and favorites.
| Authentication | [LoginPage](../Views/LoginPage.xaml) | [LoginViewModel](../ViewModels/LoginViewModel.cs) | **TODO:** None yet | — | **TODO:** Build WPF login pane/dialog and wire authentication flow.

### Services
| MAUI Service(s) | Responsibility | WPF Adapter | Notes / TODO |
| --- | --- | --- | --- |
| [SafeNavigator](../Services/SafeNavigator.cs)<br>[WindowManagerService](../Services/WindowManagerService.cs) | Shell-level windowing, safe UI thread navigation, persisted geometry. | [ShellLayoutController](../YasGMP.Wpf/Services/ShellLayoutController.cs)<br>[DockLayoutPersistenceService](../YasGMP.Wpf/Services/DockLayoutPersistenceService.cs)<br>[UserSession](../YasGMP.Wpf/Services/UserSession.cs) | Map MAUI window persistence semantics onto AvalonDock layout save/restore.
| [SignalRService](../Services/SignalRService.cs)<br>[BackgroundScheduler](../Services/BackgroundScheduler.cs)<br>[UiInstrumentationService](../Services/Diagnostics/UiInstrumentationService.cs) | Real-time notifications, background jobs, UI instrumentation. | **TODO:** None yet | **TODO:** Introduce WPF background worker/SignalR adapter to keep cockpit metrics live.
| [AlertService](../Services/Ui/AlertService.cs)<br>[SafeNavigator](../Services/SafeNavigator.cs) | Common UI alerts and safe invocation helpers. | **TODO:** None yet | **TODO:** Add WPF-friendly alert/notification abstraction.
| [CodeGeneratorService](../Services/CodeGeneratorService.cs)<br>[QRCodeService](../Services/QRCodeService.cs)<br>[SystemEvent.Poco](../Services/SystemEvent.Poco.cs) | Shared utilities for codes, QR payloads, and audit POCOs. | **TODO:** None yet | **TODO:** Surface equivalent helpers or adapters in WPF as features land.
| [IPlatformService](../Services/IPlatformService.cs) | Abstract platform operations consumed by MAUI pages. | **TODO:** None yet | **TODO:** Provide WPF platform adapter implementation.

## Operations

### Pages & View-Models
| Feature | MAUI Page(s) | MAUI View-Model(s) | WPF Document | WPF Pane | Notes / TODO |
| --- | --- | --- | --- | --- | --- |
| Work order management | [WorkOrdersPage](../Views/WorkOrdersPage.xaml)<br>[WorkOrderEditDialog](../Views/WorkOrderEditDialog.xaml)<br>[WorkOrderEditDialog](../Views/Dialogs/WorkOrderEditDialog.xaml) | [WorkOrderViewModel](../ViewModels/WorkOrderViewModel.cs)<br>[WorkOrderEditDialogViewModel](../ViewModels/WorkOrderEditDialogViewModel.cs) | **TODO:** None yet | — | **TODO:** Create dockable WPF work order document with edit dialog integration.
| Machines & assets | [MachinesPage](../Views/MachinesPage.xaml)<br>[MachineEditDialog](../Views/Dialogs/MachineEditDialog.xaml)<br>[MachineDocumentsDialog](../Views/Dialogs/MachineDocumentsDialog.xaml)<br>[MachineComponentsDialog](../Views/Dialogs/MachineComponentsDialog.xaml) | [MachineViewModel](../ViewModels/MachineViewModel.cs)<br>[AssetViewModel](../ViewModels/AssetViewModel.cs) | [MachinesDocumentView](../YasGMP.Wpf/Views/MachinesDocumentView.xaml) | — | Machines document already implemented; expand to honor full MAUI CRUD surface.
| Calibrations | [CalibrationsPage](../Views/CalibrationsPage.xaml)<br>[CalibrationEditDialog](../Views/Dialogs/CalibrationEditDialog.xaml) | [CalibrationsViewModel](../ViewModels/CalibrationsViewModel.cs)<br>[CalibrationEditDialogViewModel](../ViewModels/CalibrationEditDialogViewModel.cs) | **TODO:** None yet | — | **TODO:** Add calibration WPF document and dialog equivalents.
| Preventive maintenance | [PpmPage](../Views/PpmPage.xaml)<br>[PpmAddEditDialog](../Views/Dialogs/PpmAddEditDialog.xaml) | [PpmViewModel](../ViewModels/PpmViewModel.cs)<br>[SchedulerViewModel](../ViewModels/SchedulerViewModel.cs) | **TODO:** None yet | — | **TODO:** Build WPF PPM workspace and scheduling pane.
| Components lifecycle | [ComponentsPage](../Views/ComponentsPage.xaml)<br>[ComponentEditDialog](../Views/Dialogs/ComponentEditDialog.xaml)<br>[ComponentDocumentsDialog](../Views/Dialogs/ComponentDocumentsDialog.xaml) | [ComponentViewModel](../ViewModels/ComponentViewModel.cs) | **TODO:** None yet | — | **TODO:** Provide WPF component management document with document links.
| Parts & spare stock | [PartsPage](../Views/PartsPage.xaml)<br>[PartDetailDialog](../Views/Dialogs/PartDetailDialog.xaml) | [PartViewModel](../ViewModels/PartViewModel.cs)<br>[SparePartViewModel](../ViewModels/SparePartViewModel.cs)<br>[PartsStockViewModel](../ViewModels/PartsStockViewModel.cs) | **TODO:** None yet | — | **TODO:** Implement WPF parts/stock document with low-inventory alerts.
| Warehouse & inventory | [WarehousePage](../Views/WarehousePage.xaml)<br>[StockChangeDialog](../Views/Dialogs/StockChangeDialog.xaml) | [WarehouseViewModel](../ViewModels/WarehouseViewModel.cs) | **TODO:** None yet | — | **TODO:** Add WPF warehouse document for transactions and stock adjustments.
| Suppliers | [SuppliersPage](../Views/SuppliersPage.xaml) | [SupplierViewModel](../ViewModels/SupplierViewModel.cs) | **TODO:** None yet | — | **TODO:** Create WPF suppliers document with audit integration.
| External servicers | [ExternalServicersPage](../Views/ExternalServicersPage.xaml) | [SupplierContractorViewModel](../ViewModels/SupplierContractorViewModel.cs)<br>[ExternalContractorViewModel](../ViewModels/ExternalContractorViewModel.cs)<br>[ContractorInterventionViewModel](../ViewModels/ContractorInterventionViewModel.cs) | **TODO:** None yet | — | **TODO:** Surface external servicer cockpit in WPF.
| Rollback preview (ops) | [RollbackPreviewPage](../Views/RollbackPreviewPage.xaml) | [RollbackPreviewViewModel](../ViewModels/RollbackPreviewViewModel.cs) | **TODO:** None yet | — | **TODO:** Port rollback diff viewer to WPF docking workspace.

### Services
| MAUI Service(s) | Responsibility | WPF Adapter | Notes / TODO |
| --- | --- | --- | --- |
| [WorkOrderService](../Services/WorkOrderService.cs)<br>[WorkOrderAuditService](../Services/WorkOrderAuditService.cs)<br>[IWorkOrderAuditService](../Services/Interfaces/IWorkOrderAuditService.cs)<br>[DatabaseService.WorkOrders.Extensions](../Services/DatabaseService.WorkOrders.Extensions.cs) | Work order CRUD, audits, exports. | **TODO:** None yet | **TODO:** Author WPF data adapter to feed work order grids.
| [MachineService](../Services/MachineService.cs)<br>[DatabaseService.MachineExtensions](../Services/DatabaseService.MachineExtensions.cs)<br>[DatabaseService.MachineLookups.Extensions](../Services/DatabaseService.MachineLookups.Extensions.cs)<br>[DatabaseService.Machines.CoreExtensions](../Services/DatabaseService.Machines.CoreExtensions.cs) | Machine lifecycle APIs and lookups. | [IMachineDataService](../YasGMP.Wpf/Services/IMachineDataService.cs) | Expand WPF data adapter to call real database-backed machine service.
| [AssetService coverage](../Services/DatabaseService.Assets.Extensions.cs) | Asset-specific helpers supporting [AssetViewModel](../ViewModels/AssetViewModel.cs). | **TODO:** None yet | **TODO:** Build asset-focused WPF document once machines mature.
| [CalibrationService](../Services/CalibrationService.cs)<br>[ICalibrationService](../Services/Interfaces/ICalibrationService.cs)<br>[ICalibrationAuditService](../Services/Interfaces/ICalibrationAuditService.cs)<br>[DatabaseService.Calibrations.Extensions](../Services/DatabaseService.Calibrations.Extensions.cs) | Calibration scheduling, certificates, audit hooks. | **TODO:** None yet | **TODO:** Wire calibration adapter/service for WPF PPM module.
| [PreventiveMaintenanceService](../Services/PreventiveMaintenanceService.cs)<br>[PreventiveMaintenancePlanService](../Services/PreventiveMaintenancePlanService.cs)<br>[DatabaseService.Ppm.Extensions](../Services/DatabaseService.Ppm.Extensions.cs)<br>[IPpmAuditService](../Services/Interfaces/IPpmAuditService.cs)<br>[DatabaseService.Scheduler.Extensions](../Services/DatabaseService.Scheduler.Extensions.cs) | Preventive plan orchestration, schedule utilities, auditing. | **TODO:** None yet | **TODO:** Create WPF scheduler pane and persistence.
| [ComponentService](../Services/ComponentService.cs)<br>[DatabaseService.ComponentExtensions](../Services/DatabaseService.ComponentExtensions.cs)<br>[DatabaseService.ComponentOverloads](../Services/DatabaseService.ComponentOverloads.cs)<br>[DatabaseService.Components.QueryExtensions](../Services/DatabaseService.Components.QueryExtensions.cs) | Component master data, versioning, query helpers. | **TODO:** None yet | **TODO:** Provide WPF component data adapters and documents.
| [PartService](../Services/PartService.cs)<br>[DatabaseService.SpareParts.Extensions](../Services/DatabaseService.SpareParts.Extensions.cs)<br>[DatabaseService.Inventory.Extensions](../Services/DatabaseService.Inventory.Extensions.cs)<br>[DatabaseService.Transactions](../Services/DatabaseService.Transactions.cs) | Parts inventory, spare stock, inventory transactions. | **TODO:** None yet | **TODO:** Feed WPF inventory panes with transactional data.
| [Warehouse support](../Services/DatabaseService.Warehouse.Extensions.cs)<br>[DatabaseService.Settings.Extensions](../Services/DatabaseService.Settings.Extensions.cs) | Warehouse zones, settings helpers for inventory flows. | **TODO:** None yet | **TODO:** Introduce WPF warehouse configuration panes.
| [SupplierService](../Services/SupplierService.cs)<br>[SupplierAuditService](../Services/SupplierAuditService.cs)<br>[ISupplierAuditService](../Services/Interfaces/ISupplierAuditService.cs)<br>[DatabaseService.Suppliers.Extensions](../Services/DatabaseService.Suppliers.Extensions.cs) | Supplier onboarding, audits, metrics. | **TODO:** None yet | **TODO:** Deliver WPF supplier workspace.
| [ExternalServicerService](../Services/ExternalServicerService.cs)<br>[DatabaseService.ExternalServicers.Extensions](../Services/DatabaseService.ExternalServicers.Extensions.cs) | External contractor registry and mapping. | **TODO:** None yet | **TODO:** Add WPF pane for service provider oversight.
| [Code & QR helpers](../Services/CodeGeneratorService.cs)<br>[QRCodeService](../Services/QRCodeService.cs) | Machine/asset codes, QR payload generation. | **TODO:** None yet | **TODO:** Expose utilities through WPF dialogs as features land.
| [DatabaseService.ContractorInterventionExtensions](../Services/DatabaseService.ContractorInterventionExtensions.cs)<br>[DatabaseService.ContractorInterventions.Extensions](../Services/DatabaseService.ContractorInterventions.Extensions.cs) | Intervention tracking for contractors. | **TODO:** None yet | **TODO:** Model contractor dashboards in WPF.
| [DatabaseService.Inventory.Extensions](../Services/DatabaseService.Inventory.Extensions.cs)<br>[DatabaseService.SystemEvents.QueryExtensions](../Services/DatabaseService.SystemEvents.QueryExtensions.cs) | Inventory queries and audit join helpers. | **TODO:** None yet | **TODO:** Reuse for WPF reporting grids.

## Quality & Compliance

### Pages & View-Models
| Feature | MAUI Page(s) | MAUI View-Model(s) | WPF Document | WPF Pane | Notes / TODO |
| --- | --- | --- | --- | --- | --- |
| Audit dashboards & log | [AuditDashboardPage](../Views/AuditDashboardPage.xaml)<br>[AuditLogPage](../Views/AuditLogPage.xaml) | [AuditDashboardViewModel](../ViewModels/AuditDashboardViewModel.cs)<br>[AuditLogViewModel](../ViewModels/AuditLogViewModel.cs) | **TODO:** None yet | — | **TODO:** Create WPF audit dashboard/log documents with filtering + export parity.
| CAPA lifecycle | [CapaPage](../Views/CapaPage.xaml)<br>[CapaEditDialog](../Views/Dialogs/CapaEditDialog.xaml) | [CapaViewModel](../ViewModels/CapaViewModel.cs)<br>[CAPAWorkflowViewModel](../ViewModels/CAPAWorkflowViewModel.cs)<br>[CapaCaseViewModel](../ViewModels/CapaCaseViewModel.cs)<br>[CapaEditDialogViewModel](../ViewModels/CapaEditDialogViewModel.cs) | **TODO:** None yet | — | **TODO:** Dockable CAPA workspace required in WPF.
| Validation & qualifications | [ValidationPage](../Views/ValidationPage.xaml) | [ValidationViewModel](../ViewModels/ValidationViewModel.cs)<br>[QualificationViewModel](../ViewModels/QualificationViewModel.cs) | **TODO:** None yet | — | **TODO:** Implement IQ/OQ/PQ WPF document.
| Document control | [DocumentControlPage](../Views/DocumentControlPage.xaml) | [DocumentControlViewModel](../ViewModels/DocumentControlViewModel.cs)<br>[DigitalSignatureViewModel](../ViewModels/DigitalSignatureViewModel.cs) | **TODO:** None yet | — | **TODO:** Build WPF document control surface with signature capture.
| Audit-ready reporting | — | [ReportViewModel](../ViewModels/ReportViewModel.cs)<br>[NotificationViewModel](../ViewModels/NotificationViewModel.cs) | **TODO:** None yet | — | **TODO:** Add WPF reporting panes that reuse MAUI analytics view-models.
| Change control & deviations | — | [ChangeControlViewModel](../ViewModels/ChangeControlViewModel.cs)<br>[DeviationViewModel](../ViewModels/DeviationViewModel.cs)<br>[RiskAssessmentViewModel](../ViewModels/RiskAssessmentViewModel.cs) | **TODO:** None yet | — | **TODO:** Surface change control/deviation tooling in WPF.
| Incident & inspection tracking | — | [IncidentReportViewModel](../ViewModels/IncidentReportViewModel.cs)<br>[InspectionViewModel](../ViewModels/InspectionViewModel.cs) | **TODO:** None yet | — | **TODO:** Provide incident/inspection WPF documents.
| Training & SOP governance | — | [SopViewModel](../ViewModels/SopViewModel.cs)<br>[TrainingRecordViewModel](../ViewModels/TrainingRecordViewModel.cs) | **TODO:** None yet | — | **TODO:** Implement WPF training matrix and SOP centers.
| Attachment workflows | — | [AttachmentViewModel](../ViewModels/AttachmentViewModel.cs) | **TODO:** None yet | — | **TODO:** Build WPF attachment picker dialogs aligned with MAUI behavior.

### Services
| MAUI Service(s) | Responsibility | WPF Adapter | Notes / TODO |
| --- | --- | --- | --- |
| [AuditService](../Services/AuditService.cs)<br>[DatabaseService.Audit.Helpers](../Services/DatabaseService.Audit.Helpers.cs)<br>[DatabaseService.Audit.QueryExtensions](../Services/DatabaseService.Audit.QueryExtensions.cs)<br>[DatabaseService.SystemEvents.QueryExtensions](../Services/DatabaseService.SystemEvents.QueryExtensions.cs) | System event log queries, audit hydration. | **TODO:** None yet | **TODO:** Feed WPF audit panes with MAUI audit pipelines.
| [CAPAService](../Services/CAPAService.cs)<br>[ICAPAService](../Services/Interfaces/ICAPAService.cs)<br>[CAPAAuditService](../Services/CAPAAuditService.cs)<br>[ICapaAuditService](../Services/Interfaces/ICapaAuditService.cs)<br>[DatabaseService.CapaExtensions](../Services/DatabaseService.CapaExtensions.cs)<br>[DatabaseService.Capa.Cases.Extensions](../Services/DatabaseService.Capa.Cases.Extensions.cs) | CAPA CRUD, audits, workflow helpers. | **TODO:** None yet | **TODO:** Mirror CAPA services in WPF backend adapter.
| [DeviationService](../Services/DeviationService.cs)<br>[DeviationAuditService](../Services/DeviationAuditService.cs)<br>[IDeviationAuditService](../Services/Interfaces/IDeviationAuditService.cs)<br>[DatabaseService.Deviations.Extensions](../Services/DatabaseService.Deviations.Extensions.cs)<br>[DatabaseService.DeviationAudit.Extensions](../Services/DatabaseService.DeviationAudit.Extensions.cs) | Deviation intake and auditing. | **TODO:** None yet | **TODO:** Create deviation document & persistence adapter.
| [IncidentService](../Services/IncidentService.cs)<br>[IncidentAuditService](../Services/IncidentAuditService.cs)<br>[IIncidentAuditService](../Services/Interfaces/IIncidentAuditService.cs)<br>[DatabaseService.Incidents.Extensions](../Services/DatabaseService.Incidents.Extensions.cs)<br>[DatabaseService.IncidentReports.Extensions](../Services/DatabaseService.IncidentReports.Extensions.cs)<br>[DatabaseService.IncidentAudits.Extensions](../Services/DatabaseService.IncidentAudits.Extensions.cs) | Incident response workflows. | **TODO:** None yet | **TODO:** Add WPF incident center.
| [ValidationService](../Services/ValidationService.cs)<br>[ValidationAuditService](../Services/ValidationAuditService.cs)<br>[ValidationAudit](../Services/ValidationAudit.cs)<br>[IValidationAuditService](../Services/Interfaces/IValidationAuditService.cs)<br>[DatabaseService.Validations.Extensions](../Services/DatabaseService.Validations.Extensions.cs) | Validation packages, audit trails, reporting. | **TODO:** None yet | **TODO:** Hook WPF validation workspace into shared services.
| [Document control helpers](../Services/DocumentControlLinkException.cs)<br>[DatabaseService.Documents.Extensions](../Services/DatabaseService.Documents.Extensions.cs)<br>[DatabaseService.Docs](../Services/DatabaseService.Docs.cs)<br>[DatabaseService.DigitalSignatures.Extensions](../Services/DatabaseService.DigitalSignatures.Extensions.cs) | Document lifecycle, SOP linkage, digital signatures. | **TODO:** None yet | **TODO:** Share document control adapters with WPF UI.
| [Risk & qualification](../Services/DatabaseService.RiskAssessments.Extensions.cs)<br>[DatabaseService.Qualifications.Extensions](../Services/DatabaseService.Qualifications.Extensions.cs) | Risk/qualification data access. | **TODO:** None yet | **TODO:** Introduce WPF dashboards for risk/qualification.
| [Training & SOP data](../Services/DatabaseService.TrainingRecords.Extensions.cs)<br>[DatabaseService.Sop.Extensions](../Services/DatabaseService.Sop.Extensions.cs) | Training matrices, SOP catalogs. | **TODO:** None yet | **TODO:** Provide WPF training records module.
| [NotificationService](../Services/NotificationService.cs)<br>[INotificationService](../Services/Interfaces/INotificationService.cs)<br>[DatabaseService.Notifications.Extensions](../Services/DatabaseService.Notifications.Extensions.cs) | Notifications/escalations for quality actions. | **TODO:** None yet | **TODO:** Wire notifications into WPF cockpit/toasts.
| [ExportService](../Services/ExportService.cs) | PDF/Excel export utilities used across quality modules. | **TODO:** None yet | **TODO:** Reuse export infrastructure from WPF documents.
| [DatabaseService.ChangeControls.Extensions](../Services/DatabaseService.ChangeControls.Extensions.cs) | Change control persistence helpers. | **TODO:** None yet | **TODO:** Add WPF change control screen backed by these extensions.
| [AttachmentService](../Services/AttachmentService.cs)<br>[AttachmentRetentionEnforcer](../Services/AttachmentRetentionEnforcer.cs)<br>[AttachmentEncryptionOptions](../Services/AttachmentEncryptionOptions.cs)<br>[IAttachmentService](../Services/Interfaces/IAttachmentService.cs)<br>[DatabaseService.Attachments.Extensions](../Services/DatabaseService.Attachments.Extensions.cs) | Attachment upload, retention, encryption helpers. | **TODO:** None yet | **TODO:** Provide WPF attachment dialogs wired to retention policies.

## Admin & Security

### Pages & View-Models
| Feature | MAUI Page(s) | MAUI View-Model(s) | WPF Document | WPF Pane | Notes / TODO |
| --- | --- | --- | --- | --- | --- |
| Admin control center | [AdminPanelPage](../Views/AdminPanelPage.xaml) | [AdminViewModel](../ViewModels/AdminViewModel.cs)<br>[SettingsViewModel](../ViewModels/SettingsViewModel.cs) | **TODO:** None yet | — | **TODO:** Build WPF admin dashboard with tabs mirroring MAUI experience.
| User management | [UsersPage](../Views/UsersPage.xaml) | [UserViewModel](../ViewModels/UserViewModel.cs) | **TODO:** None yet | — | **TODO:** Deliver WPF user management document.
| RBAC matrix | [UserRolePermissionPage](../Views/UserRolePermissionPage.xaml) | [UserRolePermissionViewModel](../ViewModels/UserRolePermissionViewModel.cs) | **TODO:** None yet | — | **TODO:** Add WPF RBAC designer pane.
| Re-authentication | [ReauthenticationDialog](../Views/Dialogs/ReauthenticationDialog.xaml) | — | **TODO:** None yet | — | **TODO:** Provide WPF credential prompt dialog.

### Services
| MAUI Service(s) | Responsibility | WPF Adapter | Notes / TODO |
| --- | --- | --- | --- |
| [AuthService](../Services/AuthService.cs)<br>[IAuthContext](../Services/Interfaces/IAuthContext.cs) | Authentication context, user session metadata. | **TODO:** None yet | **TODO:** Hook WPF login/session stack into shared auth service.
| [RBACService](../Services/RBACService.cs)<br>[IRBACService](../Services/Interfaces/IRBACService.cs)<br>[DatabaseService.Rbac.Extensions](../Services/DatabaseService.Rbac.Extensions.cs)<br>[DatabaseService.Rbac.CoreExtensions](../Services/DatabaseService.Rbac.CoreExtensions.cs) | Role-based access control policies and persistence. | **TODO:** None yet | **TODO:** Expose RBAC assignment tools in WPF.
| [UserService](../Services/UserService.cs)<br>[IUserService](../Services/Interfaces/IUserService.cs)<br>[DatabaseService.Users.Extensions](../Services/DatabaseService.Users.Extensions.cs)<br>[DatabaseService.UserOps.Extensions](../Services/DatabaseService.UserOps.Extensions.cs) | User CRUD, impersonation, user operations. | **TODO:** None yet | **TODO:** Provide WPF user admin adapters.
| [DatabaseService.Rollback.Extensions](../Services/DatabaseService.Rollback.Extensions.cs) | Rollback snapshot helpers for audit recovery. | **TODO:** None yet | **TODO:** Surface rollback restore flows inside WPF shell.
| [DatabaseService.Settings.Extensions](../Services/DatabaseService.Settings.Extensions.cs) | System configuration helpers used across admin tooling. | **TODO:** None yet | **TODO:** Map global settings editing to WPF panes.
| [DatabaseService.Notifications.Extensions](../Services/DatabaseService.Notifications.Extensions.cs) | Notification preferences & delivery toggles. | **TODO:** None yet | **TODO:** Connect WPF admin with notification settings UI.

## Diagnostics & Debug

### Pages & View-Models
| Feature | MAUI Page(s) | MAUI View-Model(s) | WPF Document | WPF Pane | Notes / TODO |
| --- | --- | --- | --- | --- | --- |
| Diagnostics dashboard | [DebugDashboardPage](../Views/Debug/DebugDashboardPage.xaml) | — | **TODO:** None yet | — | **TODO:** Add WPF diagnostics dashboard for telemetry snapshots.
| Log viewer | [LogViewerPage](../Views/Debug/LogViewerPage.xaml) | — | **TODO:** None yet | — | **TODO:** Provide WPF log viewer hooked to shared logging sinks.
| Health checks | [HealthPage](../Views/Debug/HealthPage.xaml) | — | **TODO:** None yet | — | **TODO:** Implement WPF health pane summarizing service status.

### Services
| MAUI Service(s) | Responsibility | WPF Adapter | Notes / TODO |
| --- | --- | --- | --- |
| [FileLogService](../Services/Logging/FileLogService.cs)<br>[ILogService](../Services/Logging/ILogService.cs) | File-based logging abstraction. | **TODO:** None yet | **TODO:** Bridge logging to WPF status panes.
| [UiInstrumentationService](../Services/Diagnostics/UiInstrumentationService.cs) | MAUI UI instrumentation hooks. | **TODO:** None yet | **TODO:** Port instrumentation to WPF dispatcher.

## Shared Infrastructure

### Services
| MAUI Service(s) | Responsibility | WPF Adapter | Notes / TODO |
| --- | --- | --- | --- |
| [DatabaseService](../Services/DatabaseService.cs)<br>[DbCommandWrapper](../Services/Database/DbCommandWrapper.cs)<br>[DbSlowQueryRegistry](../Services/Database/DbSlowQueryRegistry.cs)<br>[DbTelemetry](../Services/Database/DbTelemetry.cs)<br>[ShadowReplicator](../Services/Database/ShadowReplicator.cs) | Core data access, diagnostics, replication helpers. | **TODO:** None yet | **TODO:** Ensure WPF host reuses shared database infrastructure.
| [DatabaseService.Migrations](../Services/DatabaseService.Migrations.cs)<br>[DatabaseService.TestHooks](../Services/DatabaseService.TestHooks.cs) | Migration and test instrumentation helpers. | **TODO:** None yet | **TODO:** Port migration/test utilities for WPF dev workflows.
| [DatabaseService.DashboardExtensions](../Services/DatabaseService.DashboardExtensions.cs) | KPI aggregations backing dashboards. | **TODO:** None yet | **TODO:** Feed WPF cockpit and dashboards via shared extensions.
