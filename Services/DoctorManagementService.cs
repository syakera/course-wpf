using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;
using MedicalCenter.Data.UnitOfWork;
using MedicalCenter.Models;

namespace MedicalCenter.Services
{
    public class DoctorManagementService
    {
        public List<Doctor> GetAllDoctors()
        {
            using (IUnitOfWork uow = new UnitOfWork())
            {
                var entities = uow.Doctors.GetActiveAsync().GetAwaiter().GetResult();
                return entities.Select(MapDoctor).ToList();
            }
        }

        public Doctor GetById(int id)
        {
            using (IUnitOfWork uow = new UnitOfWork())
            {
                var entity = uow.Doctors.GetByIdAsync(id).GetAwaiter().GetResult();
                return entity == null ? null : MapDoctor(entity);
            }
        }

        public List<Doctor> GetDoctorsBySpecialization(string specialization)
        {
            using (IUnitOfWork uow = new UnitOfWork())
            {
                IQueryable<DoctorEntity> query = uow.Doctors.QueryWithServices();
                if (!string.IsNullOrWhiteSpace(specialization))
                    query = query.Where(d => d.Specialization == specialization);

                var entities = query.Where(d => d.IsActive).OrderBy(d => d.FullName).ToList();
                return entities.Select(MapDoctor).ToList();
            }
        }

        public List<Doctor> GetDoctorsByServiceId(int serviceId)
        {
            using (IUnitOfWork uow = new UnitOfWork())
            {
                var entities = uow.Doctors.QueryWithServices()
                    .Where(d => d.IsActive && d.DoctorServices.Any(ds => ds.ServiceId == serviceId))
                    .OrderBy(d => d.FullName)
                    .ToList();
                return entities.Select(MapDoctor).ToList();
            }
        }

        public int SaveDoctor(Doctor doctor)
        {
            if (doctor == null) throw new ArgumentNullException(nameof(doctor));
            if (string.IsNullOrWhiteSpace(doctor.FullName))
                throw new InvalidOperationException("ФИО врача обязательно.");

            using (IUnitOfWork uow = new UnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    DoctorEntity entity;
                    if (doctor.Id > 0)
                    {
                        entity = uow.Doctors.GetByIdAsync(doctor.Id).GetAwaiter().GetResult();
                        if (entity == null)
                            throw new InvalidOperationException("Карточка врача не найдена.");
                    }
                    else
                    {
                        entity = new DoctorEntity();
                        uow.Doctors.Add(entity);

                        var accountService = new AccountService();
                        string login = accountService.BuildDoctorLogin(doctor.FullName);
                        string finalLogin = login;
                        int suffix = 1;
                        while (uow.Users.Query().Any(u => u.Username == finalLogin))
                        {
                            suffix++;
                            finalLogin = login + suffix;
                        }

                        var user = new UserEntity
                        {
                            Username = finalLogin,
                            Password = "doctor",
                            Role = (int)UserRole.Doctor,
                            DisplayName = doctor.FullName
                        };
                        uow.Users.Add(user);
                        uow.SaveChanges();
                        entity.UserId = user.Id;
                    }

                    entity.FullName = doctor.FullName.Trim();
                    entity.Specialization = doctor.Specialization;
                    entity.ExperienceYears = doctor.ExperienceYears;
                    entity.Description = doctor.Description;
                    entity.PhotoPath = doctor.PhotoPath;
                    entity.IsActive = doctor.IsActive;

                    SetDoctorServices(uow, entity, doctor.Services?.Select(s => s.Id).ToList() ?? new List<int>());

                    uow.SaveChanges();
                    tx.Commit();
                    return entity.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void SetDoctorServices(int doctorId, IEnumerable<int> serviceIds)
        {
            using (IUnitOfWork uow = new UnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    var entity = uow.Doctors.GetByIdAsync(doctorId).GetAwaiter().GetResult();
                    if (entity == null) return;
                    SetDoctorServices(uow, entity, serviceIds?.ToList() ?? new List<int>());
                    uow.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static void SetDoctorServices(IUnitOfWork uow, DoctorEntity entity, List<int> serviceIds)
        {
            var existing = entity.DoctorServices?.ToList() ?? new List<DoctorServiceEntity>();
            foreach (var link in existing)
            {
                if (!serviceIds.Contains(link.ServiceId))
                    uow.DoctorServices.Remove(link);
            }

            foreach (var id in serviceIds.Distinct())
            {
                bool already = existing.Any(l => l.ServiceId == id);
                if (!already)
                    uow.DoctorServices.Add(new DoctorServiceEntity { DoctorId = entity.Id, ServiceId = id });
            }
        }

        public void DeleteDoctor(int doctorId)
        {
            using (IUnitOfWork uow = new UnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    var entity = uow.Doctors.GetByIdAsync(doctorId).GetAwaiter().GetResult();
                    if (entity == null) return;

                    var hasActive = uow.Appointments.Query()
                        .Any(a => a.DoctorName == entity.FullName
                                  && (a.Status == "Ожидает" || a.Status == "Подтверждена"));
                    if (hasActive)
                        throw new InvalidOperationException(
                            "Нельзя удалить врача: у него есть активные записи пациентов. Сначала отмените или завершите все приёмы.");

                    entity.IsActive = false;
                    uow.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static Doctor MapDoctor(DoctorEntity entity)
        {
            return new Doctor
            {
                Id = entity.Id,
                FullName = entity.FullName,
                Specialization = entity.Specialization,
                ExperienceYears = entity.ExperienceYears,
                Description = entity.Description,
                PhotoPath = entity.PhotoPath,
                IsActive = entity.IsActive,
                UserId = entity.UserId,
                Services = entity.DoctorServices?
                    .Where(ds => ds.Service != null)
                    .Select(ds => new MedicalService
                    {
                        Id = ds.Service.Id,
                        ShortName = ds.Service.ShortName,
                        FullName = ds.Service.FullName,
                        Description = ds.Service.Description,
                        Category = ds.Service.Category,
                        Rating = ds.Service.Rating,
                        Price = ds.Service.Price,
                        Duration = ds.Service.Duration,
                        Specialist = ds.Service.Specialist,
                        HasDiscount = ds.Service.HasDiscount,
                        DiscountPercent = ds.Service.DiscountPercent
                    })
                    .ToList() ?? new List<MedicalService>()
            };
        }
    }
}
