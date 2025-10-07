using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YasGMP.Models;
using PhotoType = YasGMP.Models.Enums.PhotoType;
using WorkOrderActionType = YasGMP.Models.Enums.WorkOrderActionType;

namespace YasGMP.Data
{
    /// <summary>
    /// Glavni DbContext klase za pristup bazi podataka YasGMP aplikacije.
    /// SadrÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ„Äľi DbSet-ove za sve entitete i konfiguracije modela.
    /// </summary>
    public class YasGmpDbContext : DbContext
    {
        /// <summary>
        /// Konstruktor prima DbContextOptions za konfiguraciju konteksta.
        /// </summary>
        /// <param name="options">Opcije konfiguracije DbContext-a</param>
        public YasGmpDbContext(DbContextOptions<YasGmpDbContext> options) : base(options)
        {
        }

		// === DbSet-ovi za sve entitete ===
        /// <summary>
        /// Gets or sets the admin activity logs.
        /// </summary>
        public DbSet<AdminActivityLog> AdminActivityLogs { get; set; }
        /// <summary>
        /// Gets or sets the api audit logs.
        /// </summary>
        public DbSet<ApiAuditLog> ApiAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the api keys.
        /// </summary>
        public DbSet<ApiKey> ApiKeys { get; set; }
        /// <summary>
        /// Gets or sets the api usage logs.
        /// </summary>
        public DbSet<ApiUsageLog> ApiUsageLogs { get; set; }
        /// <summary>
        /// Gets or sets the assets.
        /// </summary>
        public DbSet<Asset> Assets { get; set; }
        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        public DbSet<Attachment> Attachments { get; set; }
        /// <summary>
        /// Gets or sets the attachment links.
        /// </summary>
        public DbSet<AttachmentLink> AttachmentLinks { get; set; }
        /// <summary>
        /// Gets or sets the retention policies.
        /// </summary>
        public DbSet<RetentionPolicy> RetentionPolicies { get; set; }
        /// <summary>
        /// Gets or sets the attachment audit logs.
        /// </summary>
        public DbSet<AttachmentAuditLog> AttachmentAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the lkp statuses.
        /// </summary>
        public DbSet<LkpStatus> LkpStatuses { get; set; }
        /// <summary>
        /// Gets or sets the lkp work order types.
        /// </summary>
        public DbSet<LkpWorkOrderType> LkpWorkOrderTypes { get; set; }
        /// <summary>
        /// Gets or sets the lkp machine types.
        /// </summary>
        public DbSet<LkpMachineType> LkpMachineTypes { get; set; }
        /// <summary>
        /// Gets or sets the audit logs.
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the audit log entries.
        /// </summary>
        public DbSet<AuditLogEntry> AuditLogEntries { get; set; }
        /// <summary>
        /// Gets or sets the backup histories.
        /// </summary>
        public DbSet<BackupHistory> BackupHistories { get; set; }
        /// <summary>
        /// Gets or sets the buildings.
        /// </summary>
        public DbSet<Building> Buildings { get; set; }
        /// <summary>
        /// Gets or sets the calibrations.
        /// </summary>
        public DbSet<Calibration> Calibrations { get; set; }
        /// <summary>
        /// Gets or sets the calibration audits.
        /// </summary>
        public DbSet<CalibrationAudit> CalibrationAudits { get; set; }
        /// <summary>
        /// Gets or sets the calibration audit logs.
        /// </summary>
        public DbSet<CalibrationAuditLog> CalibrationAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the calibration export logs.
        /// </summary>
        public DbSet<CalibrationExportLog> CalibrationExportLogs { get; set; }
        /// <summary>
        /// Gets or sets the calibration sensors.
        /// </summary>
        public DbSet<CalibrationSensor> CalibrationSensors { get; set; }
        /// <summary>
        /// Gets or sets the capa actions.
        /// </summary>
        public DbSet<CapaAction> CapaActions { get; set; }
        /// <summary>
        /// Gets or sets the capa action logs.
        /// </summary>
        public DbSet<CapaActionLog> CapaActionLogs { get; set; }
        /// <summary>
        /// Gets or sets the capa cases.
        /// </summary>
        public DbSet<CapaCase> CapaCases { get; set; }
        /// <summary>
        /// Gets or sets the capa status histories.
        /// </summary>
        public DbSet<CapaStatusHistory> CapaStatusHistories { get; set; }
        /// <summary>
        /// Gets or sets the change controls.
        /// </summary>
        public DbSet<ChangeControl> ChangeControls { get; set; }
        /// <summary>
        /// Gets or sets the checklist items.
        /// </summary>
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        /// <summary>
        /// Gets or sets the checklist templates.
        /// </summary>
        public DbSet<ChecklistTemplate> ChecklistTemplates { get; set; }
        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        public DbSet<Comment> Comments { get; set; }
        /// <summary>
        /// Gets or sets the components.
        /// </summary>
        public DbSet<Component> Components { get; set; }
        /// <summary>
        /// Gets or sets the component devices.
        /// </summary>
        public DbSet<ComponentDevice> ComponentDevices { get; set; }
        /// <summary>
        /// Gets or sets the component models.
        /// </summary>
        public DbSet<ComponentModel> ComponentModels { get; set; }
        /// <summary>
        /// Gets or sets the component parts.
        /// </summary>
        public DbSet<ComponentPart> ComponentParts { get; set; }
        /// <summary>
        /// Gets or sets the component qualifications.
        /// </summary>
        public DbSet<ComponentQualification> ComponentQualifications { get; set; }
        /// <summary>
        /// Gets or sets the component types.
        /// </summary>
        public DbSet<ComponentType> ComponentTypes { get; set; }
        /// <summary>
        /// Gets or sets the config change logs.
        /// </summary>
        public DbSet<ConfigChangeLog> ConfigChangeLogs { get; set; }
        /// <summary>
        /// Gets or sets the contractor interventions.
        /// </summary>
        public DbSet<ContractorIntervention> ContractorInterventions { get; set; }
        /// <summary>
        /// Gets or sets the contractor intervention audits.
        /// </summary>
        public DbSet<ContractorInterventionAudit> ContractorInterventionAudits { get; set; }
        /// <summary>
        /// Gets or sets the dashboards.
        /// </summary>
        public DbSet<Dashboard> Dashboards { get; set; }
        /// <summary>
        /// Gets or sets the delegated permissions.
        /// </summary>
        public DbSet<DelegatedPermission> DelegatedPermissions { get; set; }
        /// <summary>
        /// Gets or sets the delete logs.
        /// </summary>
        public DbSet<DeleteLog> DeleteLogs { get; set; }
        /// <summary>
        /// Gets or sets the departments.
        /// </summary>
        public DbSet<Department> Departments { get; set; }
        /// <summary>
        /// Gets or sets the deviations.
        /// </summary>
        public DbSet<Deviation> Deviations { get; set; }
        /// <summary>
        /// Gets or sets the deviation audits.
        /// </summary>
        public DbSet<DeviationAudit> DeviationAudits { get; set; }
        /// <summary>
        /// Gets or sets the digital signatures.
        /// </summary>
        public DbSet<DigitalSignature> DigitalSignatures { get; set; }
        /// <summary>
        /// Gets or sets the document audit events.
        /// </summary>
        public DbSet<DocumentAuditEvent> DocumentAuditEvents { get; set; }
        /// <summary>
        /// Gets or sets the document controls.
        /// </summary>
        public DbSet<DocumentControl> DocumentControls { get; set; }
        /// <summary>
        /// Gets or sets the document versions.
        /// </summary>
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        /// <summary>
        /// Gets or sets the entity audit logs.
        /// </summary>
        public DbSet<EntityAuditLog> EntityAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the entity tags.
        /// </summary>
        public DbSet<EntityTag> EntityTags { get; set; }
        /// <summary>
        /// Gets or sets the export audit logs.
        /// </summary>
        public DbSet<ExportAuditLog> ExportAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the export print logs.
        /// </summary>
        public DbSet<ExportPrintLog> ExportPrintLogs { get; set; }
        /// <summary>
        /// Gets or sets the external contractors.
        /// </summary>
        public DbSet<ExternalContractor> ExternalContractors { get; set; }
        /// <summary>
        /// Gets or sets the failure modes.
        /// </summary>
        public DbSet<FailureMode> FailureModes { get; set; }
        /// <summary>
        /// Gets or sets the forensic user change logs.
        /// </summary>
        public DbSet<ForensicUserChangeLog> ForensicUserChangeLogs { get; set; }
        /// <summary>
        /// Gets or sets the incidents.
        /// </summary>
        public DbSet<Incident> Incidents { get; set; }
        /// <summary>
        /// Gets or sets the incident actions.
        /// </summary>
        public DbSet<IncidentAction> IncidentActions { get; set; }
        /// <summary>
        /// Gets or sets the incident audits.
        /// </summary>
        public DbSet<IncidentAudit> IncidentAudits { get; set; }
        /// <summary>
        /// Gets or sets the incident logs.
        /// </summary>
        public DbSet<IncidentLog> IncidentLogs { get; set; }
        /// <summary>
        /// Gets or sets the incident reports.
        /// </summary>
        public DbSet<IncidentReport> IncidentReports { get; set; }
        /// <summary>
        /// Gets or sets the inspections.
        /// </summary>
        public DbSet<Inspection> Inspections { get; set; }
        /// <summary>
        /// Gets or sets the integration logs.
        /// </summary>
        public DbSet<IntegrationLog> IntegrationLogs { get; set; }
        /// <summary>
        /// Gets or sets the inventory transactions.
        /// </summary>
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        /// <summary>
        /// Gets or sets the inventory locations.
        /// </summary>
        public DbSet<InventoryLocation> InventoryLocations { get; set; }
        /// <summary>
        /// Gets or sets the iot anomaly logs.
        /// </summary>
        public DbSet<IotAnomalyLog> IotAnomalyLogs { get; set; }
        /// <summary>
        /// Gets or sets the iot devices.
        /// </summary>
        public DbSet<IotDevice> IotDevices { get; set; }
        /// <summary>
        /// Gets or sets the iot event audits.
        /// </summary>
        public DbSet<IotEventAudit> IotEventAudits { get; set; }
        /// <summary>
        /// Gets or sets the iot gateways.
        /// </summary>
        public DbSet<IotGateway> IotGateways { get; set; }
        /// <summary>
        /// Gets or sets the iot sensor datas.
        /// </summary>
        public DbSet<IotSensorData> IotSensorDatas { get; set; }
        /// <summary>
        /// Gets or sets the irregularities logs.
        /// </summary>
        public DbSet<IrregularitiesLog> IrregularitiesLogs { get; set; }
        /// <summary>
        /// Gets or sets the job titles.
        /// </summary>
        public DbSet<JobTitle> JobTitles { get; set; }
        /// <summary>
        /// Gets or sets the kpi widgets.
        /// </summary>
        public DbSet<KpiWidget> KpiWidgets { get; set; }
        /// <summary>
        /// Gets or sets the locations.
        /// </summary>
        public DbSet<YasGMP.Models.Location> Locations { get; set; }
        /// <summary>
        /// Gets or sets the log entries.
        /// </summary>
        public DbSet<LogEntry> LogEntries { get; set; }
        /// <summary>
        /// Gets or sets the login attempt logs.
        /// </summary>
        public DbSet<LoginAttemptLog> LoginAttemptLogs { get; set; }
        /// <summary>
        /// Gets or sets the lookup domains.
        /// </summary>
        public DbSet<LookupDomain> LookupDomains { get; set; }
        /// <summary>
        /// Gets or sets the lookup values.
        /// </summary>
        public DbSet<LookupValue> LookupValues { get; set; }
        /// <summary>
        /// Gets or sets the machines.
        /// </summary>
        public DbSet<Machine> Machines { get; set; }
        /// <summary>
        /// Gets or sets the machine components.
        /// </summary>
        public DbSet<MachineComponent> MachineComponents { get; set; }
        /// <summary>
        /// Gets or sets the machine lifecycle events.
        /// </summary>
        public DbSet<MachineLifecycleEvent> MachineLifecycleEvents { get; set; }
        /// <summary>
        /// Gets or sets the machine models.
        /// </summary>
        public DbSet<MachineModel> MachineModels { get; set; }
        /// <summary>
        /// Gets or sets the machine statuses.
        /// </summary>
        public DbSet<MachineStatus> MachineStatuses { get; set; }
        /// <summary>
        /// Gets or sets the machine types.
        /// </summary>
        public DbSet<MachineType> MachineTypes { get; set; }
        /// <summary>
        /// Gets or sets the maintenance execution logs.
        /// </summary>
        public DbSet<MaintenanceExecutionLog> MaintenanceExecutionLogs { get; set; }
        /// <summary>
        /// Gets or sets the manufacturers.
        /// </summary>
        public DbSet<Manufacturer> Manufacturers { get; set; }
        /// <summary>
        /// Gets or sets the measurement units.
        /// </summary>
        public DbSet<MeasurementUnit> MeasurementUnits { get; set; }
        /// <summary>
        /// Gets or sets the mobile device logs.
        /// </summary>
        public DbSet<MobileDeviceLog> MobileDeviceLogs { get; set; }
        /// <summary>
        /// Gets or sets the notifications.
        /// </summary>
        public DbSet<Notification> Notifications { get; set; }
        /// <summary>
        /// Gets or sets the notification channel events.
        /// </summary>
        public DbSet<NotificationChannelEvent> NotificationChannelEvents { get; set; }
        /// <summary>
        /// Gets or sets the notification logs.
        /// </summary>
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        /// <summary>
        /// Gets or sets the notification queues.
        /// </summary>
        public DbSet<NotificationQueue> NotificationQueues { get; set; }
        /// <summary>
        /// Gets or sets the notification templates.
        /// </summary>
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        /// <summary>
        /// Gets or sets the parts.
        /// </summary>
        public DbSet<Part> Parts { get; set; }
        /// <summary>
        /// Gets or sets the part boms.
        /// </summary>
        public DbSet<PartBom> PartBoms { get; set; }
        /// <summary>
        /// Gets or sets the part change logs.
        /// </summary>
        public DbSet<PartChangeLog> PartChangeLogs { get; set; }
        /// <summary>
        /// Gets or sets the part supplier prices.
        /// </summary>
        public DbSet<PartSupplierPrice> PartSupplierPrices { get; set; }
        /// <summary>
        /// Gets or sets the part usages.
        /// </summary>
        public DbSet<PartUsage> PartUsages { get; set; }
        /// <summary>
        /// Gets or sets the part usage audit logs.
        /// </summary>
        public DbSet<PartUsageAuditLog> PartUsageAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the part stocks.
        /// </summary>
        public DbSet<PartStock> PartStocks { get; set; }
        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public DbSet<Permission> Permissions { get; set; }
        /// <summary>
        /// Gets or sets the permission change logs.
        /// </summary>
        public DbSet<PermissionChangeLog> PermissionChangeLogs { get; set; }
        /// <summary>
        /// Gets or sets the permission requests.
        /// </summary>
        public DbSet<PermissionRequest> PermissionRequests { get; set; }
        /// <summary>
        /// Gets or sets the photos.
        /// </summary>
        public DbSet<Photo> Photos { get; set; }
        /// <summary>
        /// Gets or sets the ppm plans.
        /// </summary>
        public DbSet<PpmPlan> PpmPlans { get; set; }
        /// <summary>
        /// Gets or sets the preventive maintenance plans.
        /// </summary>
        public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans { get; set; }
        /// <summary>
        /// Gets or sets the product recall logs.
        /// </summary>
        public DbSet<ProductRecallLog> ProductRecallLogs { get; set; }
        /// <summary>
        /// Gets or sets the qualifications.
        /// </summary>
        public DbSet<Qualification> Qualifications { get; set; }
        /// <summary>
        /// Gets or sets the qualification audit logs.
        /// </summary>
        public DbSet<QualificationAuditLog> QualificationAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the quality events.
        /// </summary>
        public DbSet<QualityEvent> QualityEvents { get; set; }
        /// <summary>
        /// Gets or sets the ref domains.
        /// </summary>
        public DbSet<RefDomain> RefDomains { get; set; }
        /// <summary>
        /// Gets or sets the ref values.
        /// </summary>
        public DbSet<RefValue> RefValues { get; set; }
        /// <summary>
        /// Gets or sets the reports.
        /// </summary>
        public DbSet<Report> Reports { get; set; }
        /// <summary>
        /// Gets or sets the report schedules.
        /// </summary>
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        /// <summary>
        /// Gets or sets the requalification schedules.
        /// </summary>
        public DbSet<RequalificationSchedule> RequalificationSchedules { get; set; }
        /// <summary>
        /// Gets or sets the responsible parties.
        /// </summary>
        public DbSet<ResponsibleParty> ResponsibleParties { get; set; }
        /// <summary>
        /// Gets or sets the risk assessments.
        /// </summary>
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        /// <summary>
        /// Gets or sets the risk assessment audit logs.
        /// </summary>
        public DbSet<RiskAssessmentAuditLog> RiskAssessmentAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the roles.
        /// </summary>
        public DbSet<Role> Roles { get; set; }
        /// <summary>
        /// Gets or sets the role audits.
        /// </summary>
        public DbSet<RoleAudit> RoleAudits { get; set; }
        /// <summary>
        /// Gets or sets the role permissions.
        /// </summary>
        public DbSet<RolePermission> RolePermissions { get; set; }
        /// <summary>
        /// Gets or sets the rooms.
        /// </summary>
        public DbSet<Room> Rooms { get; set; }
        /// <summary>
        /// Gets or sets the scheduled jobs.
        /// </summary>
        public DbSet<ScheduledJob> ScheduledJobs { get; set; }
        /// <summary>
        /// Gets or sets the scheduled job audit logs.
        /// </summary>
        public DbSet<ScheduledJobAuditLog> ScheduledJobAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the schema migration logs.
        /// </summary>
        public DbSet<SchemaMigrationLog> SchemaMigrationLogs { get; set; }
        /// <summary>
        /// Gets or sets the sensitive data access logs.
        /// </summary>
        public DbSet<SensitiveDataAccessLog> SensitiveDataAccessLogs { get; set; }
        /// <summary>
        /// Gets or sets the sensor data logs.
        /// </summary>
        public DbSet<SensorDataLog> SensorDataLogs { get; set; }
        /// <summary>
        /// Gets or sets the sensor models.
        /// </summary>
        public DbSet<SensorModel> SensorModels { get; set; }
        /// <summary>
        /// Gets or sets the sensor types.
        /// </summary>
        public DbSet<SensorTypeEntity> SensorTypes { get; set; }
        /// <summary>
        /// Gets or sets the session logs.
        /// </summary>
        public DbSet<SessionLog> SessionLogs { get; set; }
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public DbSet<Setting> Settings { get; set; }
        /// <summary>
        /// Gets or sets the setting audit logs.
        /// </summary>
        public DbSet<SettingAuditLog> SettingAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the setting versions.
        /// </summary>
        public DbSet<SettingVersion> SettingVersions { get; set; }
        /// <summary>
        /// Gets or sets the sites.
        /// </summary>
        public DbSet<Site> Sites { get; set; }
        /// <summary>
        /// Gets or sets the sop documents.
        /// </summary>
        public DbSet<SopDocument> SopDocuments { get; set; }
        /// <summary>
        /// Gets or sets the sop document logs.
        /// </summary>
        public DbSet<SopDocumentLog> SopDocumentLogs { get; set; }
        /// <summary>
        /// Gets or sets the sql query audit logs.
        /// </summary>
        public DbSet<SqlQueryAuditLog> SqlQueryAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the stock change logs.
        /// </summary>
        public DbSet<StockChangeLog> StockChangeLogs { get; set; }
        /// <summary>
        /// Gets or sets the stock levels.
        /// </summary>
        public DbSet<StockLevel> StockLevels { get; set; }
        /// <summary>
        /// Gets or sets the suppliers.
        /// </summary>
        public DbSet<Supplier> Suppliers { get; set; }
        /// <summary>
        /// Gets or sets the supplier audits.
        /// </summary>
        public DbSet<SupplierAudit> SupplierAudits { get; set; }
        /// <summary>
        /// Gets or sets the supplier risk audits.
        /// </summary>
        public DbSet<SupplierRiskAudit> SupplierRiskAudits { get; set; }
        /// <summary>
        /// Gets or sets the system event logs.
        /// </summary>
        public DbSet<SystemEventLog> SystemEventLogs { get; set; }
        /// <summary>
        /// Gets or sets the system parameters.
        /// </summary>
        public DbSet<SystemParameter> SystemParameters { get; set; }
        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public DbSet<Tag> Tags { get; set; }
        /// <summary>
        /// Gets or sets the tenants.
        /// </summary>
        public DbSet<Tenant> Tenants { get; set; }
        /// <summary>
        /// Gets or sets the training audit logs.
        /// </summary>
        public DbSet<TrainingAuditLog> TrainingAuditLogs { get; set; }
        /// <summary>
        /// Gets or sets the training logs.
        /// </summary>
        public DbSet<TrainingLog> TrainingLogs { get; set; }
        /// <summary>
        /// Gets or sets the training records.
        /// </summary>
        public DbSet<TrainingRecord> TrainingRecords { get; set; }
        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        public DbSet<User> Users { get; set; }
        /// <summary>
        /// Gets or sets the user activity logs.
        /// </summary>
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        /// <summary>
        /// Gets or sets the user audits.
        /// </summary>
        public DbSet<UserAudit> UserAudits { get; set; }
        /// <summary>
        /// Gets or sets the user esignatures.
        /// </summary>
        public DbSet<UserEsignature> UserEsignatures { get; set; }
        /// <summary>
        /// Gets or sets the user login logs.
        /// </summary>
        public DbSet<UserLoginLog> UserLoginLogs { get; set; }
        /// <summary>
        /// Gets or sets the user permissions.
        /// </summary>
        public DbSet<UserPermission> UserPermissions { get; set; }
        /// <summary>
        /// Gets or sets the user permission overrides.
        /// </summary>
        public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }
        /// <summary>
        /// Gets or sets the user role assignments.
        /// </summary>
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        /// <summary>
        /// Gets or sets the user role histories.
        /// </summary>
        public DbSet<UserRoleHistory> UserRoleHistories { get; set; }
        /// <summary>
        /// Gets or sets the user role mappings.
        /// </summary>
        public DbSet<UserRoleMapping> UserRoleMappings { get; set; }
        /// <summary>
        /// Gets or sets the user subscriptions.
        /// </summary>
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        /// <summary>
        /// Gets or sets the user trainings.
        /// </summary>
        public DbSet<UserTraining> UserTrainings { get; set; }
        /// <summary>
        /// Gets or sets the user window layouts.
        /// </summary>
        public DbSet<UserWindowLayout> UserWindowLayouts { get; set; }
        /// <summary>
        /// Gets or sets the warehouses.
        /// </summary>
        public DbSet<Warehouse> Warehouses { get; set; }
        /// <summary>
        /// Gets or sets the warehouse stocks.
        /// </summary>
        public DbSet<WarehouseStock> WarehouseStocks { get; set; }
        /// <summary>
        /// Gets or sets the work orders.
        /// </summary>
        public DbSet<WorkOrder> WorkOrders { get; set; }
        /// <summary>
        /// Gets or sets the work order attachments.
        /// </summary>
        public DbSet<WorkOrderAttachment> WorkOrderAttachments { get; set; }
        /// <summary>
        /// Gets or sets the work order audits.
        /// </summary>
        public DbSet<WorkOrderAudit> WorkOrderAudits { get; set; }
        /// <summary>
        /// Gets or sets the work order checklist items.
        /// </summary>
        public DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems { get; set; }
        /// <summary>
        /// Gets or sets the work order comments.
        /// </summary>
        public DbSet<WorkOrderComment> WorkOrderComments { get; set; }
        /// <summary>
        /// Gets or sets the work order parts.
        /// </summary>
        public DbSet<WorkOrderPart> WorkOrderParts { get; set; }
        /// <summary>
        /// Gets or sets the work order signatures.
        /// </summary>
        public DbSet<WorkOrderSignature> WorkOrderSignatures { get; set; }
        /// <summary>
        /// Gets or sets the work order status logs.
        /// </summary>
        public DbSet<WorkOrderStatusLog> WorkOrderStatusLogs { get; set; }

        /// <summary>
        /// Konfiguracija modela i relacija u bazi podataka.
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder za konfiguraciju EF Core modela</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            var customFieldsComparer = new ValueComparer<Dictionary<string, string>>(
                (left, right) =>
                    left == right ||
                    (left != null && right != null && left.Count == right.Count && !left.Except(right).Any()),
                dictionary =>
                    dictionary == null
                        ? 0
                        : dictionary.Aggregate(0, (hash, pair) =>
                            HashCode.Combine(
                                hash,
                                pair.Key != null ? StringComparer.Ordinal.GetHashCode(pair.Key) : 0,
                                pair.Value != null ? StringComparer.Ordinal.GetHashCode(pair.Value) : 0)),
                dictionary =>
                    dictionary == null
                        ? new Dictionary<string, string>()
                        : dictionary.ToDictionary(entry => entry.Key, entry => entry.Value));

            var customFieldsProperty = modelBuilder.Entity<Deviation>()
                .Property(d => d.CustomFields);

            customFieldsProperty.HasConversion(
                v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), jsonOptions),
                v => string.IsNullOrWhiteSpace(v)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>());

            customFieldsProperty.HasColumnType("TEXT");
            customFieldsProperty.Metadata.SetValueComparer(customFieldsComparer);

            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.UploadedBy)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.ApprovedBy)
                .WithMany()
                .HasForeignKey(a => a.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Attachment>()
                .HasIndex(a => new { a.Sha256, a.FileSize })
                .HasDatabaseName("ux_attachments_sha256_size")
                .IsUnique();

            modelBuilder.Entity<RetentionPolicy>()
                .Property(r => r.DeleteMode)
                .HasMaxLength(32)
                .HasDefaultValue("soft");

            modelBuilder.Entity<RetentionPolicy>()
                .HasOne(r => r.Attachment)
                .WithMany(a => a.RetentionPolicies)
                .HasForeignKey(r => r.AttachmentId)
                .OnDelete(DeleteBehavior.Cascade);

            var photoTypeConverter = new ValueConverter<PhotoType, string>(
                v => ConvertPhotoTypeToString(v),
                v => ConvertStringToPhotoType(v));

            modelBuilder.Entity<Photo>()
                .Property(p => p.Type)
                .HasConversion(photoTypeConverter)
                .HasMaxLength(32);

            modelBuilder.Entity<Photo>()
                .HasOne(p => p.WorkOrder)
                .WithMany(wo => wo.Photos)
                .HasForeignKey(p => p.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Photo>()
                .HasOne(p => p.Component)
                .WithMany(c => c.Photos)
                .HasForeignKey(p => p.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Photo>()
                .HasOne(p => p.Uploader)
                .WithMany(u => u.UploadedPhotos)
                .HasForeignKey(p => p.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SessionLog>()
                .HasOne(sl => sl.User)
                .WithMany(u => u.SessionLogs)
                .HasForeignKey(sl => sl.UserId);

            modelBuilder.Entity<SessionLog>()
                .HasOne(sl => sl.ImpersonatedBy)
                .WithMany()
                .HasForeignKey(sl => sl.ImpersonatedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.LastModifiedBy)
                .WithMany()
                .HasForeignKey(p => p.LastModifiedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Role>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Role>()
                .HasOne(r => r.LastModifiedBy)
                .WithMany()
                .HasForeignKey(r => r.LastModifiedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Konfiguracija enum polja u WorkOrderAudit (Action) kao string u bazi
            modelBuilder.Entity<WorkOrderAudit>()
                .Property(a => a.Action)
                .HasConversion(new EnumToStringConverter<WorkOrderActionType>())
                .IsRequired();

            modelBuilder.Entity<Calibration>()
                .HasOne(c => c.PreviousCalibration)
                .WithMany()
                .HasForeignKey(c => c.PreviousCalibrationId);

            modelBuilder.Entity<Calibration>()
                .HasOne(c => c.NextCalibration)
                .WithMany()
                .HasForeignKey(c => c.NextCalibrationId);

            // Primjer konfiguracije odnosa izmeĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľÄ‚â€žĂ˘â‚¬ĹˇÄ‚â€ąĂ‚ÂĂ„â€šĂ‹ÂÄ‚ËĂ˘â€šÂ¬ÄąË‡Ä‚â€šĂ‚Â¬Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Âu WorkOrder i WorkOrderAudit
            modelBuilder.Entity<WorkOrder>()
                .HasOne(wo => wo.CreatedBy)
                .WithMany(u => u.CreatedWorkOrders)
                .HasForeignKey(wo => wo.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(wo => wo.AssignedTo)
                .WithMany(u => u.AssignedWorkOrders)
                .HasForeignKey(wo => wo.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(wo => wo.RequestedBy)
                .WithMany()
                .HasForeignKey(wo => wo.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(wo => wo.LastModifiedBy)
                .WithMany()
                .HasForeignKey(wo => wo.LastModifiedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(wo => wo.Incident)
                .WithOne(i => i.WorkOrder)
                .HasForeignKey<WorkOrder>(wo => wo.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkOrderAudit>()
                .HasOne(a => a.WorkOrder)
                .WithMany(w => w.AuditTrail)
                .HasForeignKey(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkOrderAudit>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Konfiguracija indeksa za performanse na WorkOrderAudit ChangedAt
            modelBuilder.Entity<WorkOrderAudit>()
                .HasIndex(a => a.ChangedAt);

            // Konfiguracija User role enum kao string
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .IsRequired();

            // Ostale konfiguracije (npr. duÄ‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬Ă‚Â¦Ä‚â€žĂ˘â‚¬ĹˇÄ‚ËĂ˘â€šÂ¬ÄąÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ„Äľine stringova, odnosi, indeksi)

            // Primjer: jedinstveni indeks na korisniĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¤ko ime
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("role_permissions");

                entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.AssignedBy)
                    .WithMany()
                    .HasForeignKey(rp => rp.AssignedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserRoleMapping>(entity =>
            {
                entity.ToTable("user_roles");

                entity.HasKey(urm => new { urm.UserId, urm.RoleId });

                entity.HasOne(urm => urm.User)
                    .WithMany()
                    .HasForeignKey(urm => urm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(urm => urm.Role)
                    .WithMany()
                    .HasForeignKey(urm => urm.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(urm => urm.AssignedBy)
                    .WithMany()
                    .HasForeignKey(urm => urm.AssignedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles)
                .UsingEntity<RolePermission>(
                    j => j.HasOne(rp => rp.Permission)
                        .WithMany(p => p.RolePermissions)
                        .HasForeignKey(rp => rp.PermissionId),
                    j => j.HasOne(rp => rp.Role)
                        .WithMany(r => r.RolePermissions)
                        .HasForeignKey(rp => rp.RoleId));


            modelBuilder.Entity<User>()
                .HasMany(u => u.Permissions)
                .WithMany(p => p.Users)
                .UsingEntity<UserPermission>(
                    j => j.HasOne(up => up.Permission)
                        .WithMany(p => p.UserPermissions)
                        .HasForeignKey(up => up.PermissionId),
                    j => j.HasOne(up => up.User)
                        .WithMany(u => u.UserPermissions)
                        .HasForeignKey(up => up.UserId));

            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<UserRoleMapping>(

                    j => j.HasOne(urm => urm.Role)
                        .WithMany()
                        .HasForeignKey(urm => urm.RoleId),
                    j => j.HasOne(urm => urm.User)
                        .WithMany()
                        .HasForeignKey(urm => urm.UserId));

            modelBuilder.Entity<DelegatedPermission>(entity =>
            {
                entity.HasOne(dp => dp.FromUser)
                    .WithMany(u => u.DelegatedPermissions)
                    .HasForeignKey(dp => dp.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(dp => dp.ToUser)
                    .WithMany()
                    .HasForeignKey(dp => dp.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(dp => dp.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(dp => dp.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            ConfigureAdminActivityLog(modelBuilder);
            ConfigureApiKey(modelBuilder);
            ConfigureContractorInterventionAudit(modelBuilder);
            ConfigureInventoryTransaction(modelBuilder);
            ConfigureQualityEvent(modelBuilder);
            ConfigureStockLevel(modelBuilder);
            ConfigureWarehouse(modelBuilder);

            // Postavi cascade delete gdje je smisleno (npr. WorkOrder -> Comments)
            modelBuilder.Entity<WorkOrderComment>()
                .HasOne(c => c.WorkOrder)
                .WithMany(w => w.Comments)
                .HasForeignKey(c => c.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Work order attachments cascade on delete
            modelBuilder.Entity<WorkOrderAttachment>()
                .HasOne(a => a.WorkOrder)
                .WithMany()
                .HasForeignKey(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Work order status logs cascade on delete
            modelBuilder.Entity<WorkOrderStatusLog>()
                .HasOne(l => l.WorkOrder)
                .WithMany(w => w.StatusTimeline)
                .HasForeignKey(l => l.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkOrder -> Signatures (1:N, cascade)
            modelBuilder.Entity<WorkOrderSignature>()
                .HasOne(s => s.WorkOrder)
                .WithMany(w => w.Signatures)
                .HasForeignKey(s => s.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MachineComponent>()
                .HasMany(mc => mc.WorkOrders)
                .WithOne(wo => wo.Component)
                .HasForeignKey(wo => wo.ComponentId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- Machine relacije i ograniĂ„â€šĂ˘â‚¬ĹľÄ‚ËĂ˘â€šÂ¬ÄąË‡Ă„â€šĂ‹ÂÄ‚ËĂ˘â‚¬ĹˇĂ‚Â¬Ă„Ä…Ă„ÄľĂ„â€šĂ˘â‚¬ĹľÄ‚â€žĂ˘â‚¬Â¦Ă„â€šĂ˘â‚¬ĹˇÄ‚â€šĂ‚Â¤enja ---
            ConfigureMachine(modelBuilder);

            ConfigureMachineComponent(modelBuilder);
            ConfigureMachineLifecycleEvent(modelBuilder);
            ConfigureSupplier(modelBuilder);
            ConfigureExternalContractor(modelBuilder);
            ConfigureUser(modelBuilder);
        }

        private static void ConfigureMachine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Machine>(entity =>
            {
                entity.ToTable("machines");

                entity.HasOne(m => m.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(m => m.LastModifiedById)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_m_last_modified_by");

                entity.HasOne<MachineStatus>()
                    .WithMany()
                    .HasForeignKey(m => m.StatusId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_m_status");

                entity.HasOne<MachineType>()
                    .WithMany()
                    .HasForeignKey(m => m.MachineTypeId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_mach_machine_type");

                entity.HasOne<ResponsibleParty>()
                    .WithMany()
                    .HasForeignKey(m => m.ResponsiblePartyId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_machines_party");

                entity.HasIndex(m => m.Code)
                    .IsUnique();

                entity.Property(m => m.Code).HasMaxLength(64);
                entity.Property(m => m.Name).HasMaxLength(100);
                entity.Property(m => m.Status).HasMaxLength(30);
            });
        }

        private static void ConfigureMachineComponent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MachineComponent>(entity =>
            {
                entity.ToTable("machine_components");

                entity.HasKey(mc => mc.Id);

                entity.Property(mc => mc.Id).HasColumnName("id");
                entity.Property(mc => mc.MachineId).HasColumnName("machine_id");
                entity.Property(mc => mc.Code)
                    .HasColumnName("code")
                    .HasMaxLength(50);
                entity.Property(mc => mc.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100);
                entity.Property(mc => mc.Description)
                    .HasColumnName("description")
                    .HasMaxLength(255);
                entity.Property(mc => mc.Type)
                    .HasColumnName("type")
                    .HasMaxLength(50);
                entity.Property(mc => mc.Model)
                    .HasColumnName("model")
                    .HasMaxLength(255);
                entity.Property(mc => mc.InstallDate).HasColumnName("install_date");
                entity.Property(mc => mc.PurchaseDate).HasColumnName("purchase_date");
                entity.Property(mc => mc.WarrantyUntil).HasColumnName("warranty_until");
                entity.Property(mc => mc.WarrantyExpiry).HasColumnName("warranty_expiry");
                entity.Property(mc => mc.Status)
                    .HasColumnName("status")
                    .HasMaxLength(30);
                entity.Property(mc => mc.SerialNumber)
                    .HasColumnName("serial_number")
                    .HasMaxLength(255);
                entity.Property(mc => mc.Supplier)
                    .HasColumnName("supplier")
                    .HasMaxLength(255);
                entity.Property(mc => mc.RfidTag)
                    .HasColumnName("rfid_tag")
                    .HasMaxLength(255);
                entity.Property(mc => mc.IoTDeviceId)
                    .HasColumnName("io_tdevice_id")
                    .HasMaxLength(255);
                entity.Property(mc => mc.LifecyclePhase)
                    .HasColumnName("lifecycle_phase")
                    .HasMaxLength(255);
                entity.Property(mc => mc.IsCritical).HasColumnName("is_critical");
                entity.Property(mc => mc.Note)
                    .HasColumnName("note")
                    .HasMaxLength(255);
                entity.Property(mc => mc.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(255);
                entity.Property(mc => mc.Notes)
                    .HasColumnName("notes")
                    .HasMaxLength(255);
                entity.Property(mc => mc.SopDoc)
                    .HasColumnName("sop_doc")
                    .HasMaxLength(255);
                entity.Property(mc => mc.LastModified).HasColumnName("last_modified");
                entity.Property(mc => mc.LastModifiedById).HasColumnName("last_modified_by_id");
                entity.Property(mc => mc.SourceIp)
                    .HasColumnName("source_ip")
                    .HasMaxLength(255);
                entity.Property(mc => mc.IsDeleted).HasColumnName("is_deleted");
                entity.Property(mc => mc.ChangeVersion).HasColumnName("change_version");

                entity.Property(mc => mc.ComponentTypeId).HasColumnName("component_type_id");
                entity.Property(mc => mc.StatusId).HasColumnName("status_id");
                entity.Property(mc => mc.CreatedAt).HasColumnName("created_at");
                entity.Property(mc => mc.UpdatedAt).HasColumnName("updated_at");
                entity.Property(mc => mc.DeletedAt).HasColumnName("deleted_at");
                entity.Property(mc => mc.DeletedById).HasColumnName("deleted_by");

                entity.HasIndex(mc => mc.Code)
                    .IsUnique()
                    .HasDatabaseName("code");

                entity.HasIndex(mc => mc.MachineId)
                    .HasDatabaseName("ix_machine_components_machine_id");

                entity.HasOne(mc => mc.Machine)
                    .WithMany(m => m.Components)
                    .HasForeignKey(mc => mc.MachineId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_mc_machine");

                entity.HasOne(mc => mc.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(mc => mc.LastModifiedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<ComponentType>()
                    .WithMany()
                    .HasForeignKey(mc => mc.ComponentTypeId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_mc_type");

                entity.HasOne<RefValue>()
                    .WithMany()
                    .HasForeignKey(mc => mc.StatusId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_mc_status");
            });
        }
        private static void ConfigureMachineLifecycleEvent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MachineLifecycleEvent>(entity =>
            {
                entity.ToTable("machine_lifecycle_event");

                entity.Property(e => e.EventType)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.HasOne(e => e.Machine)
                    .WithMany()
                    .HasForeignKey(e => e.MachineId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PerformedBy)
                    .WithMany()
                    .HasForeignKey(e => e.PerformedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.LastModifiedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<RefValue>()
                    .WithMany()
                    .HasForeignKey(e => e.EventTypeId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_mle_event_type");
            });
        }
        private static void ConfigureSupplier(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("suppliers");

                entity.HasOne(s => s.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(s => s.LastModifiedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<RefValue>()
                    .WithMany()
                    .HasForeignKey(s => s.StatusId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_sup_status");

                entity.HasOne<RefValue>()
                    .WithMany()
                    .HasForeignKey(s => s.TypeId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_sup_type");
            });

            modelBuilder.Entity<SupplierAudit>(entity =>
            {
                entity.ToTable("supplier_audit");

                entity.HasKey(sa => sa.Id);

                entity.Property(sa => sa.Id).HasColumnName("id");
                entity.Property(sa => sa.SupplierId).HasColumnName("supplier_id");
                entity.Property(sa => sa.UserId).HasColumnName("user_id");
                entity.Property(sa => sa.ActionType)
                    .HasColumnName("action")
                    .HasMaxLength(255);
                entity.Property(sa => sa.ActionTimestamp).HasColumnName("changed_at");
                entity.Property(sa => sa.Details)
                    .HasColumnName("details")
                    .HasMaxLength(255);
                entity.Property(sa => sa.DeviceInfo)
                    .HasColumnName("device_info")
                    .HasMaxLength(255);
                entity.Property(sa => sa.SourceIp)
                    .HasColumnName("source_ip")
                    .HasMaxLength(255);
                entity.Property(sa => sa.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<SupplierRiskAudit>(entity =>
            {
                entity.ToTable("supplier_risk_audit");

                entity.HasKey(sra => sra.Id);

                entity.Property(sra => sra.Id).HasColumnName("id");
                entity.Property(sra => sra.SupplierId).HasColumnName("supplier_id");
                entity.Property(sra => sra.AuditDate).HasColumnName("audit_date");
                entity.Property(sra => sra.Score).HasColumnName("score");
                entity.Property(sra => sra.PerformedBy).HasColumnName("performed_by");
                entity.Property(sra => sra.Findings).HasColumnName("findings");
                entity.Property(sra => sra.CorrectiveActions).HasColumnName("corrective_actions");
                entity.Property(sra => sra.CreatedAt).HasColumnName("created_at");
                entity.Property(sra => sra.UpdatedAt).HasColumnName("updated_at");
            });
        }
        private static void ConfigureExternalContractor(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExternalContractor>(entity =>
            {
                entity.ToTable("external_contractors");

                entity.HasKey(ec => ec.Id);

                entity.Property(ec => ec.Id).HasColumnName("id");
                entity.Property(ec => ec.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100);
                entity.Property(ec => ec.CompanyName)
                    .HasColumnName("company_name")
                    .HasMaxLength(255);
                entity.Property(ec => ec.RegistrationNumber)
                    .HasColumnName("registration_number")
                    .HasMaxLength(255);
                entity.Property(ec => ec.Type)
                    .HasColumnName("type")
                    .HasMaxLength(255);
                entity.Property(ec => ec.ContactPerson)
                    .HasColumnName("contact_person")
                    .HasMaxLength(255);
                entity.Property(ec => ec.Email)
                    .HasColumnName("email")
                    .HasMaxLength(100);
                entity.Property(ec => ec.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(50);
                entity.Property(ec => ec.Address)
                    .HasColumnName("address")
                    .HasMaxLength(255);
                entity.Property(ec => ec.Certificates)
                    .HasColumnName("certificates")
                    .HasMaxLength(255);
                entity.Property(ec => ec.IsBlacklisted).HasColumnName("is_blacklisted");
                entity.Property(ec => ec.BlacklistReason)
                    .HasColumnName("blacklist_reason")
                    .HasMaxLength(255);
                entity.Property(ec => ec.RiskScore).HasColumnName("risk_score");
                entity.Property(ec => ec.SupplierId).HasColumnName("supplier_id");
                entity.Property(ec => ec.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(255);
                entity.Property(ec => ec.Note)
                    .HasColumnName("note")
                    .HasMaxLength(255);

                entity.Property(ec => ec.CreatedAt).HasColumnName("created_at");
                entity.Property(ec => ec.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(ec => ec.Supplier)
                    .WithMany()
                    .HasForeignKey(ec => ec.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasOne(u => u.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(u => u.LastModifiedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_users_last_modified_by");

                entity.HasOne<Role>()
                    .WithMany()
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_users_role");

                entity.HasOne<Department>()
                    .WithMany()
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_users_department");

                entity.HasOne<Tenant>()
                    .WithMany()
                    .HasForeignKey(u => u.TenantId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_users_tenant_id");

                entity.HasOne<JobTitle>()
                    .WithMany()
                    .HasForeignKey(u => u.JobTitleId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_users_job_title");
            });

            modelBuilder.Entity<UserRoleMapping>(entity =>
            {
                entity.ToTable("user_roles");

                entity.HasKey(urm => new { urm.UserId, urm.RoleId });

                entity.Property(urm => urm.UserId).HasColumnName("user_id");
                entity.Property(urm => urm.RoleId).HasColumnName("role_id");
                entity.Property(urm => urm.AssignedAt).HasColumnName("assigned_at");
                entity.Property(urm => urm.AssignedById).HasColumnName("assigned_by_id");
                entity.Property(urm => urm.ExpiresAt).HasColumnName("expires_at");
                entity.Property(urm => urm.IsActive).HasColumnName("is_active");
                entity.Property(urm => urm.ChangeVersion).HasColumnName("change_version");
                entity.Property(urm => urm.DigitalSignature).HasColumnName("digital_signature");
                entity.Property(urm => urm.SessionId).HasColumnName("session_id");
                entity.Property(urm => urm.SourceIp).HasColumnName("source_ip");
                entity.Property(urm => urm.Note).HasColumnName("note");
                entity.Property(urm => urm.Reason).HasColumnName("reason");
                entity.Property(urm => urm.CreatedAt).HasColumnName("created_at");
                entity.Property(urm => urm.UpdatedAt).HasColumnName("updated_at");
                entity.Property(urm => urm.GrantedById).HasColumnName("granted_by");
                entity.Property(urm => urm.GrantedAt).HasColumnName("granted_at");
                entity.Property(urm => urm.UserLabel).HasColumnName("user");
                entity.Property(urm => urm.RoleLabel).HasColumnName("role");
                entity.Property(urm => urm.AssignedByLegacyId).HasColumnName("assigned_by");

                entity.HasOne(urm => urm.User)
                    .WithMany()
                    .HasForeignKey(urm => urm.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_ur_user");

                entity.HasOne(urm => urm.Role)
                    .WithMany()
                    .HasForeignKey(urm => urm.RoleId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_ur_role");

                entity.HasOne(urm => urm.AssignedBy)
                    .WithMany()
                    .HasForeignKey(urm => urm.AssignedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_ur_assigned_by_id");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(urm => urm.GrantedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_user_roles_granted_by");
            });

            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.ToTable("user_permissions");

                entity.HasKey(up => new { up.UserId, up.PermissionId });

                entity.Property(up => up.UserId).HasColumnName("user_id");
                entity.Property(up => up.PermissionId).HasColumnName("permission_id");
                entity.Property(up => up.Allowed).HasColumnName("allowed");
                entity.Property(up => up.Reason).HasColumnName("reason");
                entity.Property(up => up.GrantedById).HasColumnName("granted_by");
                entity.Property(up => up.GrantedAt).HasColumnName("granted_at");
                entity.Property(up => up.AssignedAt).HasColumnName("assigned_at");
                entity.Property(up => up.AssignedById).HasColumnName("assigned_by");
                entity.Property(up => up.ExpiresAt).HasColumnName("expires_at");
                entity.Property(up => up.IsActive).HasColumnName("is_active");
                entity.Property(up => up.ChangeVersion).HasColumnName("change_version");
                entity.Property(up => up.DigitalSignature).HasColumnName("digital_signature");
                entity.Property(up => up.SessionId).HasColumnName("session_id");
                entity.Property(up => up.SourceIp).HasColumnName("source_ip");
                entity.Property(up => up.Note).HasColumnName("note");
                entity.Property(up => up.CreatedAt).HasColumnName("created_at");
                entity.Property(up => up.UpdatedAt).HasColumnName("updated_at");
                entity.Property(up => up.UserLabel).HasColumnName("user");
                entity.Property(up => up.PermissionLabel).HasColumnName("permission");
                entity.Property(up => up.PermissionCode).HasColumnName("code");

                entity.HasOne(up => up.User)
                    .WithMany()
                    .HasForeignKey(up => up.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_up_user");

                entity.HasOne(up => up.Permission)
                    .WithMany()
                    .HasForeignKey(up => up.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_up_perm");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(up => up.GrantedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_up_by");

                entity.HasOne(up => up.AssignedBy)
                    .WithMany()
                    .HasForeignKey(up => up.AssignedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DelegatedPermission>(entity =>
            {
                entity.ToTable("delegated_permissions");

                entity.HasKey(dp => dp.Id);

                entity.Property(dp => dp.Id).HasColumnName("id");
                entity.Property(dp => dp.FromUserId).HasColumnName("from_user_id");
                entity.Property(dp => dp.ToUserId).HasColumnName("to_user_id");
                entity.Property(dp => dp.PermissionId).HasColumnName("permission_id");
                entity.Property(dp => dp.StartAt).HasColumnName("start_at");
                entity.Property(dp => dp.EndAt).HasColumnName("end_at");
                entity.Property(dp => dp.Reason).HasColumnName("reason");
                entity.Property(dp => dp.IsActive).HasColumnName("is_active");
                entity.Property(dp => dp.IsRevoked).HasColumnName("is_revoked");
                entity.Property(dp => dp.ApprovedById).HasColumnName("approved_by");
                entity.Property(dp => dp.Note).HasColumnName("note");
                entity.Property(dp => dp.ChangeVersion).HasColumnName("change_version");
                entity.Property(dp => dp.DigitalSignature).HasColumnName("digital_signature");
                entity.Property(dp => dp.SessionId).HasColumnName("session_id");
                entity.Property(dp => dp.SourceIp).HasColumnName("source_ip");
                entity.Property(dp => dp.GrantedById).HasColumnName("granted_by");
                entity.Property(dp => dp.StartTimeRaw).HasColumnName("start_time");
                entity.Property(dp => dp.ExpiresAtRaw).HasColumnName("expires_at");
                entity.Property(dp => dp.RevokedLegacy).HasColumnName("revoked");
                entity.Property(dp => dp.RevokedAt).HasColumnName("revoked_at");
                entity.Property(dp => dp.CreatedAt).HasColumnName("created_at");
                entity.Property(dp => dp.UpdatedAt).HasColumnName("updated_at");
                entity.Property(dp => dp.PermissionLabel).HasColumnName("permission?");
                entity.Property(dp => dp.UserLabel).HasColumnName("user?");
                entity.Property(dp => dp.DelegationCode).HasColumnName("code");

                entity.HasOne(dp => dp.Permission)
                    .WithMany(p => p.DelegatedPermissions)
                    .HasForeignKey(dp => dp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_dp_perm");

                entity.HasOne(dp => dp.FromUser)
                    .WithMany()
                    .HasForeignKey(dp => dp.FromUserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_dp_from");

                entity.HasOne(dp => dp.ToUser)
                    .WithMany()
                    .HasForeignKey(dp => dp.ToUserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_dp_to");

                entity.HasOne(dp => dp.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(dp => dp.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(dp => dp.GrantedById)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_dp_by");
            });
        }
        private static void ConfigureAdminActivityLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AdminActivityLog>(entity =>
            {
                entity.ToTable("admin_activity_log");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AdminId).HasColumnName("admin_id");
                entity.Property(e => e.ActivityTime)
                    .HasColumnName("activity_time")
                    .IsRequired();
                entity.Property(e => e.Activity)
                    .HasColumnName("activity")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.AffectedTable)
                    .HasColumnName("affected_table")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.AffectedRecordId).HasColumnName("affected_record_id");
                entity.Property(e => e.Details)
                    .HasColumnName("details")
                    .HasColumnType("text");
                entity.Property(e => e.SourceIp)
                    .HasColumnName("source_ip")
                    .HasMaxLength(45);
                entity.Property(e => e.DeviceName)
                    .HasColumnName("device_name")
                    .HasMaxLength(128);
                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id")
                    .HasMaxLength(64);
                entity.Property(e => e.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(256);
                entity.Property(e => e.ChangeVersion).HasColumnName("change_version");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
                entity.Property(e => e.Note)
                    .HasColumnName("note")
                    .HasColumnType("text");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Action)
                    .HasColumnName("action")
                    .HasMaxLength(255);

                entity.HasIndex(e => e.AdminId)
                    .HasDatabaseName("fk_adminact_user");

                entity.HasOne(e => e.Admin)
                    .WithMany()
                    .HasForeignKey(e => e.AdminId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_adminact_user");
            });
        }

        private static void ConfigureApiKey(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("api_keys");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Key)
                    .HasColumnName("key_value")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasMaxLength(255);
                entity.Property(e => e.OwnerId).HasColumnName("owner_id");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.UsageLogsSnapshot)
                    .HasColumnName("usage_logs")
                    .HasMaxLength(255);

                entity.HasIndex(e => e.Key)
                    .HasDatabaseName("key_value")
                    .IsUnique();

                entity.HasIndex(e => e.OwnerId)
                    .HasDatabaseName("fk_apikey_owner");

                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_apikey_owner");

                entity.HasMany(e => e.UsageLogEntries)
                    .WithOne(l => l.ApiKey)
                    .HasForeignKey(l => l.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureContractorInterventionAudit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContractorInterventionAudit>(entity =>
            {
                entity.ToTable("contractor_intervention_audit");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.InterventionId)
                    .HasColumnName("intervention_id")
                    .IsRequired();
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Action)
                    .HasColumnName("action")
                    .HasMaxLength(30)
                    .IsRequired();
                entity.Property(e => e.Details).HasColumnName("details");
                entity.Property(e => e.ChangedAt).HasColumnName("changed_at");
                entity.Property(e => e.SourceIp)
                    .HasColumnName("source_ip")
                    .HasMaxLength(45);
                entity.Property(e => e.DeviceInfo)
                    .HasColumnName("device_info")
                    .HasMaxLength(255);
                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id")
                    .HasMaxLength(100);
                entity.Property(e => e.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(255);
                entity.Property(e => e.OldValue).HasColumnName("old_value");
                entity.Property(e => e.NewValue).HasColumnName("new_value");
                entity.Property(e => e.Note).HasColumnName("note");

                entity.HasIndex(e => e.InterventionId)
                    .HasDatabaseName("fk_cia_intervention");
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("fk_cia_user");

                entity.HasOne<ContractorIntervention>()
                    .WithMany()
                    .HasForeignKey(e => e.InterventionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_cia_intervention");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_cia_user");
            });
        }

        private static void ConfigureInventoryTransaction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryTransaction>(entity =>
            {
                entity.ToTable("inventory_transactions");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TransactionType)
                    .HasColumnName("transaction_type")
                    .HasMaxLength(16);

                entity.HasIndex(e => e.PartId)
                    .HasDatabaseName("fk_it_part");
                entity.HasIndex(e => e.PerformedById)
                    .HasDatabaseName("fk_it_user");
                entity.HasIndex(e => e.WarehouseId)
                    .HasDatabaseName("fk_it_warehouse");

                entity.HasOne(e => e.Part)
                    .WithMany()
                    .HasForeignKey(e => e.PartId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_it_part");

                entity.HasOne(e => e.Warehouse)
                    .WithMany()
                    .HasForeignKey(e => e.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_it_warehouse");

                entity.HasOne(e => e.PerformedBy)
                    .WithMany()
                    .HasForeignKey(e => e.PerformedById)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_it_user");
            });
        }

        private static void ConfigureQualityEvent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QualityEvent>(entity =>
            {
                entity.ToTable("quality_events");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EventTypeRaw)
                    .HasColumnName("event_type")
                    .HasMaxLength(14);
                entity.Property(e => e.DateOpen)
                    .HasColumnName("date_open")
                    .HasColumnType("date");
                entity.Property(e => e.DateClose)
                    .HasColumnName("date_close")
                    .HasColumnType("date");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.RelatedMachineId).HasColumnName("related_machine_id");
                entity.Property(e => e.RelatedComponentId).HasColumnName("related_component_id");
                entity.Property(e => e.StatusRaw)
                    .HasColumnName("status")
                    .HasMaxLength(12);
                entity.Property(e => e.Actions).HasColumnName("actions");
                entity.Property(e => e.DocFile)
                    .HasColumnName("doc_file")
                    .HasMaxLength(255);
                entity.Property(e => e.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(128);
                entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
                entity.Property(e => e.LastModifiedById).HasColumnName("last_modified_by_id");
                entity.Property(e => e.LastModified).HasColumnName("last_modified");

                entity.Property<int?>("TypeId").HasColumnName("type_id");
                entity.Property<int?>("StatusId").HasColumnName("status_id");

                entity.HasIndex(e => e.RelatedComponentId)
                    .HasDatabaseName("fk_qe_component");
                entity.HasIndex(e => e.RelatedMachineId)
                    .HasDatabaseName("fk_qe_machine");
                entity.HasIndex("StatusId")
                    .HasDatabaseName("idx_qe_status_id");
                entity.HasIndex("TypeId")
                    .HasDatabaseName("idx_qe_type_id");

                entity.HasOne(e => e.RelatedComponent)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedComponentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_qe_component");

                entity.HasOne(e => e.RelatedMachine)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedMachineId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_qe_machine");
            });
        }

        private static void ConfigureStockLevel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockLevel>(entity =>
            {
                entity.ToTable("stock_levels");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PartId).HasColumnName("part_id");
                entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.MinThreshold).HasColumnName("min_threshold");
                entity.Property(e => e.MaxThreshold).HasColumnName("max_threshold");
                entity.Property(e => e.AutoReorderTriggered).HasColumnName("auto_reorder_triggered");
                entity.Property(e => e.DaysBelowMin).HasColumnName("days_below_min");
                entity.Property(e => e.AlarmStatus)
                    .HasColumnName("alarm_status")
                    .HasMaxLength(30);
                entity.Property(e => e.AnomalyScore).HasColumnName("anomaly_score");
                entity.Property(e => e.LastModified).HasColumnName("last_modified");
                entity.Property(e => e.LastModifiedById).HasColumnName("last_modified_by_id");
                entity.Property(e => e.SourceIp)
                    .HasColumnName("source_ip")
                    .HasMaxLength(45);
                entity.Property(e => e.GeoLocation)
                    .HasColumnName("geo_location")
                    .HasMaxLength(100);
                entity.Property(e => e.Comment)
                    .HasColumnName("comment")
                    .HasMaxLength(255);
                entity.Property(e => e.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(128);
                entity.Property(e => e.EntryHash)
                    .HasColumnName("entry_hash")
                    .HasMaxLength(128);
                entity.Property(e => e.OldStateSnapshot)
                    .HasColumnName("old_state_snapshot")
                    .HasMaxLength(255);
                entity.Property(e => e.NewStateSnapshot)
                    .HasColumnName("new_state_snapshot")
                    .HasMaxLength(255);
                entity.Property(e => e.IsAutomated).HasColumnName("is_automated");
                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id")
                    .HasMaxLength(80);
                entity.Property(e => e.RelatedCaseId).HasColumnName("related_case_id");
                entity.Property(e => e.RelatedCaseType)
                    .HasColumnName("related_case_type")
                    .HasMaxLength(30);

                entity.HasIndex(e => e.PartId)
                    .HasDatabaseName("fk_sl_part");
                entity.HasIndex(e => e.WarehouseId)
                    .HasDatabaseName("fk_sl_warehouse");

                entity.HasOne(e => e.Part)
                    .WithMany(p => p.StockLevels)
                    .HasForeignKey(e => e.PartId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_sl_part");

                entity.HasOne(e => e.Warehouse)
                    .WithMany()
                    .HasForeignKey(e => e.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_sl_warehouse");

            });
        }

        private static void ConfigureWarehouse(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.ToTable("warehouses");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.Location)
                    .HasColumnName("location")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.ResponsibleId).HasColumnName("responsible_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.LastModified).HasColumnName("last_modified");
                entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
                entity.Property(e => e.LastModifiedById).HasColumnName("last_modified_by_id");
                entity.Property(e => e.QrCode)
                    .HasColumnName("qr_code")
                    .HasMaxLength(255);
                entity.Property(e => e.Note)
                    .HasColumnName("note")
                    .HasMaxLength(500);
                entity.Property(e => e.DigitalSignature)
                    .HasColumnName("digital_signature")
                    .HasMaxLength(128);
                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(30);
                entity.Property(e => e.IoTDeviceId)
                    .HasColumnName("io_tdevice_id")
                    .HasMaxLength(64);
                entity.Property(e => e.ClimateMode)
                    .HasColumnName("climate_mode")
                    .HasMaxLength(60);
                entity.Property(e => e.EntryHash)
                    .HasColumnName("entry_hash")
                    .HasMaxLength(128);
                entity.Property(e => e.SourceIp)
                    .HasColumnName("source_ip")
                    .HasMaxLength(45);
                entity.Property(e => e.IsQualified).HasColumnName("is_qualified");
                entity.Property(e => e.LastQualified).HasColumnName("last_qualified");
                entity.Property(e => e.SessionId)
                    .HasColumnName("session_id")
                    .HasMaxLength(80);
                entity.Property(e => e.AnomalyScore).HasColumnName("anomaly_score");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");

                entity.Property<int?>("LocationId").HasColumnName("location_id");

                entity.HasIndex(e => e.ResponsibleId)
                    .HasDatabaseName("fk_wh_user");
                entity.HasIndex("LocationId")
                    .HasDatabaseName("fk_wh_location");

                entity.HasOne(e => e.Responsible)
                    .WithMany()
                    .HasForeignKey(e => e.ResponsibleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_wh_user");

                entity.Ignore(e => e.ComplianceDocs);
            });
        }

        private static string ConvertPhotoTypeToString(PhotoType type)
            => type switch
            {
                PhotoType.Prije => "prije",
                PhotoType.Poslije => "poslije",
                PhotoType.Dokumentacija => "dokumentacija",
                _ => "drugo"
            };

        private static PhotoType ConvertStringToPhotoType(string? value)
            => value?.ToLowerInvariant() switch
            {
                "prije" => PhotoType.Prije,
                "poslije" => PhotoType.Poslije,
                "dokumentacija" => PhotoType.Dokumentacija,
                "drugo" => PhotoType.Drugo,
                _ => PhotoType.Drugo
            };
    }
}




























