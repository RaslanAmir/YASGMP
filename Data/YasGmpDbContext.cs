using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YasGMP.Models;
using YasGMP.Models.Enums;

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
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<CapaCase> CapaCases { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<TrainingLog> TrainingLogs { get; set; }
        public DbSet<SystemEventLog> SystemEventLogs { get; set; }
        // Dodaj sve ostale DbSet-ove za entitete koje imaš u projektu...

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

            // Postavi cascade delete gdje je smisleno (npr. WorkOrder -> Comments)
            modelBuilder.Entity<WorkOrderComment>()
                .HasOne(c => c.WorkOrder)
                .WithMany(w => w.Comments)
                .HasForeignKey(c => c.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Slično za ostale relacije...

            // Dodaj još konfiguracija prema potrebi
        }
    }
}
