# План рассказа по лабораторной №9 (Entity Framework)

## Формат защиты: "Задание -> Сделано -> Где в проекте"

### 1) Добавить Entity Framework к проекту
- **Задание:** подключить Entity Framework через NuGet.
- **Сделано:** в проект добавлен пакет `EntityFramework` версии `6.5.2`.
- **Где смотреть:**
  - `packages.config` — пакет `EntityFramework`.
  - `LAB45.csproj` — ссылки `EntityFramework` и `EntityFramework.SqlServer`.
  - `App.config` — секция `entityFramework` и SQL-провайдер.

### 2) Сохранить строку подключения и параметры в конфиге
- **Задание:** вынести подключение к БД и параметры приложения в конфигурационный файл.
- **Сделано:**
  - строка подключения хранится как `MedicalCenterDb` в `connectionStrings`;
  - параметр таймаута хранится как `DbCommandTimeoutSeconds` в `appSettings`;
  - в коде это читается через `ConfigurationManager`.
- **Где смотреть:**
  - `App.config` (`connectionStrings`, `appSettings`);
  - `Services/MedicalServiceService.cs` (конструктор сервиса).

### 3) Code First: сущности и связи
- **Задание:** создать сущности и связи через подход Code First.
- **Сделано:**
  - сущности: `DepartmentEntity`, `MedicalServiceEntity`, `ServiceTimeSlotEntity`, `AppointmentEntity`, `AuditLogEntity`;
  - в `MedicalCenterDbContext` добавлены `DbSet` для этих сущностей;
  - связи настроены через Fluent API как "один-ко-многим":
    - `Department -> MedicalServices`,
    - `MedicalService -> ServiceTimeSlots`,
    - `MedicalService -> Appointments`.
- **Где смотреть:**
  - `Data/Entities/*.cs` — классы сущностей;
  - `Data/MedicalCenterDbContext.cs` — `DbSet` и `OnModelCreating`.

### 4) CRUD-операции
- **Задание:** реализовать CRUD (добавление, просмотр, редактирование, удаление).
- **Сделано:**
  - для услуг:
    - `AddOrUpdateServiceAsync` (Create/Update),
    - `LoadServicesAsync` (Read),
    - `DeleteServiceAsync` (Delete);
  - для записей:
    - `CreateAppointmentAsync` (Create),
    - `GetAppointmentsAsync` (Read),
    - `UpdateAppointmentStatusAsync` (Update),
    - `DeleteAppointmentAsync` (Delete).
- **Где смотреть:**
  - `Services/MedicalServiceService.cs`;
  - `Data/Repositories/*` и `Data/UnitOfWork/*` (слой доступа к данным).

### 5) Сортировка, фильтрация, поиск, LINQ to Entities, async, транзакции
- **Задание:** сделать выборки по критериям, поиск по полям, асинхронность и транзакции.
- **Сделано:**
  - сортировка и фильтрация:
    - `OrderBy`, `ThenBy`, `Where` в методах выборки;
  - поиск по нескольким полям:
    - `Contains` по `ShortName`, `FullName`, `Specialist`, `Description`;
  - асинхронность:
    - `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`;
  - транзакции:
    - `using (var tx = uow.BeginTransaction())` + `Commit/Rollback`.
- **Где смотреть:**
  - `Services/MedicalServiceService.cs`;
  - `Data/UnitOfWork/EfUnitOfWork.cs`.

### 6) Демонстрация DB First и Model First
- **Задание:** показать генерацию EDM и сущностей из БД и из модели.
- **Сделано:** в проекте подготовлена отдельная инструкция с шагами демонстрации:
  - Database First: генерация `.edmx` из существующей БД;
  - Model First: генерация SQL из `.edmx` модели.
- **Где смотреть:**
  - `Lab9_EF_DatabaseFirst_ModelFirst.md`.
- **Важно проговорить на защите:** рабочий код проекта — Code First, а DB First/Model First оформлены как демонстрационные шаги.

---

## Готовый текст для устного рассказа (5-7 минут)

В этой лабораторной работе нужно было реализовать доступ к данным через Entity Framework.

По первому пункту я подключил EF6 через NuGet. Это видно в `packages.config`, где добавлен пакет `EntityFramework`, и в `LAB45.csproj`, где подключены сборки `EntityFramework` и `EntityFramework.SqlServer`.

По второму пункту я вынес настройки в конфигурацию. В `App.config` хранится строка подключения `MedicalCenterDb`, а также параметр `DbCommandTimeoutSeconds`. В `MedicalServiceService` эти значения читаются через `ConfigurationManager`, поэтому код не захардкожен под конкретное окружение.

По третьему пункту использован подход Code First. Сущности реализованы отдельными классами в `Data/Entities`: отделы, услуги, временные слоты, записи и аудит. Контекст `MedicalCenterDbContext` содержит соответствующие `DbSet`. Связи настроены во Fluent API в `OnModelCreating`: отдел к услугам один-ко-многим, услуга к слотам один-ко-многим, услуга к записям один-ко-многим. То есть структура модели задается кодом.

По четвертому пункту реализован полный CRUD. Для услуг есть методы добавления и редактирования, чтения и удаления. Для записей есть создание, получение, изменение статуса и удаление. Основная бизнес-логика находится в `MedicalServiceService`, а доступ к данным проходит через слой `Repository` и `UnitOfWork`.

По пятому пункту реализованы выборки через LINQ. Используются `Where`, `OrderBy`, `ThenBy`, а поиск выполняется по нескольким полям через `Contains`. Также показана асинхронная работа через `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, что важно для отзывчивости интерфейса. Для операций изменения данных применяются транзакции через `BeginTransaction`, `Commit` и `Rollback`, чтобы изменения выполнялись атомарно.

По шестому пункту подготовлена демонстрация Database First и Model First. Для этого есть отдельный файл `Lab9_EF_DatabaseFirst_ModelFirst.md` с пошаговой процедурой: как сгенерировать EDMX из существующей базы и как сгенерировать базу из модели.

Итог: все требования лабораторной выполнены. Рабочая часть приложения построена на Code First, а DB First и Model First подготовлены как демонстрация альтернативных подходов в рамках задания.

---

## Короткие ответы на возможные вопросы

- **Почему используется Code First?**  
  Потому что модель предметной области задается напрямую в C# и удобно поддерживается вместе с кодом приложения.

- **Где именно асинхронность?**  
  В сервисном слое: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, а также асинхронные методы `*Async`.

- **Где именно транзакции?**  
  В изменяющих операциях сервиса через `uow.BeginTransaction()` и `Commit/Rollback`.

- **Как показаны DB First и Model First?**  
  Через пошаговую инструкцию в `Lab9_EF_DatabaseFirst_ModelFirst.md`.
