# Сценарий показа в IDE (лаба 9)

## Порядок открытия файлов

1. `packages.config`  
   - Показать пакет `EntityFramework` (подключение через NuGet).

2. `LAB45.csproj`  
   - Показать ссылки `EntityFramework` и `EntityFramework.SqlServer`.

3. `App.config`  
   - Показать `connectionStrings` с `MedicalCenterDb`.
   - Показать `appSettings` с `DbCommandTimeoutSeconds`.
   - Показать секцию `entityFramework` с SQL-провайдером.

4. `Data/MedicalCenterDbContext.cs`  
   - Показать `DbSet` сущностей.
   - Показать `OnModelCreating` и связи `HasRequired(...).WithMany(...).HasForeignKey(...)`.

5. `Data/Entities` (по очереди)  
   - `DepartmentEntity.cs`
   - `MedicalServiceEntity.cs`
   - `ServiceTimeSlotEntity.cs`
   - `AppointmentEntity.cs`
   - `AuditLogEntity.cs`
   - Коротко: это Code First-модель предметной области.

6. `Services/MedicalServiceService.cs`  
   - Показать CRUD-методы для услуг и записей.
   - Показать LINQ-запросы: `Where`, `OrderBy`, `ThenBy`, `Contains`.
   - Показать async: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`.
   - Показать транзакции: `BeginTransaction`, `Commit`, `Rollback`.

7. `Data/UnitOfWork/EfUnitOfWork.cs`  
   - Коротко показать, где создается транзакция и как сохраняются изменения через UoW.

8. `Lab9_EF_DatabaseFirst_ModelFirst.md`  
   - Показать, что DB First и Model First продемонстрированы отдельной инструкцией.

## Финальная фраза на защите

В проекте основная рабочая реализация выполнена через Code First и LINQ to Entities, а альтернативные подходы Database First и Model First показаны и описаны как демонстрационные шаги в отдельном файле.
