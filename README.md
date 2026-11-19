# Інформаційна система «Відділ реєстрації кореспонденції»

Рішення (solution) на базі **.NET Aspire**:

| Проєкт | Технології |
|---|---|
| **AppHost** (`AppHost/`) | .NET Aspire — оркестрація: запускає бекенд і фронтенд разом, дашборд, телеметрія, service discovery. |
| **ServiceDefaults** (`ServiceDefaults/`) | Спільні налаштування Aspire: OpenTelemetry, health checks, resilience. |
| **backend** (`backend/CorrespondenceApi.csproj`) | ASP.NET Core 10, **Dapper** + Microsoft.Data.SqlClient (MS SQL Server LocalDB), MediatR, FluentValidation, JWT. Архітектура — Vertical Slice. Бізнес-логіка БД — скалярні й табличні функції, представлення, табличні типи, збережені процедури та тригери (`backend/Database/`). |
| **frontend** (`frontend/`) | React 19 + TypeScript + Vite + Ant Design. Запускається через Aspire (`AddViteApp`). |

## Запуск (один крок)

```bash
cd AppHost
dotnet run            # підніме бекенд + фронтенд, відкриє Aspire-дашборд
```

У Visual Studio — відкрити `CorrespondenceSystem.slnx`, зробити **AppHost** стартовим
проєктом і натиснути F5. Дашборд Aspire покаже ресурси `backend` і `frontend` з їх
URL, логами та трасуванням. Фронтенд звертається до `/api/...`, який проксі Vite
перенаправляє на бекенд (один origin — без CORS).

> Фронтенд більше не потребує `.esproj` — його запускає Aspire. БД (LocalDB), її структура,
> функції, процедури, тригери і тестові дані розгортаються автоматично під час старту бекенда
> з директорії `backend/Database/` (див. [docs/DATABASE.md](docs/DATABASE.md)).

### Файлове сховище вкладень (MinIO)
Вкладення документів зберігаються в S3-сумісному **MinIO**, який AppHost запускає в контейнері
(потрібен запущений **Docker**). Використовується спільнотна збірка `pgsty/minio` — офіційні образи
`minio/minio` більше не публікуються. Дані — в томі `correspondence-minio-data`, бакет `correspondence`
створюється автоматично. У БД (`dbo.Attachment`) зберігаються лише метадані та ключ об'єкта.
Налаштування — секція `FileStorage` в `appsettings.json` (`Bucket`, `MaxFileSizeMb`, `AllowedExtensions`);
рядок підключення `ConnectionStrings:minio` (`Endpoint=…;AccessKey=…;SecretKey=…`) під Aspire
передається автоматично, для окремого запуску бекенда його треба задати вручну.

### Окремий запуск (без Aspire)
`backend`: `dotnet run` (→ `http://localhost:5080`, Swagger `/swagger`).
`frontend`: `npm install && npm run dev` (→ `http://localhost:5180`, проксі на :5080).

### Лише розгортання БД
```bash
cd backend
dotnet run -- --deploy-db   # створить/оновить БД і завершить роботу
```

## База даних

Доступ до даних — **Dapper** (EF Core і міграції EF прибрано). Увесь SQL-код зберігається в
проєкті й автоматично розгортається в БД при старті:

```
backend/Database/
├── Schema/         001_Tables.sql          — таблиці, ключі, індекси (одноразово)
├── Types/          *.sql                   — табличні типи (User-Defined Table Types)
├── TableFunctions/ fn_*.sql                — inline-табличні функції (щоразу, CREATE OR ALTER)
├── Functions/      fn_*.sql                — скалярні функції (щоразу, CREATE OR ALTER)
├── Views/          vw_*.sql                — представлення (щоразу, CREATE OR ALTER)
├── Procedures/     usp_*.sql               — збережені процедури (щоразу, CREATE OR ALTER)
├── Triggers/       trg_*.sql               — тригери (щоразу, CREATE OR ALTER)
└── Seed/           001..003_*.sql          — довідники, демо-дані, ролі й користувачі (одноразово)
```

Деплоєр (`Infrastructure/Database/SqlScriptDeployer.cs`) створює БД, якщо її немає,
веде журнал одноразових скриптів у `dbo.__SchemaVersions` і ділить скрипти на пакети за `GO`.
Налаштування — секція `Database` в `appsettings.json` (`DeployOnStartup`, `ScriptsPath`,
`CommandTimeoutSeconds`). Повний опис об'єктів БД і правила додавання нового SQL-коду —
у [docs/DATABASE.md](docs/DATABASE.md).

## Демо-облікові записи

| Логін | Пароль | Роль |
|---|---|---|
| admin | admin123 | Адміністратор |
| registrar | registrar123 | Реєстратор |
| manager | manager123 | Керівник |
| executor | executor123 | Виконавець |

## Структура бекенда (Vertical Slice)

Кожна фіча — окремий каталог у `backend/Features/` з неймспейсом за назвою каталогу:

```
Features/Documents/                      namespace CorrespondenceApi.Features.Documents
├── DocumentsEndpoints.cs                — основний файл фічі: маршрути /api/documents
├── GetDocuments.cs                      — слайс: Query + Handler (один файл — один слайс)
├── GetDocumentById.cs, CreateDocument.cs, UpdateDocument.cs, DeleteDocument.cs, …
├── DocumentInputValidator.cs            — спільна валідація кількох слайсів
└── Dtos/                                namespace CorrespondenceApi.Features.Documents.Dtos
    ├── DocumentInput.cs                 — вхідні моделі (*Input, *Request)
    ├── DocumentListItem.cs              — моделі відповіді (*Dto, *Item, *Row, *Option)
    └── DocumentDetail.cs, DocumentTaskItem.cs, NextNumberResult.cs
```

Правила:
* один публічний DTO — один файл у `Dtos/`, з конкретною назвою (`RoleDto`, `UserOptionsDto`,
  `OverdueTaskRow`), а не загальною `Dto`;
* слайс — статичний клас (`GetRoles`, `SaveUser`) з вкладеними `Query`/`Command`,
  `Validator` і `Handler`;
* проміжні рядки, потрібні лише одному запиту (напр. `RoleRow` для мапінгу Dapper), лишаються
  `private` усередині слайсу.

## Авторизація

Рольова модель із **динамічними пермішенами** (зберігаються в БД, керуються через
розділ «Ролі та доступ», застосовуються пер-запит). Зони видимості кореспонденції:
вся / свого підрозділу / лише задіяні документи.
