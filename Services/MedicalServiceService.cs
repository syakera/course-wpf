using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;
using MedicalCenter.Data.UnitOfWork;
using MedicalCenter.Models;
using MedicalCenter.Services.Notifications;

namespace MedicalCenter.Services
{
    public class MedicalServiceService
    {
        private readonly string _connectionString;
        private readonly int _commandTimeoutSeconds;

        public MedicalServiceService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MedicalCenterDb"]?.ConnectionString
                                ?? throw new InvalidOperationException("Connection string 'MedicalCenterDb' is not configured.");
            _commandTimeoutSeconds = int.TryParse(ConfigurationManager.AppSettings["DbCommandTimeoutSeconds"], out var timeout)
                ? timeout
                : 30;

            DatabaseInitializer.EnsureDatabaseCreated(_connectionString, _commandTimeoutSeconds);
        }

        public List<MedicalService> LoadServices()
            => LoadServicesAsync().GetAwaiter().GetResult();

        public async Task<List<MedicalService>> LoadServicesAsync()
        {
            using (var uow = CreateUnitOfWork())
            {
                var entities = await uow.MedicalServices
                    .QueryWithDetails()
                    .OrderBy(x => x.ShortName)
                    .ToListAsync()
                    .ConfigureAwait(false);

                return entities.Select(MapService).ToList();
            }
        }

        public void AddOrUpdateService(MedicalService service)
            => AddOrUpdateServiceAsync(service).GetAwaiter().GetResult();

        public async Task AddOrUpdateServiceAsync(MedicalService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            using (var uow = CreateUnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    string departmentName = NormalizeDepartment(service.Department);
                    var department = await uow.Departments
                        .GetByNameAsync(departmentName)
                        .ConfigureAwait(false);

                    if (department == null)
                    {
                        department = new DepartmentEntity { Name = departmentName };
                        uow.Departments.Add(department);
                        await uow.SaveChangesAsync().ConfigureAwait(false);
                    }

                    MedicalServiceEntity entity;
                    if (service.Id > 0)
                    {
                        entity = await uow.MedicalServices
                            .GetByIdWithSlotsAsync(service.Id)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        entity = null;
                    }

                    if (entity == null)
                    {
                        entity = new MedicalServiceEntity();
                        uow.MedicalServices.Add(entity);
                    }

                    FillServiceEntity(entity, service, department.Id);

                    if (entity.ServiceTimeSlots == null)
                        entity.ServiceTimeSlots = new List<ServiceTimeSlotEntity>();

                    foreach (var oldSlot in entity.ServiceTimeSlots.ToList())
                    {
                        uow.ServiceTimeSlots.Remove(oldSlot);
                    }

                    foreach (var slot in (service.TimeSlots ?? new List<TimeSlot>())
                                 .Where(s => !string.IsNullOrWhiteSpace(s.Time)))
                    {
                        entity.ServiceTimeSlots.Add(new ServiceTimeSlotEntity
                        {
                            SlotTime = slot.Time.Trim(),
                            IsAvailable = slot.IsAvailable
                        });
                    }

                    await uow.SaveChangesAsync().ConfigureAwait(false);
                    tx.Commit();
                    service.Id = entity.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void DeleteService(int serviceId)
            => DeleteServiceAsync(serviceId).GetAwaiter().GetResult();

        public async Task DeleteServiceAsync(int serviceId)
        {
            using (var uow = CreateUnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    bool hasActiveAppointments = await uow.Appointments
                        .Query()
                        .AnyAsync(a => a.ServiceId == serviceId
                                       && (a.Status == "Ожидает" || a.Status == "Подтверждена"))
                        .ConfigureAwait(false);
                    if (hasActiveAppointments)
                    {
                        throw new InvalidOperationException(
                            "Нельзя удалить услугу: по ней есть активные записи пациентов.");
                    }

                    var entity = await uow.MedicalServices
                        .Query()
                        .FirstOrDefaultAsync(x => x.Id == serviceId)
                        .ConfigureAwait(false);
                    if (entity != null)
                    {
                        uow.MedicalServices.Remove(entity);
                        await uow.SaveChangesAsync().ConfigureAwait(false);
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public async Task CreateAppointmentAsync(Appointment appointment)
        {
            if (appointment == null) throw new ArgumentNullException(nameof(appointment));

            using (var uow = CreateUnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    var busy = await uow.Appointments
                        .Query()
                        .AnyAsync(a => a.ServiceId == appointment.ServiceId
                                       && a.AppointmentDate == appointment.AppointmentDate.Date
                                       && a.AppointmentTime == appointment.Time
                                       && a.Status != "Отменена")
                        .ConfigureAwait(false);
                    if (busy)
                        throw new InvalidOperationException("Этот слот уже занят. Выберите другое время.");

                    var sameTimeAppointments = await uow.Appointments
                        .Query()
                        .Where(a => a.AppointmentDate == appointment.AppointmentDate.Date
                                    && a.AppointmentTime == appointment.Time
                                    && a.Status != "Отменена"
                                    && a.Status != "Завершена"
                                    && a.Status != "Не явился")
                        .Select(a => new
                        {
                            a.PatientName,
                            a.PatientPhone
                        })
                        .ToListAsync()
                        .ConfigureAwait(false);

                    bool hasPatientConflict = sameTimeAppointments.Any(a =>
                        IsSamePatient(a.PatientPhone, a.PatientName, appointment.PatientPhone, appointment.PatientName));
                    if (hasPatientConflict)
                    {
                        throw new InvalidOperationException(
                            "У вас уже есть незавершенная запись на это время. Выберите другое время или завершите текущий прием.");
                    }

                    var doctorName = (appointment.Doctor ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(doctorName))
                    {
                        var sameDoctorSameDayAppointments = await uow.Appointments
                            .Query()
                            .Where(a => a.AppointmentDate == appointment.AppointmentDate.Date
                                        && a.DoctorName == doctorName
                                        && a.Status != "Отменена"
                                        && a.Status != "Завершена"
                                        && a.Status != "Не явился")
                            .Select(a => new
                            {
                                a.PatientName,
                                a.PatientPhone
                            })
                            .ToListAsync()
                            .ConfigureAwait(false);

                        bool hasSameDoctorSameDayConflict = sameDoctorSameDayAppointments.Any(a =>
                            IsSamePatient(a.PatientPhone, a.PatientName, appointment.PatientPhone, appointment.PatientName));
                        if (hasSameDoctorSameDayConflict)
                        {
                            throw new InvalidOperationException(
                                "У вас уже есть незавершенная запись к этому врачу на выбранную дату. Выберите другую дату или сначала завершите текущий прием.");
                        }
                    }

                    var entity = new AppointmentEntity
                    {
                        ServiceId = appointment.ServiceId,
                        PatientName = appointment.PatientName ?? string.Empty,
                        PatientPhone = appointment.PatientPhone ?? string.Empty,
                        AppointmentDate = appointment.AppointmentDate.Date,
                        AppointmentTime = appointment.Time ?? string.Empty,
                        DoctorName = doctorName,
                        Price = appointment.Price,
                        Status = appointment.Status ?? "Ожидает",
                        Comment = appointment.Comment,
                        PatientRating = appointment.PatientRating,
                        CreatedAt = DateTime.Now
                    };

                    uow.Appointments.Add(entity);
                    await uow.SaveChangesAsync().ConfigureAwait(false);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public async Task<List<string>> GetBusyTimesAsync(int serviceId, DateTime date)
        {
            using (var uow = CreateUnitOfWork())
            {
                return await uow.Appointments
                    .Query()
                    .Where(a => a.ServiceId == serviceId
                                && a.AppointmentDate == date.Date
                                && a.Status != "Отменена")
                    .Select(a => a.AppointmentTime)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<string>> GetDoctorsAsync()
        {
            using (var uow = CreateUnitOfWork())
            {
                return await uow.Doctors.Query()
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.FullName)
                    .Select(d => d.FullName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<Appointment>> GetAppointmentsForDoctorAsync(string doctorName, DateTime startDate, DateTime endDate)
        {
            using (var uow = CreateUnitOfWork())
            {
                var entities = await uow.Appointments
                    .QueryWithService()
                    .Where(a => a.DoctorName == doctorName
                                && a.AppointmentDate >= startDate.Date
                                && a.AppointmentDate <= endDate.Date)
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return entities.Select(MapAppointment).ToList();
            }
        }

        public async Task<List<Appointment>> GetAppointmentsForPatientAsync(string patientPhoneOrName)
        {
            using (var uow = CreateUnitOfWork())
            {
                var entities = await uow.Appointments
                    .QueryWithService()
                    .Where(a => a.PatientPhone == patientPhoneOrName || a.PatientName == patientPhoneOrName)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return entities.Select(MapAppointment).ToList();
            }
        }

        public async Task<List<Appointment>> GetPatientHistoryAsync(string patientPhone)
        {
            using (var uow = CreateUnitOfWork())
            {
                var entities = await uow.Appointments
                    .QueryWithService()
                    .Where(a => a.PatientPhone == patientPhone)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return entities.Select(MapAppointment).ToList();
            }
        }

        public async Task AddMedicalNoteAsync(int appointmentId, string noteType, string content, string doctorName)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            using (var uow = CreateUnitOfWork())
            {
                uow.MedicalNotes.Add(new MedicalNoteEntity
                {
                    AppointmentId = appointmentId,
                    NoteType = noteType ?? "Назначение",
                    Content = content,
                    DoctorName = doctorName,
                    CreatedAt = DateTime.Now
                });
                await uow.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<MedicalNote>> GetMedicalNotesByAppointmentAsync(int appointmentId)
        {
            using (var uow = CreateUnitOfWork())
            {
                var entities = await uow.MedicalNotes.GetByAppointmentAsync(appointmentId).ConfigureAwait(false);
                return entities.Select(e => new MedicalNote
                {
                    Id = e.Id,
                    AppointmentId = e.AppointmentId,
                    NoteType = e.NoteType,
                    Content = e.Content,
                    DoctorName = e.DoctorName,
                    CreatedAt = e.CreatedAt
                }).ToList();
            }
        }

        public async Task UpdateAppointmentRatingAsync(int appointmentId, int rating)
        {
            using (var uow = CreateUnitOfWork())
            {
                var entity = await uow.Appointments.GetByIdAsync(appointmentId).ConfigureAwait(false);
                if (entity == null) return;
                entity.PatientRating = rating;
                await uow.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<Appointment>> GetAppointmentsAsync()
        {
            using (var uow = CreateUnitOfWork())
            {
                var entities = await uow.Appointments
                    .QueryWithService()
                    .OrderByDescending(x => x.AppointmentDate)
                    .ThenBy(x => x.AppointmentTime)
                    .ToListAsync()
                    .ConfigureAwait(false);

                return entities.Select(MapAppointment).ToList();
            }
        }

        public async Task<List<MedicalService>> GetServicesByCategoryAsync(string category)
        {
            using (var uow = CreateUnitOfWork())
            {
                IQueryable<MedicalServiceEntity> query = uow.MedicalServices.QueryWithDetails();

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(x => x.Category == category);
                }

                var entities = await query
                    .OrderBy(x => x.ShortName)
                    .ToListAsync()
                    .ConfigureAwait(false);

                return entities.Select(MapService).ToList();
            }
        }

        public async Task<List<MedicalService>> SearchServicesAsync(
            string searchText,
            string category,
            decimal? minPrice,
            decimal? maxPrice,
            string sortOption)
        {
            using (var uow = CreateUnitOfWork())
            {
                IQueryable<MedicalServiceEntity> query = uow.MedicalServices.QueryWithDetails();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(x =>
                        (x.ShortName ?? string.Empty).Contains(searchText) ||
                        (x.FullName ?? string.Empty).Contains(searchText) ||
                        (x.Specialist ?? string.Empty).Contains(searchText) ||
                        (x.Description ?? string.Empty).Contains(searchText));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(x => x.Category == category);
                }

                var entities = await query.ToListAsync().ConfigureAwait(false);

                var models = entities.Select(MapService);
                if (minPrice.HasValue)
                {
                    models = models.Where(x => x.FinalPrice >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    models = models.Where(x => x.FinalPrice <= maxPrice.Value);
                }

                switch (sortOption)
                {
                    case "Цена (возр)":
                        models = models.OrderBy(x => x.FinalPrice);
                        break;
                    case "Цена (уб)":
                        models = models.OrderByDescending(x => x.FinalPrice);
                        break;
                    case "Рейтинг":
                        models = models.OrderByDescending(x => x.Rating);
                        break;
                    case "Длительность":
                        models = models.OrderBy(x => x.Duration);
                        break;
                    default:
                        models = models.OrderBy(x => x.ShortName);
                        break;
                }

                return models.ToList();
            }
        }

        public async Task<List<Appointment>> GetAppointmentsByStatusAsync(string status)
        {
            using (var uow = CreateUnitOfWork())
            {
                IQueryable<AppointmentEntity> query = uow.Appointments.QueryWithService();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(x => x.Status == status);
                }

                var entities = await query
                    .OrderByDescending(x => x.AppointmentDate)
                    .ThenBy(x => x.AppointmentTime)
                    .ToListAsync()
                    .ConfigureAwait(false);

                return entities.Select(MapAppointment).ToList();
            }
        }

        public Task UpdateAppointmentStatusAsync(int appointmentId, string status)
        {
            return UpdateAppointmentStatusAsync(appointmentId, status, UserRole.Patient);
        }

        public async Task UpdateAppointmentStatusAsync(int appointmentId, string status, bool isAdmin)
        {
            var role = isAdmin ? UserRole.Admin : UserRole.Doctor;
            await UpdateAppointmentStatusAsync(appointmentId, status, role).ConfigureAwait(false);
        }

        public async Task UpdateAppointmentStatusAsync(int appointmentId, string status, UserRole changedByRole)
        {
            using (var uow = CreateUnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    var entity = await uow.Appointments
                        .GetByIdAsync(appointmentId)
                        .ConfigureAwait(false);
                    if (entity != null)
                    {
                        entity.Status = string.IsNullOrWhiteSpace(status) ? "Ожидает" : status;
                        await uow.SaveChangesAsync().ConfigureAwait(false);

                        if (changedByRole == UserRole.Admin || changedByRole == UserRole.Doctor)
                        {
                            AppointmentStatusSubject.Instance.Notify(new AppointmentStatusChangedEvent
                            {
                                AppointmentId = entity.Id,
                                PatientName = entity.PatientName,
                                PatientPhone = entity.PatientPhone,
                                NewStatus = entity.Status,
                                ChangedAt = DateTime.Now,
                                ChangedByRole = changedByRole
                            });
                        }
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public async Task DeleteAppointmentAsync(int appointmentId)
        {
            using (var uow = CreateUnitOfWork())
            using (var tx = uow.BeginTransaction())
            {
                try
                {
                    var entity = await uow.Appointments
                        .GetByIdAsync(appointmentId)
                        .ConfigureAwait(false);
                    if (entity != null)
                    {
                        uow.Appointments.Remove(entity);
                        await uow.SaveChangesAsync().ConfigureAwait(false);
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            using (var uow = CreateUnitOfWork())
            {
                switch (tableName)
                {
                    case "MedicalServices":
                    {
                        var rows = await uow.MedicalServices
                            .QueryWithDetails()
                            .OrderByDescending(x => x.Id)
                            .Take(200)
                            .Select(x => new
                            {
                                x.Id,
                                x.ShortName,
                                x.FullName,
                                x.Category,
                                DepartmentName = x.Department.Name,
                                x.Price,
                                x.Duration,
                                x.Specialist,
                                x.Rating,
                                x.IsPopular,
                                x.HasDiscount
                            })
                            .ToListAsync()
                            .ConfigureAwait(false);

                        var table = new DataTable("MedicalServices");
                        table.Columns.Add("Id", typeof(int));
                        table.Columns.Add("ShortName", typeof(string));
                        table.Columns.Add("FullName", typeof(string));
                        table.Columns.Add("Category", typeof(string));
                        table.Columns.Add("DepartmentName", typeof(string));
                        table.Columns.Add("Price", typeof(decimal));
                        table.Columns.Add("Duration", typeof(int));
                        table.Columns.Add("Specialist", typeof(string));
                        table.Columns.Add("Rating", typeof(double));
                        table.Columns.Add("IsPopular", typeof(bool));
                        table.Columns.Add("HasDiscount", typeof(bool));

                        foreach (var row in rows)
                        {
                            table.Rows.Add(
                                row.Id,
                                row.ShortName,
                                row.FullName,
                                row.Category,
                                row.DepartmentName,
                                row.Price,
                                row.Duration,
                                row.Specialist,
                                row.Rating,
                                row.IsPopular,
                                row.HasDiscount);
                        }

                        return table;
                    }
                    case "Appointments":
                    {
                        var rows = await uow.Appointments
                            .QueryWithService()
                            .OrderByDescending(x => x.Id)
                            .Take(200)
                            .Select(x => new
                            {
                                x.Id,
                                x.ServiceId,
                                ServiceName = x.Service.ShortName,
                                x.PatientName,
                                x.PatientPhone,
                                x.AppointmentDate,
                                x.AppointmentTime,
                                x.DoctorName,
                                x.Price,
                                x.Status,
                                x.Comment,
                                x.PatientRating,
                                x.CreatedAt
                            })
                            .ToListAsync()
                            .ConfigureAwait(false);

                        var table = new DataTable("Appointments");
                        table.Columns.Add("Id", typeof(int));
                        table.Columns.Add("ServiceId", typeof(int));
                        table.Columns.Add("ServiceName", typeof(string));
                        table.Columns.Add("PatientName", typeof(string));
                        table.Columns.Add("PatientPhone", typeof(string));
                        table.Columns.Add("AppointmentDate", typeof(DateTime));
                        table.Columns.Add("AppointmentTime", typeof(string));
                        table.Columns.Add("DoctorName", typeof(string));
                        table.Columns.Add("Price", typeof(decimal));
                        table.Columns.Add("Status", typeof(string));
                        table.Columns.Add("Comment", typeof(string));
                        table.Columns.Add("PatientRating", typeof(int));
                        table.Columns.Add("CreatedAt", typeof(DateTime));

                        foreach (var row in rows)
                        {
                            table.Rows.Add(
                                row.Id,
                                row.ServiceId,
                                row.ServiceName,
                                row.PatientName,
                                row.PatientPhone,
                                row.AppointmentDate,
                                row.AppointmentTime,
                                row.DoctorName,
                                row.Price,
                                row.Status,
                                (object)row.Comment ?? DBNull.Value,
                                (object)row.PatientRating ?? DBNull.Value,
                                row.CreatedAt);
                        }

                        return table;
                    }
                    case "Doctors":
                    {
                        var rows = await uow.Doctors
                            .Query()
                            .OrderByDescending(x => x.Id)
                            .Take(200)
                            .Select(x => new
                            {
                                x.Id,
                                x.FullName,
                                x.Specialization,
                                x.ExperienceYears,
                                x.Description,
                                x.PhotoPath,
                                x.IsActive,
                                x.UserId
                            })
                            .ToListAsync()
                            .ConfigureAwait(false);

                        var table = new DataTable("Doctors");
                        table.Columns.Add("Id", typeof(int));
                        table.Columns.Add("FullName", typeof(string));
                        table.Columns.Add("Specialization", typeof(string));
                        table.Columns.Add("ExperienceYears", typeof(int));
                        table.Columns.Add("Description", typeof(string));
                        table.Columns.Add("PhotoPath", typeof(string));
                        table.Columns.Add("IsActive", typeof(bool));
                        table.Columns.Add("UserId", typeof(int));

                        foreach (var row in rows)
                        {
                            table.Rows.Add(
                                row.Id,
                                row.FullName,
                                row.Specialization,
                                row.ExperienceYears,
                                (object)row.Description ?? DBNull.Value,
                                (object)row.PhotoPath ?? DBNull.Value,
                                row.IsActive,
                                (object)row.UserId ?? DBNull.Value);
                        }

                        return table;
                    }
                    case "AuditLog":
                    {
                        var rows = await uow.AuditLog
                            .Query()
                            .OrderByDescending(x => x.Id)
                            .Take(200)
                            .Select(x => new
                            {
                                x.Id,
                                x.EntityName,
                                x.EntityId,
                                x.ActionType,
                                x.ChangedAt
                            })
                            .ToListAsync()
                            .ConfigureAwait(false);

                        var table = new DataTable("AuditLog");
                        table.Columns.Add("Id", typeof(int));
                        table.Columns.Add("EntityName", typeof(string));
                        table.Columns.Add("EntityId", typeof(int));
                        table.Columns.Add("ActionType", typeof(string));
                        table.Columns.Add("ChangedAt", typeof(DateTime));

                        foreach (var row in rows)
                        {
                            table.Rows.Add(
                                row.Id,
                                row.EntityName,
                                (object)row.EntityId ?? DBNull.Value,
                                row.ActionType,
                                row.ChangedAt);
                        }

                        return table;
                    }
                    default:
                        throw new ArgumentException("Unsupported table.", nameof(tableName));
                }
            }
        }

        public async Task<string> RunAsyncDemoAsync()
        {
            var sw = Stopwatch.StartNew();

            using (var uow = CreateUnitOfWork())
            {
                var servicesTask = uow.MedicalServices
                    .QueryWithDetails()
                    .OrderBy(x => x.ShortName)
                    .Take(50)
                    .ToListAsync();

                await Task.Delay(3000).ConfigureAwait(false);
                var services = await servicesTask.ConfigureAwait(false);

                sw.Stop();
                return $"Async demo: получено {services.Count} услуг за {sw.ElapsedMilliseconds} мс. UI не блокируется, т.к. используется await.";
            }
        }

        public async Task<string> RunTransactionRollbackDemoAsync()
        {
            using (var uow = CreateUnitOfWork())
            {
                int serviceId = await uow.MedicalServices
                    .Query()
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (serviceId <= 0)
                {
                    return "Transaction demo: в БД нет услуг, поэтому нечего использовать для демонстрации транзакции.";
                }

                int before = await uow.Appointments.Query().CountAsync().ConfigureAwait(false);

                using (var tx = uow.BeginTransaction())
                {
                    try
                    {
                        uow.Appointments.Add(new AppointmentEntity
                        {
                            ServiceId = serviceId,
                            PatientName = "TX Demo",
                            PatientPhone = "+000000000",
                            AppointmentDate = DateTime.Today,
                            AppointmentTime = "00:00",
                            DoctorName = "Transaction Test",
                            Price = 0m,
                            Status = "TX_DEMO",
                            CreatedAt = DateTime.Now
                        });

                        await uow.SaveChangesAsync().ConfigureAwait(false);

                        throw new InvalidOperationException("Демонстрационный откат транзакции.");
                    }
                    catch (InvalidOperationException)
                    {
                        tx.Rollback();
                    }
                }

                int after = await uow.Appointments.Query().CountAsync().ConfigureAwait(false);
                bool rolledBack = before == after;
                return rolledBack
                    ? "Transaction demo: запись была создана внутри транзакции и успешно откатена (Rollback)."
                    : "Transaction demo: количество записей изменилось, проверьте ограничения и сценарий демонстрации.";
            }
        }

        private IUnitOfWork CreateUnitOfWork()
        {
            return new EfUnitOfWork(_connectionString, _commandTimeoutSeconds);
        }

        private static string NormalizeDepartment(string department)
        {
            return string.IsNullOrWhiteSpace(department) ? "Общее" : department.Trim();
        }

        private static void FillServiceEntity(MedicalServiceEntity entity, MedicalService model, int departmentId)
        {
            entity.ShortName = model.ShortName ?? string.Empty;
            entity.FullName = model.FullName;
            entity.Description = model.Description;
            entity.Category = model.Category;
            entity.Rating = model.Rating;
            entity.Price = model.Price;
            entity.Duration = model.Duration;
            entity.Specialist = model.Specialist;
            entity.SpecialistImage = ParseImage(model.SpecialistImage);
            entity.DepartmentId = departmentId;
            entity.IsPopular = model.IsPopular;
            entity.HasDiscount = model.HasDiscount;
            entity.DiscountPercent = model.DiscountPercent;
            entity.PatientsCount = model.PatientsCount;
        }

        private static MedicalService MapService(MedicalServiceEntity x)
        {
            return new MedicalService
            {
                Id = x.Id,
                ShortName = x.ShortName,
                FullName = x.FullName,
                Description = x.Description,
                Category = x.Category,
                Rating = x.Rating,
                Price = x.Price,
                Duration = x.Duration,
                Specialist = x.Specialist,
                SpecialistImage = x.SpecialistImage == null ? null : Convert.ToBase64String(x.SpecialistImage),
                Department = x.Department?.Name,
                IsPopular = x.IsPopular,
                HasDiscount = x.HasDiscount,
                DiscountPercent = x.DiscountPercent,
                PatientsCount = x.PatientsCount,
                TimeSlots = x.ServiceTimeSlots?
                    .OrderBy(t => t.SlotTime)
                    .Select(t => new TimeSlot
                    {
                        Time = t.SlotTime,
                        IsAvailable = t.IsAvailable,
                        DoctorName = x.Specialist
                    })
                    .ToList() ?? new List<TimeSlot>()
            };
        }

        private static Appointment MapAppointment(AppointmentEntity x)
        {
            return new Appointment
            {
                Id = x.Id,
                ServiceId = x.ServiceId,
                ServiceName = x.Service?.ShortName,
                PatientName = x.PatientName,
                PatientPhone = x.PatientPhone,
                AppointmentDate = x.AppointmentDate,
                Time = x.AppointmentTime,
                Doctor = x.DoctorName,
                Price = x.Price,
                Status = x.Status,
                Comment = x.Comment,
                PatientRating = x.PatientRating
            };
        }

        private static byte[] ParseImage(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            try
            {
                return Convert.FromBase64String(source);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static bool IsSamePatient(string leftPhone, string leftName, string rightPhone, string rightName)
        {
            var lp = NormalizePhone(leftPhone);
            var rp = NormalizePhone(rightPhone);
            if (!string.IsNullOrWhiteSpace(lp) && !string.IsNullOrWhiteSpace(rp))
                return lp == rp;

            return NormalizeText(leftName) == NormalizeText(rightName);
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            return digits;
        }

        private static string NormalizeText(string text)
        {
            return (text ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
