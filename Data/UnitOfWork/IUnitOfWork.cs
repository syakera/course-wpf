using System;
using System.Data.Entity;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;
using MedicalCenter.Data.Repositories;

namespace MedicalCenter.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IMedicalServiceRepository MedicalServices { get; }
        IAppointmentRepository Appointments { get; }
        IDepartmentRepository Departments { get; }
        IRepository<ServiceTimeSlotEntity> ServiceTimeSlots { get; }
        IDoctorRepository Doctors { get; }
        IUserRepository Users { get; }
        IMedicalNoteRepository MedicalNotes { get; }
        IRepository<DoctorServiceEntity> DoctorServices { get; }
        IRepository<AuditLogEntity> AuditLog { get; }

        DbContextTransaction BeginTransaction();
        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
