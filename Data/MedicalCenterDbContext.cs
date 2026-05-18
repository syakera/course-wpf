using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data
{
    public class MedicalCenterDbContext : DbContext
    {
        public MedicalCenterDbContext()
            : base("name=MedicalCenterDb")
        {
            ConfigureContext();
        }

        public MedicalCenterDbContext(string connectionString)
            : base(connectionString)
        {
            ConfigureContext();
        }

        public MedicalCenterDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            ConfigureContext();
        }

        private void ConfigureContext()
        {
            Configuration.ProxyCreationEnabled = false;
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<DepartmentEntity> Departments { get; set; }
        public DbSet<MedicalServiceEntity> MedicalServices { get; set; }
        public DbSet<ServiceTimeSlotEntity> ServiceTimeSlots { get; set; }
        public DbSet<AppointmentEntity> Appointments { get; set; }
        public DbSet<AuditLogEntity> AuditLog { get; set; }
        public DbSet<DoctorEntity> Doctors { get; set; }
        public DbSet<DoctorServiceEntity> DoctorServices { get; set; }
        public DbSet<MedicalNoteEntity> MedicalNotes { get; set; }
        public DbSet<UserEntity> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MedicalServiceEntity>()
                .Property(x => x.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<AppointmentEntity>()
                .Property(x => x.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<MedicalServiceEntity>()
                .HasRequired(x => x.Department)
                .WithMany(d => d.MedicalServices)
                .HasForeignKey(x => x.DepartmentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ServiceTimeSlotEntity>()
                .HasRequired(x => x.MedicalService)
                .WithMany(s => s.ServiceTimeSlots)
                .HasForeignKey(x => x.ServiceId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<AppointmentEntity>()
                .HasRequired(x => x.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(x => x.ServiceId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MedicalNoteEntity>()
                .HasRequired(x => x.Appointment)
                .WithMany(a => a.MedicalNotes)
                .HasForeignKey(x => x.AppointmentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DoctorEntity>()
                .HasOptional(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .WillCascadeOnDelete(false);

            // Composite key for the DoctorServices junction table (many-to-many)
            modelBuilder.Entity<DoctorServiceEntity>()
                .HasKey(x => new { x.DoctorId, x.ServiceId });

            modelBuilder.Entity<DoctorServiceEntity>()
                .HasRequired(x => x.Doctor)
                .WithMany(d => d.DoctorServices)
                .HasForeignKey(x => x.DoctorId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DoctorServiceEntity>()
                .HasRequired(x => x.Service)
                .WithMany(s => s.DoctorServices)
                .HasForeignKey(x => x.ServiceId)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            var pendingAudit = CaptureAppointmentAuditEntries();
            PrepareAuditEntries(pendingAudit);
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync()
        {
            var pendingAudit = CaptureAppointmentAuditEntries();
            PrepareAuditEntries(pendingAudit);
            return await base.SaveChangesAsync().ConfigureAwait(false);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            var pendingAudit = CaptureAppointmentAuditEntries();
            PrepareAuditEntries(pendingAudit);
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private List<PendingAppointmentAudit> CaptureAppointmentAuditEntries()
        {
            return ChangeTracker
                .Entries<AppointmentEntity>()
                .Where(e => e.State == EntityState.Added
                            || e.State == EntityState.Modified
                            || e.State == EntityState.Deleted)
                .Select(e => new PendingAppointmentAudit
                {
                    Entry = e,
                    ActionType = e.State == EntityState.Added
                        ? "INSERT"
                        : e.State == EntityState.Modified
                            ? "UPDATE"
                            : "DELETE"
                })
                .ToList();
        }

        private void PrepareAuditEntries(List<PendingAppointmentAudit> pendingAudit)
        {
            if (pendingAudit.Count == 0)
                return;

            foreach (var item in pendingAudit)
            {
                AuditLog.Add(new AuditLogEntity
                {
                    EntityName = "Appointment",
                    EntityId = ResolveAppointmentId(item.Entry),
                    ActionType = item.ActionType,
                    ChangedAt = DateTime.Now
                });
            }
        }

        private static int? ResolveAppointmentId(DbEntityEntry<AppointmentEntity> entry)
        {
            if (entry.State == EntityState.Deleted)
            {
                object originalId = entry.OriginalValues[nameof(AppointmentEntity.Id)];
                return originalId == null ? (int?)null : Convert.ToInt32(originalId);
            }

            return entry.Entity?.Id;
        }

        private sealed class PendingAppointmentAudit
        {
            public DbEntityEntry<AppointmentEntity> Entry { get; set; }
            public string ActionType { get; set; }
        }
    }
}
