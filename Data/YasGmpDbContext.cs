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
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<AttachmentAuditLog> AttachmentAuditLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AuditLogEntry> AuditLogEntries { get; set; }
        public DbSet<BackupHistory> BackupHistories { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Calibration> Calibrations { get; set; }
        public DbSet<CalibrationAudit> CalibrationAudits { get; set; }
        public DbSet<CalibrationAuditLog> CalibrationAuditLogs { get; set; }
        public DbSet<CalibrationExportLog> CalibrationExportLogs { get; set; }
        public DbSet<CalibrationSensor> CalibrationSensors { get; set; }
        public DbSet<CapaAction> CapaActions { get; set; }
        public DbSet<CapaActionLog> CapaActionLogs { get; set; }
        public DbSet<CapaCase> CapaCases { get; set; }
        public DbSet<CapaStatusHistory> CapaStatusHistories { get; set; }
        public DbSet<ChangeControl> ChangeControls { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<ChecklistTemplate> ChecklistTemplates { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Component> Components { get; set; }
        public DbSet<ComponentDevice> ComponentDevices { get; set; }
        public DbSet<ComponentModel> ComponentModels { get; set; }
        public DbSet<ComponentPart> ComponentParts { get; set; }
        public DbSet<ComponentQualification> ComponentQualifications { get; set; }
        public DbSet<ComponentType> ComponentTypes { get; set; }
        public DbSet<ConfigChangeLog> ConfigChangeLogs { get; set; }
        public DbSet<ContractorIntervention> ContractorInterventions { get; set; }
        public DbSet<ContractorInterventionAudit> ContractorInterventionAudits { get; set; }
        public DbSet<Dashboard> Dashboards { get; set; }
        public DbSet<DelegatedPermission> DelegatedPermissions { get; set; }
        public DbSet<DeleteLog> DeleteLogs { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Deviation> Deviations { get; set; }
        public DbSet<DeviationAudit> DeviationAudits { get; set; }
        public DbSet<DigitalSignature> DigitalSignatures { get; set; }
        public DbSet<DocumentAuditEvent> DocumentAuditEvents { get; set; }
        public DbSet<DocumentControl> DocumentControls { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<EntityAuditLog> EntityAuditLogs { get; set; }
        public DbSet<EntityTag> EntityTags { get; set; }
        public DbSet<ExportAuditLog> ExportAuditLogs { get; set; }
        public DbSet<ExportPrintLog> ExportPrintLogs { get; set; }
        public DbSet<ExternalContractor> ExternalContractors { get; set; }
        public DbSet<FailureMode> FailureModes { get; set; }
        public DbSet<ForensicUserChangeLog> ForensicUserChangeLogs { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<IncidentAction> IncidentActions { get; set; }
        public DbSet<IncidentAudit> IncidentAudits { get; set; }
        public DbSet<IncidentLog> IncidentLogs { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<IntegrationLog> IntegrationLogs { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<IotAnomalyLog> IotAnomalyLogs { get; set; }
        public DbSet<IotDevice> IotDevices { get; set; }
        public DbSet<IotEventAudit> IotEventAudits { get; set; }
        public DbSet<IotGateway> IotGateways { get; set; }
        public DbSet<IotSensorData> IotSensorDatas { get; set; }
        public DbSet<IrregularitiesLog> IrregularitiesLogs { get; set; }
        public DbSet<JobTitle> JobTitles { get; set; }
        public DbSet<KpiWidget> KpiWidgets { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<LoginAttemptLog> LoginAttemptLogs { get; set; }
        public DbSet<LookupDomain> LookupDomains { get; set; }
        public DbSet<LookupValue> LookupValues { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineComponent> MachineComponents { get; set; }
        public DbSet<MachineLifecycleEvent> MachineLifecycleEvents { get; set; }
        public DbSet<MachineModel> MachineModels { get; set; }
        public DbSet<MachineStatus> MachineStatuses { get; set; }
        public DbSet<MachineType> MachineTypes { get; set; }
        public DbSet<MaintenanceExecutionLog> MaintenanceExecutionLogs { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<MeasurementUnit> MeasurementUnits { get; set; }
        public DbSet<MobileDeviceLog> MobileDeviceLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationChannelEvent> NotificationChannelEvents { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<NotificationQueue> NotificationQueues { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<PartBom> PartBoms { get; set; }
        public DbSet<PartChangeLog> PartChangeLogs { get; set; }
        public DbSet<PartSupplierPrice> PartSupplierPrices { get; set; }
        public DbSet<PartUsage> PartUsages { get; set; }
        public DbSet<PartUsageAuditLog> PartUsageAuditLogs { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<PermissionChangeLog> PermissionChangeLogs { get; set; }
        public DbSet<PermissionRequest> PermissionRequests { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<PpmPlan> PpmPlans { get; set; }
        public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans { get; set; }
        public DbSet<ProductRecallLog> ProductRecallLogs { get; set; }
        public DbSet<Qualification> Qualifications { get; set; }
        public DbSet<QualificationAuditLog> QualificationAuditLogs { get; set; }
        public DbSet<QualityEvent> QualityEvents { get; set; }
        public DbSet<RefDomain> RefDomains { get; set; }
        public DbSet<RefValue> RefValues { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        public DbSet<RequalificationSchedule> RequalificationSchedules { get; set; }
        public DbSet<ResponsibleParty> ResponsibleParties { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<RiskAssessmentAuditLog> RiskAssessmentAuditLogs { get; set; }
        public DbSet<RiskWorkflowEntry> RiskWorkflowEntries { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleAudit> RoleAudits { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<ScheduledJob> ScheduledJobs { get; set; }
        public DbSet<ScheduledJobAuditLog> ScheduledJobAuditLogs { get; set; }
        public DbSet<SchemaMigrationLog> SchemaMigrationLogs { get; set; }
        public DbSet<SensitiveDataAccessLog> SensitiveDataAccessLogs { get; set; }
        public DbSet<SensorDataLog> SensorDataLogs { get; set; }
        public DbSet<SensorModel> SensorModels { get; set; }
        public DbSet<SensorType> SensorTypes { get; set; }
        public DbSet<SessionLog> SessionLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<SettingAuditLog> SettingAuditLogs { get; set; }
        public DbSet<SettingVersion> SettingVersions { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SopDocument> SopDocuments { get; set; }
        public DbSet<SopDocumentLog> SopDocumentLogs { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }
        public DbSet<SqlQueryAuditLog> SqlQueryAuditLogs { get; set; }
        public DbSet<StockChangeLog> StockChangeLogs { get; set; }
        public DbSet<StockLevel> StockLevels { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierAudit> SupplierAudits { get; set; }
        public DbSet<SupplierEntity> SupplierEntities { get; set; }
        public DbSet<SupplierRiskAudit> SupplierRiskAudits { get; set; }
        public DbSet<SystemEventLog> SystemEventLogs { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TrainingAuditLog> TrainingAuditLogs { get; set; }
        public DbSet<TrainingLog> TrainingLogs { get; set; }
        public DbSet<TrainingRecord> TrainingRecords { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<UserAudit> UserAudits { get; set; }
        public DbSet<UserEsignature> UserEsignatures { get; set; }
        public DbSet<UserLoginLog> UserLoginLogs { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<UserRoleHistory> UserRoleHistories { get; set; }
        public DbSet<UserRoleMapping> UserRoleMappings { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<UserTraining> UserTrainings { get; set; }
        public DbSet<UserWindowLayout> UserWindowLayouts { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseStock> WarehouseStocks { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WorkOrderAttachment> WorkOrderAttachments { get; set; }
        public DbSet<WorkOrderAudit> WorkOrderAudits { get; set; }
        public DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems { get; set; }
        public DbSet<WorkOrderComment> WorkOrderComments { get; set; }
        public DbSet<WorkOrderPart> WorkOrderParts { get; set; }
        public DbSet<WorkOrderSignature> WorkOrderSignatures { get; set; }
        public DbSet<WorkOrderStatusLog> WorkOrderStatusLogs { get; set; }

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


