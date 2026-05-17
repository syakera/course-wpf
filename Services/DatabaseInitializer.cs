using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using MedicalCenter.Data;
using MedicalCenter.Data.Entities;
using MedicalCenter.Data.Migrations;
using MedicalCenter.Models;

namespace MedicalCenter.Services
{
    public static class DatabaseInitializer
    {
        public static void EnsureDatabaseCreated(string connectionString, int timeoutSeconds)
        {
            using (var context = new MedicalCenterDbContext(connectionString))
            {
                context.Database.CommandTimeout = timeoutSeconds;
                bool hasMigrationHistory = TableExists(context, "__MigrationHistory");
                bool hasDomainTables = TableExists(context, "Appointments")
                                       || TableExists(context, "MedicalServices")
                                       || TableExists(context, "Users");

                if (hasDomainTables && !hasMigrationHistory)
                {
                    // Legacy DB created before migration history was enabled:
                    // keep existing schema and avoid first auto-migration conflict.
                    Database.SetInitializer<MedicalCenterDbContext>(null);
                    context.Database.Initialize(false);
                }
                else
                {
                    Database.SetInitializer(new MigrateDatabaseToLatestVersion<MedicalCenterDbContext, Configuration>());
                    context.Database.Initialize(false);
                }

                SeedDataIfNeeded(context);
            }
        }

        private static bool TableExists(MedicalCenterDbContext context, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            const string sql = @"
SELECT COUNT(*) 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = @p0";

            try
            {
                int count = context.Database.SqlQuery<int>(sql, tableName).FirstOrDefault();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void SeedDataIfNeeded(MedicalCenterDbContext context)
        {
            EnsureAdminIfMissing(context);

            foreach (var service in BuildCatalog())
            {
                bool exists = context.MedicalServices.Any(x => x.ShortName == service.ShortName);
                if (exists)
                    continue;

                string departmentName = NormalizeDepartment(service.Department);
                var department = context.Departments.FirstOrDefault(x => x.Name == departmentName);
                if (department == null)
                {
                    department = new DepartmentEntity { Name = departmentName };
                    context.Departments.Add(department);
                }

                string doctorName = string.IsNullOrWhiteSpace(service.Specialist)
                    ? "Без имени"
                    : service.Specialist.Trim();
                var doctor = context.Doctors.FirstOrDefault(x => x.FullName == doctorName);
                if (doctor == null)
                {
                    var doctorUser = CreateDoctorUser(context, doctorName);
                    doctor = new DoctorEntity
                    {
                        FullName = doctorName,
                        Specialization = departmentName,
                        ExperienceYears = 5,
                        Description = null,
                        IsActive = true,
                        User = doctorUser
                    };
                    context.Doctors.Add(doctor);
                }

                var serviceEntity = new MedicalServiceEntity
                {
                    ShortName = service.ShortName ?? string.Empty,
                    FullName = service.FullName,
                    Description = service.Description,
                    Category = service.Category,
                    Rating = service.Rating,
                    Price = service.Price,
                    Duration = service.Duration,
                    Specialist = service.Specialist,
                    SpecialistImage = ParseImage(service.SpecialistImage),
                    Department = department,
                    IsPopular = service.IsPopular,
                    HasDiscount = service.HasDiscount,
                    DiscountPercent = service.DiscountPercent,
                    PatientsCount = service.PatientsCount
                };

                foreach (var slot in service.TimeSlots.Where(x => !string.IsNullOrWhiteSpace(x.Time)))
                {
                    serviceEntity.ServiceTimeSlots.Add(new ServiceTimeSlotEntity
                    {
                        SlotTime = slot.Time.Trim(),
                        IsAvailable = slot.IsAvailable
                    });
                }

                serviceEntity.DoctorServices.Add(new DoctorServiceEntity
                {
                    Doctor = doctor
                });
                context.MedicalServices.Add(serviceEntity);
            }

            context.SaveChanges();
        }

        private static void EnsureAdminIfMissing(MedicalCenterDbContext context)
        {
            if (context.Users.Any(x => x.Username == "admin"))
                return;

            context.Users.Add(new UserEntity
            {
                Username = "admin",
                Password = "admin",
                Role = 0,
                DisplayName = "Администратор"
            });
        }

        private static UserEntity CreateDoctorUser(MedicalCenterDbContext context, string displayName)
        {
            string loginBase = BuildLoginFromName(displayName);
            string login = loginBase;
            int suffix = 1;

            while (context.Users.Any(x => x.Username == login))
            {
                suffix++;
                login = loginBase + suffix;
            }

            return new UserEntity
            {
                Username = login,
                Password = "doctor",
                Role = 1,
                DisplayName = displayName
            };
        }

        private static List<MedicalService> BuildCatalog()
        {
            return new List<MedicalService>
            {
                MakeService("Аллерголог",   "Аллергологические пробы и консультация",
                    "Диагностика аллергических реакций, кожные тесты, иммунотерапия",
                    "Консультация", 4.7, 80m, 60, "Белова Татьяна Олеговна",          "Аллергология"),
                MakeService("Анализ крови", "Лабораторная диагностика",
                    "Общий и биохимический анализ, гормоны, онкомаркеры, витамины",
                    "Диагностика",  4.6, 55m, 15, "Волкова Елена Сергеевна",         "Лаборатория"),
                MakeService("Гастроскопия", "Эндоскопическое исследование ЖКТ",
                    "Эндоскопическое исследование желудка и двенадцатиперстной кишки",
                    "Диагностика",  4.5, 120m, 30, "Давыдова Ирина Николаевна",       "Гастроэнтерология"),
                MakeService("Гинеколог",    "Профилактический осмотр гинеколога",
                    "Профилактический осмотр, УЗИ органов малого таза, онкоцитология",
                    "Консультация", 4.8, 65m, 40, "Жукова Светлана Олеговна",         "Гинекология"),
                MakeService("Дерматолог",   "Консультация дерматолога",
                    "Диагностика и лечение кожных заболеваний, удаление новообразований",
                    "Консультация", 4.7, 55m, 30, "Захарова Ольга Михайловна",        "Дерматология"),
                MakeService("Кардиолог",    "Консультация кардиолога",
                    "Проверка сердечно-сосудистой системы, ЭКГ, рекомендации",
                    "Консультация", 4.9, 70m, 45, "Иванова Мария Петровна",           "Кардиология",
                    hasDiscount: true, discount: 10),
                MakeService("ЛОР",          "Консультация оториноларинголога",
                    "Лечение заболеваний уха, горла и носа, промывания",
                    "Консультация", 4.6, 60m, 30, "Козлова Анна Юрьевна",             "Оториноларингология"),
                MakeService("Мануальная терапия", "Мануальная терапия позвоночника",
                    "Коррекция позвоночника, лечение остеохондроза и грыж",
                    "Процедуры",    4.5, 90m, 60, "Кузнецов Владимир Александрович",  "Мануальная терапия"),
                MakeService("МРТ",          "Магнитно-резонансная томография",
                    "Современная диагностика позвоночника, суставов, внутренних органов",
                    "Диагностика",  4.7, 150m, 60, "Лебедев Игорь Сергеевич",         "Диагностика"),
                MakeService("Невролог",     "Консультация невролога",
                    "Диагностика и лечение заболеваний нервной системы",
                    "Консультация", 4.7, 70m, 40, "Морозова Светлана Ивановна",       "Неврология"),
                MakeService("Ортопед",      "Консультация ортопеда-травматолога",
                    "Диагностика и лечение заболеваний опорно-двигательного аппарата",
                    "Консультация", 4.6, 65m, 30, "Новиков Андрей Владимирович",      "Травматология"),
                MakeService("Офтальмолог",  "Консультация офтальмолога",
                    "Проверка зрения, подбор очков, диагностика заболеваний глаз",
                    "Консультация", 4.7, 60m, 30, "Орлов Дмитрий Николаевич",         "Офтальмология"),
                MakeService("Педиатр",      "Консультация педиатра",
                    "Осмотр ребёнка, профилактические рекомендации, прививки",
                    "Консультация", 4.9, 55m, 30, "Павлова Наталья Александровна",    "Педиатрия",
                    isPopular: true),
                MakeService("Психотерапия", "Сеанс психотерапии",
                    "Индивидуальные консультации психотерапевта",
                    "Процедуры",    4.6, 85m, 60, "Романова Алина Юрьевна",           "Психотерапия"),
                MakeService("Стоматолог",   "Терапевтическая стоматология",
                    "Лечение кариеса, чистка, профилактика стоматологических заболеваний",
                    "Стоматология", 4.8, 90m, 45, "Соколов Михаил Игоревич",          "Стоматология",
                    isPopular: true),
                MakeService("Терапевт",     "Консультация врача-терапевта",
                    "Первичная консультация, сбор анамнеза, назначение лечения",
                    "Консультация", 4.8, 50m, 30, "Петрова Анна Сергеевна",           "Терапия",
                    isPopular: true),
                MakeService("УЗИ брюшной полости", "УЗИ органов брюшной полости",
                    "Ультразвуковое исследование печени, поджелудочной, почек",
                    "Диагностика",  4.6, 75m, 30, "Тихонова Юлия Олеговна",           "Диагностика"),
                MakeService("УЗИ сердца",   "Эхокардиография",
                    "Ультразвуковое исследование сердца и крупных сосудов",
                    "Диагностика",  4.7, 80m, 30, "Иванова Мария Петровна",           "Кардиология"),
                MakeService("Уролог",       "Консультация уролога",
                    "Диагностика и лечение заболеваний мочеполовой системы",
                    "Консультация", 4.5, 65m, 30, "Фёдоров Сергей Михайлович",        "Урология"),
                MakeService("Хирург",       "Консультация хирурга",
                    "Осмотр, перевязки, малые амбулаторные операции",
                    "Консультация", 4.7, 70m, 30, "Чернов Виктор Алексеевич",         "Хирургия"),
                MakeService("ЭКГ",          "Электрокардиография",
                    "Регистрация ЭКГ в покое, оценка ритма сердца",
                    "Диагностика",  4.6, 45m, 15, "Иванова Мария Петровна",           "Кардиология"),
                MakeService("Эндокринолог", "Консультация эндокринолога",
                    "Диагностика и лечение заболеваний щитовидной железы и диабета",
                    "Консультация", 4.7, 70m, 45, "Шевченко Ольга Викторовна",        "Эндокринология"),
            };
        }

        private static MedicalService MakeService(
            string shortName, string fullName, string description, string category,
            double rating, decimal price, int duration, string specialist, string department,
            bool isPopular = false, bool hasDiscount = false, double discount = 0)
        {
            return new MedicalService
            {
                ShortName = shortName,
                FullName = fullName,
                Description = description,
                Category = category,
                Rating = rating,
                Price = price,
                Duration = duration,
                Specialist = specialist,
                Department = department,
                IsPopular = isPopular,
                HasDiscount = hasDiscount,
                DiscountPercent = discount,
                PatientsCount = 0,
                TimeSlots = BuildDefaultTimeSlots()
            };
        }

        private static string BuildLoginFromName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "doctor";

            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "doctor" : parts[0];
        }

        private static List<TimeSlot> BuildDefaultTimeSlots()
        {
            return new List<TimeSlot>
            {
                new TimeSlot { Time = "09:00", IsAvailable = true },
                new TimeSlot { Time = "10:00", IsAvailable = true },
                new TimeSlot { Time = "11:00", IsAvailable = true },
                new TimeSlot { Time = "12:00", IsAvailable = true },
                new TimeSlot { Time = "13:00", IsAvailable = true },
                new TimeSlot { Time = "14:00", IsAvailable = true },
                new TimeSlot { Time = "15:00", IsAvailable = true },
                new TimeSlot { Time = "16:00", IsAvailable = true },
                new TimeSlot { Time = "17:00", IsAvailable = true }
            };
        }

        private static string NormalizeDepartment(string department)
        {
            return string.IsNullOrWhiteSpace(department) ? "Общее" : department.Trim();
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
    }
}
