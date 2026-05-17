# Лабораторная №10: многоуровневая архитектура. Паттерны Repository и Unit of Work

## Цель лабораторной

В проекте из предыдущих лабораторных реализовать:
1. Паттерн **Repository**.
2. Паттерн **Unit of Work**.

---

## Структура рассказа: "задание -> сделано -> где"

## 1) Паттерн Repository

### Теория
`Repository` — это слой доступа к данным, который инкапсулирует работу с ORM (Entity Framework) и предоставляет бизнес-слою понятный API для чтения и изменения сущностей.

Плюсы:
- уменьшает связанность между бизнес-логикой и EF;
- упрощает сопровождение и тестирование;
- централизует запросы к конкретным сущностям.

### Что сделано в проекте

Реализован базовый generic-репозиторий и специализированные репозитории:

- Базовые контракты:
  - `Data/Repositories/IRepository.cs`
  - `Data/Repositories/Repository.cs`

- Репозитории предметной области:
  - `Data/Repositories/IMedicalServiceRepository.cs`
  - `Data/Repositories/MedicalServiceRepository.cs`
  - `Data/Repositories/IAppointmentRepository.cs`
  - `Data/Repositories/AppointmentRepository.cs`
  - `Data/Repositories/IDepartmentRepository.cs`
  - `Data/Repositories/DepartmentRepository.cs`

### Какие операции вынесены в Repository

- Общие операции:
  - `Query()`
  - `Add(...)`
  - `Remove(...)`

- Предметные операции:
  - услуги: `QueryWithDetails()`, `GetByIdWithSlotsAsync(...)`
  - записи: `QueryWithService()`, `GetByIdAsync(...)`
  - отделы: `GetByNameAsync(...)`

---

## 2) Паттерн Unit of Work

### Теория
`Unit of Work` объединяет работу с несколькими репозиториями в пределах одного `DbContext` и одного бизнес-сценария.

Плюсы:
- единая точка сохранения (`SaveChangesAsync`);
- единая транзакция на несколько операций;
- контроль целостности данных.

### Что сделано в проекте

Реализованы интерфейс и класс UoW:
- `Data/UnitOfWork/IUnitOfWork.cs`
- `Data/UnitOfWork/EfUnitOfWork.cs`

В `EfUnitOfWork`:
- создается и хранится один `MedicalCenterDbContext`;
- доступны репозитории через свойства:
  - `MedicalServices`
  - `Appointments`
  - `Departments`
  - `ServiceTimeSlots`
- реализованы:
  - `SaveChangesAsync()`
  - `BeginTransaction()`
  - `Dispose()`

---

## 3) Как эти паттерны используются в сервисном слое

Бизнес-логика работает через UoW и репозитории в `Services/MedicalServiceService.cs`.

Примеры:
- `AddOrUpdateServiceAsync(...)`
- `DeleteServiceAsync(...)`
- `CreateAppointmentAsync(...)`
- `UpdateAppointmentStatusAsync(...)`
- `DeleteAppointmentAsync(...)`

Типовой шаблон:
1. `using (var uow = CreateUnitOfWork())`
2. `using (var tx = uow.BeginTransaction())`
3. операции через репозитории
4. `await uow.SaveChangesAsync()`
5. `tx.Commit()` или `tx.Rollback()`

Это показывает связку двух паттернов:
- Repository отвечает за доступ к конкретной сущности;
- Unit of Work управляет общим контекстом и транзакцией.

---

## 4) Что показать в IDE на защите

1. `Data/Repositories/IRepository.cs` и `Repository.cs`  
   Показать базовую идею репозитория.

2. `Data/Repositories/*Repository.cs`  
   Показать предметные методы для услуг, записей и отделов.

3. `Data/UnitOfWork/IUnitOfWork.cs` и `EfUnitOfWork.cs`  
   Показать единый контекст, `SaveChangesAsync`, `BeginTransaction`.

4. `Services/MedicalServiceService.cs`  
   Показать, как сервис использует UoW + Repository в реальных CRUD-операциях.

---

## 5) Короткий готовый текст ответа (1-2 минуты)

В 10 лабораторной я внедрил паттерны Repository и Unit of Work в существующий проект.

Для Repository сделан базовый generic-репозиторий с операциями `Query`, `Add`, `Remove`, а также специализированные репозитории для услуг, записей и отделов с предметными методами выборки.

Для Unit of Work реализован `EfUnitOfWork`, который управляет единым `DbContext`, предоставляет доступ к репозиториям и содержит `SaveChangesAsync` и `BeginTransaction`.

В сервисном слое все операции выполняются через UoW и репозитории: чтение, добавление, обновление и удаление услуг и записей. Для изменяющих операций используется транзакция с `Commit/Rollback`, что обеспечивает целостность данных.

Итог: доступ к данным стал многослойным, бизнес-логика отделена от деталей EF, а операции с несколькими сущностями выполняются атомарно.

---

## 6) Частые вопросы преподавателя и ответы

### Почему нужен и Repository, и Unit of Work, а не что-то одно?
Repository решает задачу доступа к конкретным наборам данных, а Unit of Work — задачу координации нескольких репозиториев в одной операции и транзакции.

### Где видно, что используется один контекст на операцию?
В `EfUnitOfWork`: репозитории создаются на одном `_context`, который живет в рамках `using` в сервисном методе.

### Где фиксируется транзакция?
В сервисе (`MedicalServiceService`) через `BeginTransaction()`, `Commit()`, `Rollback()`.

### Что поменялось архитектурно после внедрения?
UI и ViewModel не зависят от EF-напрямую; слой данных выделен в `Repositories` и `UnitOfWork`, сервисный слой стал точкой бизнес-правил.
