using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
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

            modelBuilder.Ignore<Asset>();

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

            ConfigureAdminActivityLog(modelBuilder);
            ConfigureApiKey(modelBuilder);
            ConfigureAttachment(modelBuilder);
            ConfigureContractorInterventionAudit(modelBuilder);
            ConfigureDelegatedPermission(modelBuilder);
            ConfigureInventoryTransaction(modelBuilder);
            ConfigurePermission(modelBuilder);
            ConfigureRole(modelBuilder);
            ConfigureQualityEvent(modelBuilder);
            ConfigureStockLevel(modelBuilder);
            ConfigureWarehouse(modelBuilder);
            ConfigureUserRelationships(modelBuilder);

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
        }

        private static void ConfigureAdminActivityLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AdminActivityLog>(entity =>
            {
                entity.ToTable("admin_activity_log");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AdminId).HasColumnName("admin_id");
                entity.Property(e => e.ActivityTime).HasColumnName("activity_time");
                entity.Property(e => e.Activity)
                    .HasColumnName("activity")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.AffectedTable)
                    .HasColumnName("affected_table")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(e => e.AffectedRecordId).HasColumnName("affected_record_id");
                entity.Property(e => e.Details).HasColumnName("details");
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
                entity.Property(e => e.Note).HasColumnName("note");

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

                entity.HasIndex(e => e.Key)
                    .HasDatabaseName("key_value")
                    .IsUnique();

                entity.HasIndex(e => e.OwnerId)
                    .HasDatabaseName("fk_apikey_owner");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("fk_apikey_owner");
            });
        }

        private static void ConfigureAttachment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasOne(a => a.UploadedBy)
                    .WithMany(u => u.UploadedAttachments)
                    .HasForeignKey(a => a.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ApprovedBy)
                    .WithMany(u => u.ApprovedAttachments)
                    .HasForeignKey(a => a.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
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

        private static void ConfigureDelegatedPermission(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DelegatedPermission>(entity =>
            {
                entity.HasOne(d => d.FromUser)
                    .WithMany(u => u.DelegationsGranted)
                    .HasForeignKey(d => d.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ToUser)
                    .WithMany(u => u.DelegatedPermissions)
                    .HasForeignKey(d => d.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ApprovedBy)
                    .WithMany(u => u.DelegationsApproved)
                    .HasForeignKey(d => d.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureInventoryTransaction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryTransaction>(entity =>
            {
                entity.ToTable("inventory_transactions");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PartId).HasColumnName("part_id");
                entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
                entity.Property(e => e.TransactionType)
                    .HasColumnName("transaction_type")
                    .HasMaxLength(8)
                    .IsRequired();
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");
                entity.Property(e => e.PerformedById).HasColumnName("performed_by");
                entity.Property(e => e.RelatedDocument)
                    .HasColumnName("related_document")
                    .HasMaxLength(255);
                entity.Property(e => e.Note).HasColumnName("note");

                entity.HasIndex(e => e.PartId)
                    .HasDatabaseName("fk_it_part");
                entity.HasIndex(e => e.PerformedById)
                    .HasDatabaseName("fk_it_user");
                entity.HasIndex(e => e.WarehouseId)
                    .HasDatabaseName("fk_it_warehouse");

                entity.HasOne(e => e.Part)
                    .WithMany(p => p.InventoryTransactions)
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

        private static void ConfigurePermission(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasOne(p => p.CreatedBy)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(p => p.LastModifiedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasOne(r => r.CreatedBy)
                    .WithMany()
                    .HasForeignKey(r => r.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.LastModifiedBy)
                    .WithMany()
                    .HasForeignKey(r => r.LastModifiedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureUserRelationships(ModelBuilder modelBuilder)
        {
            var userType = typeof(User);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(e => e.ClrType != null && e.ClrType != userType))
            {
                var clrType = entityType.ClrType!;
                if (clrType.IsGenericType &&
                    (clrType.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                     clrType.GetInterfaces().Any(i => i.IsGenericType &&
                                                        i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
                {
                    continue;
                }

                var entityBuilder = modelBuilder.Entity(clrType);

                foreach (var navigation in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (navigation.PropertyType != typeof(User))
                    {
                        continue;
                    }

                    if (navigation.GetCustomAttribute<InversePropertyAttribute>() != null)
                    {
                        continue;
                    }

                    var foreignKeyAttr = navigation.GetCustomAttribute<ForeignKeyAttribute>();
                    var fkName = foreignKeyAttr?.Name ?? navigation.Name + "Id";
                    var fkProperty = clrType.GetProperty(fkName, BindingFlags.Public | BindingFlags.Instance);
                    if (fkProperty == null)
                    {
                        continue;
                    }

                    var reference = entityBuilder.HasOne(userType, navigation.Name).WithMany();
                    reference.HasForeignKey(fkName);

                    var fkType = Nullable.GetUnderlyingType(fkProperty.PropertyType) ?? fkProperty.PropertyType;
                    if (fkType.IsValueType && Nullable.GetUnderlyingType(fkProperty.PropertyType) == null && fkType != typeof(string))
                    {
                        reference.OnDelete(DeleteBehavior.Restrict);
                    }
                    else
                    {
                        reference.OnDelete(DeleteBehavior.SetNull);
                    }
                }
            }
        }

        private static void ConfigureQualityEvent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QualityEvent>(entity =>
            {
                entity.ToTable("quality_events");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EventType)
                    .HasColumnName("event_type")
                    .HasConversion<string>()
                    .HasMaxLength(14);
                entity.Property(e => e.DateOpen)
                    .HasColumnName("date_open")
                    .HasColumnType("date");
                entity.Property(e => e.DateClose)
                    .HasColumnName("date_close")
                    .HasColumnType("date");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.RelatedMachineId).HasColumnName("related_machine");
                entity.Property(e => e.RelatedComponentId).HasColumnName("related_component");
                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasConversion<string>()
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


