using System;
using System.Configuration;
using System.Data.Entity;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;
using MedicalCenter.Data.Repositories;

namespace MedicalCenter.Data.UnitOfWork
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly MedicalCenterDbContext _context;

        private IMedicalServiceRepository _medicalServices;
        private IAppointmentRepository _appointments;
        private IDepartmentRepository _departments;
        private IRepository<ServiceTimeSlotEntity> _serviceTimeSlots;
        private IDoctorRepository _doctors;
        private IUserRepository _users;
        private IMedicalNoteRepository _medicalNotes;
        private IRepository<DoctorServiceEntity> _doctorServices;
        private IRepository<AuditLogEntity> _auditLog;

        public EfUnitOfWork(string connectionString, int commandTimeoutSeconds)
        {
            _context = new MedicalCenterDbContext(connectionString);
            _context.Database.CommandTimeout = commandTimeoutSeconds;
        }

        public IMedicalServiceRepository MedicalServices =>
            _medicalServices ?? (_medicalServices = new MedicalServiceRepository(_context));

        public IAppointmentRepository Appointments =>
            _appointments ?? (_appointments = new AppointmentRepository(_context));

        public IDepartmentRepository Departments =>
            _departments ?? (_departments = new DepartmentRepository(_context));

        public IRepository<ServiceTimeSlotEntity> ServiceTimeSlots =>
            _serviceTimeSlots ?? (_serviceTimeSlots = new Repository<ServiceTimeSlotEntity>(_context));

        public IDoctorRepository Doctors =>
            _doctors ?? (_doctors = new DoctorRepository(_context));

        public IUserRepository Users =>
            _users ?? (_users = new UserRepository(_context));

        public IMedicalNoteRepository MedicalNotes =>
            _medicalNotes ?? (_medicalNotes = new MedicalNoteRepository(_context));

        public IRepository<DoctorServiceEntity> DoctorServices =>
            _doctorServices ?? (_doctorServices = new Repository<DoctorServiceEntity>(_context));

        public IRepository<AuditLogEntity> AuditLog =>
            _auditLog ?? (_auditLog = new Repository<AuditLogEntity>(_context));

        public DbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

    /// <summary>
    /// Convenience wrapper that resolves the connection string from configuration
    /// so call-sites in services and view models can simply write `new UnitOfWork()`.
    /// </summary>
    public sealed class UnitOfWork : EfUnitOfWork
    {
        public UnitOfWork() : base(ResolveConnectionString(), ResolveTimeout())
        {
        }

        private static string ResolveConnectionString()
        {
            var cs = ConfigurationManager.ConnectionStrings["MedicalCenterDb"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connection string 'MedicalCenterDb' is not configured.");
            return cs;
        }

        private static int ResolveTimeout()
        {
            return int.TryParse(ConfigurationManager.AppSettings["DbCommandTimeoutSeconds"], out var timeout)
                ? timeout
                : 30;
        }
    }
}
