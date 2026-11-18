# База даних: Dapper + SQL-код у БД

Бекенд працює з MS SQL Server через **Dapper** (`Microsoft.Data.SqlClient`).
Структура БД і серверна логіка (скалярні функції, збережені процедури, тригери)
зберігаються як SQL-файли в `backend/Database/` і **автоматично розгортаються** в БД
під час старту застосунку.

## Автоматичне розгортання

Клас `backend/Infrastructure/Database/SqlScriptDeployer.cs`:

1. Створює базу даних з рядка підключення `ConnectionStrings:Default`, якщо її ще немає.
2. Створює журнал `dbo.__SchemaVersions` (ім'я скрипта, SHA-256, дата застосування).
3. Виконує етапи в такому порядку (усередині етапу — за алфавітом імен файлів):

| Етап | Директорія | Режим |
|---|---|---|
| 1 | `Schema/` | **одноразово** (журналюється) |
| 2 | `Functions/` | **щоразу** — `CREATE OR ALTER` |
| 3 | `Procedures/` | **щоразу** — `CREATE OR ALTER` |
| 4 | `Triggers/` | **щоразу** — `CREATE OR ALTER` |
| 5 | `Seed/` | **одноразово** (журналюється) |

* Скрипт ділиться на пакети за рядками `GO`; кожен скрипт виконується в окремій транзакції
  (помилка — відкат і зупинка старту з назвою скрипта в повідомленні).
* Якщо вже застосований одноразовий скрипт змінено, у лог пишеться попередження,
  але повторно він **не** виконується — зміни схеми оформлюйте новим файлом
  (`Schema/002_....sql`).
* Функції, процедури й тригери перевизначаються при кожному старті, тож БД завжди
  відповідає коду в репозиторії.
* SQL-файли копіюються у вихідну директорію збірки/публікації (`CorrespondenceApi.csproj`),
  тому розгортання працює і з опублікованого застосунку.

Налаштування (`backend/appsettings.json`):

```json
"Database": {
  "DeployOnStartup": true,      // розгортати при старті
  "ScriptsPath": "Database",    // відносно директорії збірки або абсолютний шлях
  "CommandTimeoutSeconds": 120  // таймаут одного пакета
}
```

Лише розгорнути БД без запуску API: `dotnet run -- --deploy-db` (у директорії `backend`).

**Сумісність зі старою БД (EF Core).** `Schema/001_Tables.sql` створює кожну таблицю лише
якщо її немає, а seed-скрипти пропускаються для непорожніх таблиць. Тому БД, створена
попередньою версією на EF Core, підхоплюється без втрати даних — додаються лише функції,
процедури та тригери. Таблиця `__EFMigrationsHistory` більше не використовується.

## Об'єкти БД

### Скалярні функції (`Functions/`)

| Функція | Призначення | Де використовується |
|---|---|---|
| `fn_FormatRegistrationNumber(@Pattern, @Seq, @Year, @TypeCode, @DirCode)` | Формує реєстраційний номер за патерном (`{seq}`, `{seq:000}`, `{year}`, `{yy}`, `{type}`, `{dir}`) | `usp_RegistrationNumber_Next`, налаштування (попередній перегляд), seed |
| `fn_NextRegistrationSequence(@OrganizationId, @DocumentTypeId, @Year)` | Наступне значення лічильника номера | `usp_RegistrationNumber_Next` |
| `fn_IsRegistrationNumberTaken(@OrganizationId, @Year, @Number, @ExcludeDocumentId)` | Чи зайнятий номер в організації за рік | створення/редагування документа |
| `fn_CanReadDocument(@DocumentId, @ScopeLevel, @EmployeeId, @DepartmentId)` | Зона видимості документа: 3 — усі, 2 — свого підрозділу, 1 — задіяні, 0 — немає | списки документів, доручень, звіти |
| `fn_DocumentDisplayNumber(@DocumentId)` | «номер/рік» або «—» | список доручень, звіт прострочених |
| `fn_TaskDaysOverdue(@DueDate, @TaskStatusId)` | Кількість днів прострочення | звіт прострочених доручень |
| `fn_Percent(@Part, @Total)` | Цілий відсоток | звіти |

### Збережені процедури (`Procedures/`)

| Процедура | Призначення |
|---|---|
| `usp_RegistrationNumber_Next` | Наступний вільний реєстраційний номер (OUTPUT `@Number`, `@Sequence`) |
| `usp_Document_Create` | Реєстрація документа + реєстраційний запис; автонумерація під `sp_getapplock` (без колізій при паралельній реєстрації) |
| `usp_Document_Update` / `usp_Document_Delete` | Редагування / видалення з тенант-перевіркою |
| `usp_Task_Create` / `usp_Task_Update` / `usp_Task_Delete` | Доручення разом із резолюцією |
| `usp_Report_DocumentsByType`, `usp_Report_DocumentsByDirection`, `usp_Report_ExecutorDiscipline`, `usp_Report_OverdueTasks` | Звіти з урахуванням тенанта та зони видимості |
| `usp_User_GetAccess` | Користувач + ролі + пермішени (логін, `/me`, динамічні клейми на кожен запит) |
| `usp_User_Save`, `usp_Role_Save`, `usp_Role_Delete` | Користувачі та ролі з їх зв'язками в одній транзакції |
| `usp_Organization_Delete`, `usp_Department_Delete` | Видалення з перевіркою залежностей |

**Коди помилок.** Процедури повідомляють про бізнес-помилки через `THROW`:
`50400` → HTTP 400, `50404` → HTTP 404 (текст помилки повертається клієнту в `title`).
Також `GlobalExceptionHandler` перетворює порушення FK (547) і унікальності (2601/2627) на 400.

### Тригери (`Triggers/`)

| Тригер | Подія | Дія |
|---|---|---|
| `trg_Document_StatusHistory` | `Document` AFTER INSERT, UPDATE | Пише в `StatusHistory` створення документа та кожну зміну статусу. «Хто змінив» — з `SESSION_CONTEXT(N'EmployeeId')`, інакше автор документа |
| `trg_ExecutionTask_CompletedAt` | `ExecutionTask` AFTER INSERT, UPDATE | Проставляє `CompletedAt`, якщо доручення переведено у «Виконано» без дати |
| `trg_RegistrationRecord_SyncOrganization` | `RegistrationRecord` AFTER INSERT, UPDATE | Тримає `OrganizationId` реєстраційного запису рівним організації документа |

`Db.OpenAsync()` (`Infrastructure/Database/Db.cs`) на кожному підключенні викликає
`sp_set_session_context N'EmployeeId'` з працівником поточного користувача — так тригери
знають, хто виконує дію.

## Доступ до даних у C#

* `Db` — scoped-фабрика підключень і обгортки Dapper (`QueryAsync`, `ExecuteProcAsync`,
  `ScalarProcAsync`, `QueryProcAsync`, …).
* `SqlScope` — параметри тенант-ізоляції (`@Bypass`, `@TenantOrganizationId`) і зони
  видимості (`@ScopeLevel`, `@EmployeeId`, `@DepartmentId`) та готові SQL-умови
  `TenantFilter("x")`, `DocumentFilter("d")`. Замінюють глобальні query-фільтри EF.
* `DateOnlyTypeHandler` — підтримка `DateOnly` для колонок `date`.
* Хендлери MediatR (Vertical Slice, `Features/*.cs`) виконують параметризований SQL або
  викликають процедури; DTO-записи мапляться Dapper за конструктором (порядок і імена
  колонок у `SELECT` збігаються з параметрами запису).

## Як додати новий SQL-код

* **Нова функція / процедура / тригер** — файл у відповідній директорії з `CREATE OR ALTER`
  і `GO` в кінці. Розгорнеться при наступному старті.
* **Зміна структури** — новий одноразовий скрипт `Schema/00N_Опис.sql` (ідемпотентний:
  `IF COL_LENGTH(...) IS NULL ALTER TABLE ...`). Старі одноразові скрипти не редагуйте.
* **Нові початкові дані** — новий `Seed/00N_Опис.sql` з перевіркою `IF NOT EXISTS`.
* Якщо функція залежить від іншої функції, назвіть файли так, щоб залежність
  виконувалась раніше за алфавітом.

## Демо-облікові записи

Паролі зберігаються як PBKDF2-SHA256 (`Seed/003_RolesAndUsers.sql`, формат як у
`PasswordHasher`): `admin/admin123`, `registrar/registrar123`, `manager/manager123`,
`executor/executor123`, `orgadmin1/orgadmin123`, `orgadmin2/orgadmin123`.
