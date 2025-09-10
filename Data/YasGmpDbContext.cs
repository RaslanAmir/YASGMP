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
        public DbSet<User> Users { get; set; }
        public DbSet<WorkOrderAudit> WorkOrderAudits { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<WorkOrderComment> WorkOrderComments { get; set; }
        public DbSet<WorkOrderSignature> WorkOrderSignatures { get; set; }
        public DbSet<WorkOrderStatusLog> WorkOrderStatusLogs { get; set; }
        public DbSet<WorkOrderAttachment> WorkOrderAttachments { get; set; }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineComponent> MachineComponents { get; set; }
        public DbSet<MachineType> MachineTypes { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Models.Location> Locations { get; set; }
        public DbSet<ResponsibleEntity> ResponsibleEntities { get; set; }
        public DbSet<Models.MachineStatus> MachineStatuses { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<CapaCase> CapaCases { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<TrainingLog> TrainingLogs { get; set; }
        public DbSet<SystemEventLog> SystemEventLogs { get; set; }
        // Dodaj ostale DbSet-ove po potrebi...

        /// <summary>
        /// Konfiguracija modela i relacija u bazi podataka.
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder za konfiguraciju EF Core modela</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // WorkOrderAudit.Action -> string (enum to string)
            modelBuilder.Entity<WorkOrderAudit>()
                .Property(a => a.Action)
                .HasConversion(new EnumToStringConverter<WorkOrderActionType>())
                .IsRequired();

            // WorkOrderAudit -> WorkOrder (1:N)
            modelBuilder.Entity<WorkOrderAudit>()
                .HasOne(a => a.WorkOrder)
                .WithMany(w => w.AuditTrail)
                .HasForeignKey(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkOrderAudit -> User (N:1, optional, SET NULL)
            modelBuilder.Entity<WorkOrderAudit>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index za performanse
            modelBuilder.Entity<WorkOrderAudit>()
                .HasIndex(a => a.ChangedAt);

            // User konfiguracija
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Ako je Role string u modelu, držimo jednostavno ograničenje; ako je enum, može se zamijeniti .HasConversion<string>()
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasMaxLength(64)
                .IsRequired();

            // WorkOrder -> Comments (1:N, cascade)
            modelBuilder.Entity<WorkOrderComment>()
                .HasOne(c => c.WorkOrder)
                .WithMany(w => w.Comments)
                .HasForeignKey(c => c.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkOrder -> Attachments (1:N, cascade)
            modelBuilder.Entity<WorkOrderAttachment>()
                .HasOne(a => a.WorkOrder)
                .WithMany(w => w.Attachments)
                .HasForeignKey(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkOrder -> Signatures (1:N, cascade)
            modelBuilder.Entity<WorkOrderSignature>()
                .HasOne(s => s.WorkOrder)
                .WithMany(w => w.Signatures)
                .HasForeignKey(s => s.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkOrder -> StatusLogs (1:N, cascade)
            modelBuilder.Entity<WorkOrderStatusLog>()
                .HasOne(s => s.WorkOrder)
                .WithMany(w => w.StatusLogs)
                .HasForeignKey(s => s.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Machine -> MachineComponents (1:N, cascade)
            modelBuilder.Entity<MachineComponent>()
                .HasOne(mc => mc.Machine)
                .WithMany(m => m.Components)
                .HasForeignKey(mc => mc.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Jednostavni indeksi/dužine stringova da se izbjegnu predugi varchars (opcionalno, prilagodi prema shemi)
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
