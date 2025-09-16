using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YasGMP.Models;
using WorkOrderActionType = YasGMP.Models.Enums.WorkOrderActionType;

namespace YasGMP.Data
{
    /// <summary>
    /// Glavni DbContext klase za pristup bazi podataka YasGMP aplikacije.
    /// Sadrži DbSet-ove za sve entitete i konfiguracije modela.
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
        public DbSet<AdminActivityLog> AdminActivityLogs { get; set; }
        public DbSet<ApiAuditLog> ApiAuditLogs { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<ApiUsageLog> ApiUsageLogs { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<CalibrationAuditLog> CalibrationAuditLogs { get; set; }
        public DbSet<CalibrationExportLog> CalibrationExportLogs { get; set; }
        public DbSet<CalibrationSensor> CalibrationSensors { get; set; }
        public DbSet<Calibration> Calibrations { get; set; }
        public DbSet<CapaActionLog> CapaActionLogs { get; set; }
        public DbSet<CapaAction> CapaActions { get; set; }
        public DbSet<CapaCase> CapaCases { get; set; }
        public DbSet<CapaStatusHistory> CapaStatusHistories { get; set; }
        public DbSet<ChangeControl> ChangeControls { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<ChecklistTemplate> ChecklistTemplates { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ComponentDevice> ComponentDevices { get; set; }
        public DbSet<ComponentModel> ComponentModels { get; set; }
        public DbSet<ComponentPart> ComponentParts { get; set; }
        public DbSet<ComponentQualification> ComponentQualifications { get; set; }
        public DbSet<ComponentType> ComponentTypes { get; set; }
        public DbSet<ConfigChangeLog> ConfigChangeLogs { get; set; }
        public DbSet<ContractorInterventionAudit> ContractorInterventionAudits { get; set; }
        public DbSet<ContractorIntervention> ContractorInterventions { get; set; }
        public DbSet<Dashboard> Dashboards { get; set; }
        public DbSet<DelegatedPermission> DelegatedPermissions { get; set; }
        public DbSet<DeleteLog> DeleteLogs { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<DeviationAudit> DeviationAudits { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<DigitalSignature> DigitalSignatures { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<DocumentControl> DocumentControls { get; set; }
        public DbSet<EntityAuditLog> EntityAuditLogs { get; set; }
        public DbSet<EntityTag> EntityTags { get; set; }
        public DbSet<ExportAuditLog> ExportAuditLogs { get; set; }
        public DbSet<ExportPrintLog> ExportPrintLogs { get; set; }
        public DbSet<ExternalContractor> ExternalContractors { get; set; }
        public DbSet<FailureMode> FailureModes { get; set; }
        public DbSet<ForensicUserChangeLog> ForensicUserChangeLogs { get; set; }
        public DbSet<IncidentAudit> IncidentAudits { get; set; }
        public DbSet<IncidentLog> IncidentLogs { get; set; }
        public DbSet<Inspections> Inspections { get; set; }
        public DbSet<IntegrationLog> IntegrationLogs { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<IotAnomalyLog> IotAnomalyLogs { get; set; }
        public DbSet<IotDevice> IotDevices { get; set; }
        public DbSet<IotEventAudit> IotEventAudits { get; set; }
        public DbSet<IotGateway> IotGateways { get; set; }
        public DbSet<IotSensorData> IotSensorDatas { get; set; }
        public DbSet<IrregularitiesLog> IrregularitiesLogs { get; set; }
        public DbSet<JobTitle> JobTitles { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LookupDomain> LookupDomains { get; set; }
        public DbSet<LookupValue> LookupValues { get; set; }
        public DbSet<MachineComponent> MachineComponents { get; set; }
        public DbSet<MachineLifecycleEvent> MachineLifecycleEvents { get; set; }
        public DbSet<MachineModel> MachineModels { get; set; }
        public DbSet<MachineType> MachineTypes { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<MeasurementUnit> MeasurementUnits { get; set; }
        public DbSet<MobileDeviceLog> MobileDeviceLogs { get; set; }
        public DbSet<NotificationQueue> NotificationQueues { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<PartBom> PartBoms { get; set; }
        public DbSet<PartSupplierPrice> PartSupplierPrices { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<PermissionChangeLog> PermissionChangeLogs { get; set; }
        public DbSet<PermissionRequest> PermissionRequests { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Photos> Photos { get; set; }
        public DbSet<PpmPlan> PpmPlans { get; set; }
        public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans { get; set; }
        public DbSet<QualityEvent> QualityEvents { get; set; }
        public DbSet<RefDomain> RefDomains { get; set; }
        public DbSet<RefValue> RefValues { get; set; }
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        public DbSet<RequalificationSchedule> RequalificationSchedules { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<RoleAudit> RoleAudits { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<ScheduledJobAuditLog> ScheduledJobAuditLogs { get; set; }
        public DbSet<ScheduledJob> ScheduledJobs { get; set; }
        public DbSet<SchemaMigrationLog> SchemaMigrationLogs { get; set; }
        public DbSet<SensitiveDataAccessLog> SensitiveDataAccessLogs { get; set; }
        public DbSet<SensorDataLogs> SensorDataLogs { get; set; }
        public DbSet<SensorModel> SensorModels { get; set; }
        public DbSet<SensorType> SensorTypes { get; set; }
        public DbSet<SessionLog> SessionLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SopDocumentLog> SopDocumentLogs { get; set; }
        public DbSet<SopDocument> SopDocuments { get; set; }
        public DbSet<StockLevel> StockLevels { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierAudit> SupplierAudits { get; set; }
        public DbSet<SupplierRiskAudit> SupplierRiskAudits { get; set; }
        public DbSet<SupplierEntity> SupplierEntities { get; set; }
        public DbSet<SystemEventLog> SystemEventLogs { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<UserAudit> UserAudits { get; set; }
        public DbSet<UserEsignature> UserEsignatures { get; set; }
        public DbSet<UserLoginLog> UserLoginLogs { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<UserRoleMapping> UserRoleMappings { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<UserTraining> UserTrainings { get; set; }
        public DbSet<UserWindowLayout> UserWindowLayouts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<VMachineComponentsUi> VMachineComponentsUis { get; set; }
        public DbSet<VSuppliersUi> VSuppliersUis { get; set; }
        public DbSet<ValidationAudit> ValidationAudits { get; set; }
        public DbSet<Validation> Validations { get; set; }
        public DbSet<VwAdminActivityLogAudit> VwAdminActivityLogAudits { get; set; }
        public DbSet<VwApiAuditLogAudit> VwApiAuditLogAudits { get; set; }
        public DbSet<VwApiKeysAudit> VwApiKeysAudits { get; set; }
        public DbSet<VwApiUsageLogAudit> VwApiUsageLogAudits { get; set; }
        public DbSet<VwAttachmentsAudit> VwAttachmentsAudits { get; set; }
        public DbSet<VwBuildingsAudit> VwBuildingsAudits { get; set; }
        public DbSet<VwCalibrationAuditLogAudit> VwCalibrationAuditLogAudits { get; set; }
        public DbSet<VwCalibrationExportLogAudit> VwCalibrationExportLogAudits { get; set; }
        public DbSet<VwCalibrationSensorsAudit> VwCalibrationSensorsAudits { get; set; }
        public DbSet<VwCalibrationsAudit> VwCalibrationsAudits { get; set; }
        public DbSet<VwCalibrationsFilter> VwCalibrationsFilters { get; set; }
        public DbSet<VwCapaActionLogAudit> VwCapaActionLogAudits { get; set; }
        public DbSet<VwCapaActionsAudit> VwCapaActionsAudits { get; set; }
        public DbSet<VwCapaCasesAudit> VwCapaCasesAudits { get; set; }
        public DbSet<VwCapaStatusHistoryAudit> VwCapaStatusHistoryAudits { get; set; }
        public DbSet<VwChecklistItemsAudit> VwChecklistItemsAudits { get; set; }
        public DbSet<VwChecklistTemplatesAudit> VwChecklistTemplatesAudits { get; set; }
        public DbSet<VwCommentsAudit> VwCommentsAudits { get; set; }
        public DbSet<VwComponentDevicesAudit> VwComponentDevicesAudits { get; set; }
        public DbSet<VwComponentModelsAudit> VwComponentModelsAudits { get; set; }
        public DbSet<VwComponentPartsAudit> VwComponentPartsAudits { get; set; }
        public DbSet<VwComponentQualificationsAudit> VwComponentQualificationsAudits { get; set; }
        public DbSet<VwComponentTypesAudit> VwComponentTypesAudits { get; set; }
        public DbSet<VwConfigChangeLogAudit> VwConfigChangeLogAudits { get; set; }
        public DbSet<VwContractorInterventionAuditAudit> VwContractorInterventionAuditAudits { get; set; }
        public DbSet<VwContractorInterventionsAudit> VwContractorInterventionsAudits { get; set; }
        public DbSet<VwDashboardsAudit> VwDashboardsAudits { get; set; }
        public DbSet<VwDelegatedPermissionsAudit> VwDelegatedPermissionsAudits { get; set; }
        public DbSet<VwDeleteLogAudit> VwDeleteLogAudits { get; set; }
        public DbSet<VwDepartmentsAudit> VwDepartmentsAudits { get; set; }
        public DbSet<VwDeviationAuditAudit> VwDeviationAuditAudits { get; set; }
        public DbSet<VwDeviationsAudit> VwDeviationsAudits { get; set; }
        public DbSet<VwDigitalSignaturesAudit> VwDigitalSignaturesAudits { get; set; }
        public DbSet<VwDocumentVersionsAudit> VwDocumentVersionsAudits { get; set; }
        public DbSet<VwEntityAuditLogAudit> VwEntityAuditLogAudits { get; set; }
        public DbSet<VwEntityTagsAudit> VwEntityTagsAudits { get; set; }
        public DbSet<VwExportPrintLogAudit> VwExportPrintLogAudits { get; set; }
        public DbSet<VwExternalContractorsAudit> VwExternalContractorsAudits { get; set; }
        public DbSet<VwFailureModesAudit> VwFailureModesAudits { get; set; }
        public DbSet<VwForensicUserChangeLogAudit> VwForensicUserChangeLogAudits { get; set; }
        public DbSet<VwIncidentLogAudit> VwIncidentLogAudits { get; set; }
        public DbSet<VwInspectionsAudit> VwInspectionsAudits { get; set; }
        public DbSet<VwIntegrationLogAudit> VwIntegrationLogAudits { get; set; }
        public DbSet<VwInventoryTransactionsAudit> VwInventoryTransactionsAudits { get; set; }
        public DbSet<VwIotAnomalyLogAudit> VwIotAnomalyLogAudits { get; set; }
        public DbSet<VwIotDevicesAudit> VwIotDevicesAudits { get; set; }
        public DbSet<VwIotEventAuditAudit> VwIotEventAuditAudits { get; set; }
        public DbSet<VwIotGatewaysAudit> VwIotGatewaysAudits { get; set; }
        public DbSet<VwIotSensorDataAudit> VwIotSensorDataAudits { get; set; }
        public DbSet<VwIrregularitiesLogAudit> VwIrregularitiesLogAudits { get; set; }
        public DbSet<VwJobTitlesAudit> VwJobTitlesAudits { get; set; }
        public DbSet<VwLocationsAudit> VwLocationsAudits { get; set; }
        public DbSet<VwLookupDomainAudit> VwLookupDomainAudits { get; set; }
        public DbSet<VwLookupValueAudit> VwLookupValueAudits { get; set; }
        public DbSet<VwMachineComponentsAudit> VwMachineComponentsAudits { get; set; }
        public DbSet<VwMachineLifecycleEventAudit> VwMachineLifecycleEventAudits { get; set; }
        public DbSet<VwMachineModelsAudit> VwMachineModelsAudits { get; set; }
        public DbSet<VwMachineTypesAudit> VwMachineTypesAudits { get; set; }
        public DbSet<VwMachinesAudit> VwMachinesAudits { get; set; }
        public DbSet<VwManufacturersAudit> VwManufacturersAudits { get; set; }
        public DbSet<VwMeasurementUnitsAudit> VwMeasurementUnitsAudits { get; set; }
        public DbSet<VwMobileDeviceLogAudit> VwMobileDeviceLogAudits { get; set; }
        public DbSet<VwNotificationQueueAudit> VwNotificationQueueAudits { get; set; }
        public DbSet<VwNotificationTemplatesAudit> VwNotificationTemplatesAudits { get; set; }
        public DbSet<VwPartBomAudit> VwPartBomAudits { get; set; }
        public DbSet<VwPartSupplierPricesAudit> VwPartSupplierPricesAudits { get; set; }
        public DbSet<VwPartsAudit> VwPartsAudits { get; set; }
        public DbSet<VwPermissionChangeLogAudit> VwPermissionChangeLogAudits { get; set; }
        public DbSet<VwPermissionRequestsAudit> VwPermissionRequestsAudits { get; set; }
        public DbSet<VwPermissionsAudit> VwPermissionsAudits { get; set; }
        public DbSet<VwPhotosAudit> VwPhotosAudits { get; set; }
        public DbSet<VwPpmPlansAudit> VwPpmPlansAudits { get; set; }
        public DbSet<VwPreventiveMaintenancePlansAudit> VwPreventiveMaintenancePlansAudits { get; set; }
        public DbSet<VwQualityEventsAudit> VwQualityEventsAudits { get; set; }
        public DbSet<VwReportScheduleAudit> VwReportScheduleAudits { get; set; }
        public DbSet<VwRequalificationScheduleAudit> VwRequalificationScheduleAudits { get; set; }
        public DbSet<VwRoleAuditAudit> VwRoleAuditAudits { get; set; }
        public DbSet<VwRolePermissionsAudit> VwRolePermissionsAudits { get; set; }
        public DbSet<VwRolesAudit> VwRolesAudits { get; set; }
        public DbSet<VwRoomsAudit> VwRoomsAudits { get; set; }
        public DbSet<VwScheduledJobAuditLogAudit> VwScheduledJobAuditLogAudits { get; set; }
        public DbSet<VwScheduledJobsAudit> VwScheduledJobsAudits { get; set; }
        public DbSet<VwScheduledJobsDue> VwScheduledJobsDues { get; set; }
        public DbSet<VwSchemaMigrationLogAudit> VwSchemaMigrationLogAudits { get; set; }
        public DbSet<VwSensitiveDataAccessLogAudit> VwSensitiveDataAccessLogAudits { get; set; }
        public DbSet<VwSensorDataEnriched> VwSensorDataEnricheds { get; set; }
        public DbSet<VwSensorDataLogsAudit> VwSensorDataLogsAudits { get; set; }
        public DbSet<VwSensorModelsAudit> VwSensorModelsAudits { get; set; }
        public DbSet<VwSensorTypesAudit> VwSensorTypesAudits { get; set; }
        public DbSet<VwSessionLogAudit> VwSessionLogAudits { get; set; }
        public DbSet<VwSitesAudit> VwSitesAudits { get; set; }
        public DbSet<VwSopDocumentLogAudit> VwSopDocumentLogAudits { get; set; }
        public DbSet<VwSopDocumentsAudit> VwSopDocumentsAudits { get; set; }
        public DbSet<VwStockCurrent> VwStockCurrents { get; set; }
        public DbSet<VwStockLevelsAudit> VwStockLevelsAudits { get; set; }
        public DbSet<VwSupplierRiskAuditAudit> VwSupplierRiskAuditAudits { get; set; }
        public DbSet<VwSuppliersAudit> VwSuppliersAudits { get; set; }
        public DbSet<VwSystemParametersAudit> VwSystemParametersAudits { get; set; }
        public DbSet<VwTagsAudit> VwTagsAudits { get; set; }
        public DbSet<VwTenantsAudit> VwTenantsAudits { get; set; }
        public DbSet<VwUserAuditAudit> VwUserAuditAudits { get; set; }
        public DbSet<VwUserEsignaturesAudit> VwUserEsignaturesAudits { get; set; }
        public DbSet<VwUserLoginAuditAudit> VwUserLoginAuditAudits { get; set; }
        public DbSet<VwUserPermissionsAudit> VwUserPermissionsAudits { get; set; }
        public DbSet<VwUserRolesAudit> VwUserRolesAudits { get; set; }
        public DbSet<VwUserSubscriptionsAudit> VwUserSubscriptionsAudits { get; set; }
        public DbSet<VwUserTrainingAudit> VwUserTrainingAudits { get; set; }
        public DbSet<VwUsersAudit> VwUsersAudits { get; set; }
        public DbSet<VwValidationsAudit> VwValidationsAudits { get; set; }
        public DbSet<VwWarehousesAudit> VwWarehousesAudits { get; set; }
        public DbSet<VwWorkOrderAuditAudit> VwWorkOrderAuditAudits { get; set; }
        public DbSet<VwWorkOrderChecklistItemAudit> VwWorkOrderChecklistItemAudits { get; set; }
        public DbSet<VwWorkOrderCommentsAudit> VwWorkOrderCommentsAudits { get; set; }
        public DbSet<VwWorkOrderPartsAudit> VwWorkOrderPartsAudits { get; set; }
        public DbSet<VwWorkOrderSignaturesAudit> VwWorkOrderSignaturesAudits { get; set; }
        public DbSet<VwWorkOrderStatusLogAudit> VwWorkOrderStatusLogAudits { get; set; }
        public DbSet<VwWorkOrdersAudit> VwWorkOrdersAudits { get; set; }
        public DbSet<VwWorkOrdersEnriched> VwWorkOrdersEnricheds { get; set; }
        public DbSet<VwWorkOrdersUser> VwWorkOrdersUsers { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WorkOrderAudit> WorkOrderAudits { get; set; }
        public DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems { get; set; }
        public DbSet<WorkOrderComments> WorkOrderComments { get; set; }
        public DbSet<WorkOrderPart> WorkOrderParts { get; set; }
        public DbSet<WorkOrderSignatures> WorkOrderSignatures { get; set; }
        public DbSet<WorkOrderStatusLog> WorkOrderStatusLogs { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }

        /// <summary>
        /// Konfiguracija modela i relacija u bazi podataka.
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder za konfiguraciju EF Core modela</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracija enum polja u WorkOrderAudit (Action) kao string u bazi
            modelBuilder.Entity<WorkOrderAudit>()
                .Property(a => a.Action)
                .HasConversion(new EnumToStringConverter<WorkOrderActionType>())
                .IsRequired();

            // Primjer konfiguracije odnosa između WorkOrder i WorkOrderAudit
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

            // Ostale konfiguracije (npr. dužine stringova, odnosi, indeksi)

            // Primjer: jedinstveni indeks na korisničko ime
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Primjeri dodatnih konfiguracija (po potrebi)
            modelBuilder.Entity<Manufacturer>()
                .Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<Models.Location>()
                .Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<Supplier>()
                .Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

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

            // --- Machine relacije i ograničenja ---

            // Machine -> MachineComponents (1:N, cascade)
            modelBuilder.Entity<MachineComponent>()
                .HasOne(mc => mc.Machine)
                .WithMany(m => m.Components)
                .HasForeignKey(mc => mc.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Jednostavni indeksi/dužine stringova
            modelBuilder.Entity<Machine>()
                .Property(m => m.Code)
                .HasMaxLength(128);

            modelBuilder.Entity<Machine>()
                .Property(m => m.Name)
                .HasMaxLength(256);

            modelBuilder.Entity<Machine>()
                .Property(m => m.Status)
                .HasMaxLength(64);

            modelBuilder.Entity<Machine>()
                .HasIndex(m => m.Code)
                .IsUnique(false);

            // Primjeri dodatnih konfiguracija (po potrebi)
            modelBuilder.Entity<Manufacturer>()
                .Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<Models.Location>()
                .Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<Supplier>()
                .Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired();
        }
    }
}


