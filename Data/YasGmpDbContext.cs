using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YasGMP.Models;
using YasGMP.Models.Enums;
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
        public DbSet<AdminActivityLog> AdminActivityLogs { get; set; }
        public DbSet<ApiAuditLog> ApiAuditLogs { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<ApiUsageLog> ApiUsageLogs { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<AttachmentLink> AttachmentLinks { get; set; }
        public DbSet<RetentionPolicy> RetentionPolicies { get; set; }
        public DbSet<AttachmentAuditLog> AttachmentAuditLogs { get; set; }
        public DbSet<LkpStatus> LkpStatuses { get; set; }
        public DbSet<LkpWorkOrderType> LkpWorkOrderTypes { get; set; }
        public DbSet<LkpMachineType> LkpMachineTypes { get; set; }
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
        public DbSet<InventoryLocation> InventoryLocations { get; set; }
        public DbSet<IotAnomalyLog> IotAnomalyLogs { get; set; }
        public DbSet<IotDevice> IotDevices { get; set; }
        public DbSet<IotEventAudit> IotEventAudits { get; set; }
        public DbSet<IotGateway> IotGateways { get; set; }
        public DbSet<IotSensorData> IotSensorDatas { get; set; }
        public DbSet<IrregularitiesLog> IrregularitiesLogs { get; set; }
        public DbSet<JobTitle> JobTitles { get; set; }
        public DbSet<KpiWidget> KpiWidgets { get; set; }
        public DbSet<YasGMP.Models.Location> Locations { get; set; }
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
        public DbSet<PartStock> PartStocks { get; set; }
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
        public DbSet<SensorTypeEntity> SensorTypes { get; set; }
        public DbSet<SessionLog> SessionLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<SettingAuditLog> SettingAuditLogs { get; set; }
        public DbSet<SettingVersion> SettingVersions { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SopDocument> SopDocuments { get; set; }
        public DbSet<SopDocumentLog> SopDocumentLogs { get; set; }
        public DbSet<SqlQueryAuditLog> SqlQueryAuditLogs { get; set; }
        public DbSet<StockChangeLog> StockChangeLogs { get; set; }
        public DbSet<StockLevel> StockLevels { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierAudit> SupplierAudits { get; set; }
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
                v => v switch
                {
                    PhotoType.Prije => "prije",
                    PhotoType.Poslije => "poslije",
                    PhotoType.Dokumentacija => "dokumentacija",
                    _ => "drugo"
                },
                v => v?.ToLowerInvariant() switch
                {
                    "prije" => PhotoType.Prije,
                    "poslije" => PhotoType.Poslije,
                    "dokumentacija" => PhotoType.Dokumentacija,
                    "drugo" => PhotoType.Drugo,
                    _ => PhotoType.Drugo
                });

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
    }
}




























