# Інформаційна система «Відділ реєстрації кореспонденції»

Рішення (solution) на базі **.NET Aspire**:

| Проєкт | Технології |
|---|---|
| **AppHost** (`AppHost/`) | .NET Aspire — оркестрація: запускає сервіс даних, бекенд, фронтенд і MinIO разом, дашборд, телеметрія, service discovery. |
| **ServiceDefaults** (`ServiceDefaults/`) | Спільні налаштування Aspire: OpenTelemetry, health checks, resilience. |
| **dataservice** (`dataservice/CorrespondenceSystem.DataService.csproj`) | **Сервіс даних** — єдиний компонент із доступом до БД: ASP.NET Core, **Dapper** + Microsoft.Data.SqlClient (MS SQL Server LocalDB), MediatR. Хендлери слайсів із SQL-кодом, внутрішній Data API (`POST /rpc/{операція}`), розгортання SQL-коду БД (`dataservice/Database/`: функції, представлення, табличні типи, процедури, тригери). Назовні не публікується. |
| **backend** (`backend/CorrespondenceApi.csproj`) | Публічний API-шлюз: ASP.NET Core, MediatR, FluentValidation, JWT, MinIO/S3. Автентифікація й авторизація, валідація запитів, файли вкладень; дані читає й пише через сервіс даних (підключення до БД не має). |
| **Contracts** (`contracts/CorrespondenceSystem.Contracts.csproj`) | Спільні контракти backend ↔ dataservice: запити MediatR (`Query`/`Command`), DTO, валідатори, каталог пермішенів, протокол Data API. |
| **frontend** (`frontend/`) | React 19 + TypeScript + Vite + Ant Design. Запускається через Aspire (`AddViteApp`). |

## Запуск (один крок)

```bash
cd AppHost
dotnet run            # підніме сервіс даних + бекенд + фронтенд + MinIO, відкриє Aspire-дашборд
```

У Visual Studio — відкрити `CorrespondenceSystem.slnx`, зробити **AppHost** стартовим
проєктом і натиснути F5. Дашборд Aspire покаже ресурси `dataservice`, `backend` і `frontend` з їх
URL, логами та трасуванням. Фронтенд звертається до `/api/...`, який проксі Vite
перенаправляє на бекенд (один origin — без CORS).

> Фронтенд більше не потребує `.esproj` — його запускає Aspire. БД (LocalDB), її структура,
> функції, процедури, тригери і тестові дані розгортаються автоматично під час старту сервісу даних
> з директорії `dataservice/Database/` (див. [docs/DATABASE.md](docs/DATABASE.md)).

### Сервіс даних (Data API)
```
frontend ──/api──▶ backend (JWT, пермішени, MinIO) ──POST /rpc/{операція}──▶ dataservice ──▶ MS SQL Server
```
* Кожен запит MediatR із `Contracts` — операція Data API; ім'я — повне ім'я типу без префікса
  збірки, напр. `Documents.GetDocuments.Query`. Тіло — JSON запиту, відповідь — JSON результату
  (204 для команд без результату). У backend для кожного такого запиту автоматично зареєстровано
  хендлер-пересилач (`Infrastructure/DataService`), тож ендпоінти, як і раніше, роблять `ISender.Send(...)`.
* Виклики захищені спільним секретом (`X-Data-Api-Key`, налаштування `DataApi:ApiKey`; під Aspire —
  згенерований параметр `data-api-key` у user secrets AppHost). Backend передає Id користувача з JWT
  (`X-User-Id`); сервіс даних сам читає з БД його пермішени, організацію й підрозділ і застосовує
  тенант-ізоляцію та зону видимості документів.
* Права перевіряються **і в сервісі даних**, а не лише на ендпоінтах backend: кожен запит у `Contracts`
  позначено `[RequirePermission(...)]` (або `[AllowAnonymousOperation]` для перевірки логіна);
  `PermissionBehavior` відхиляє виклик без потрібного права (401/403). Запит без атрибута не пройде —
  сервіс не стартує, доки всі операції не позначено.
* Ключ підпису JWT (`Jwt:Key`, мінімум 32 байти) у репозиторії не зберігається: під Aspire це
  згенерований параметр `jwt-key` у user secrets AppHost, для окремого запуску — `appsettings.Development.json`.
  Без ключа backend не стартує.
* Помилки сервісу даних (400 — валідація/бізнес-правила, 401, 403, 404) backend повертає клієнту без змін;
  недоступність сервісу — 503, внутрішня помилка — 502. POST-виклики не повторюються автоматично.

### Файлове сховище вкладень (MinIO)
Вкладення документів зберігаються в S3-сумісному **MinIO**, який AppHost запускає в контейнері
(потрібен запущений **Docker**). Використовується спільнотна збірка `pgsty/minio` — офіційні образи
`minio/minio` більше не публікуються. Дані — в томі `correspondence-minio-data`, бакет `correspondence`
створюється автоматично. У БД (`dbo.Attachment`) зберігаються лише метадані та ключ об'єкта.
Налаштування — секція `FileStorage` в `appsettings.json` (`Bucket`, `MaxFileSizeMb`, `AllowedExtensions`);
рядок підключення `ConnectionStrings:minio` (`Endpoint=…;AccessKey=…;SecretKey=…`) під Aspire
передається автоматично, для окремого запуску бекенда його треба задати вручну.

### Окремий запуск (без Aspire)
`dataservice`: `dotnet run` (→ `http://localhost:5090`, лише внутрішній `/rpc`).
`backend`: `dotnet run` (→ `http://localhost:5080`, Swagger `/swagger`); адреса сервісу даних —
`Services:dataservice` в `appsettings.Development.json`, спільний dev-ключ — `DataApi:ApiKey` в обох проєктах.
`frontend`: `npm install && npm run dev` (→ `http://localhost:5180`, проксі на :5080).

### Лише розгортання БД
```bash
cd dataservice
dotnet run -- --deploy-db   # створить/оновить БД і завершить роботу
```

## База даних

Доступ до даних — **Dapper** (EF Core і міграції EF прибрано). Увесь SQL-код зберігається в
проєкті й автоматично розгортається в БД при старті:

```
dataservice/Database/
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

## Структура (Vertical Slice у трьох проєктах)

Кожна фіча — каталог з однаковою назвою в трьох проєктах:

```
contracts/Documents/                     namespace CorrespondenceSystem.Contracts.Documents
├── GetDocuments.cs                      — слайс: Query/Command + Validator (статичний клас)
├── GetDocumentById.cs, CreateDocument.cs, UpdateDocument.cs, DeleteDocumentRecord.cs, …
├── DocumentInputValidator.cs            — спільна валідація кількох слайсів
└── Dtos/                                namespace CorrespondenceSystem.Contracts.Documents.Dtos
    ├── DocumentInput.cs                 — вхідні моделі (*Input, *Request)
    └── DocumentListItem.cs, DocumentDetail.cs, …  — моделі відповіді (*Dto, *Item, *Row, *Option)

dataservice/Features/Documents/          namespace CorrespondenceSystem.DataService.Features.Documents
└── GetDocumentsHandler.cs, …            — хендлер слайсу з SQL-кодом (Dapper)

backend/Features/Documents/              namespace CorrespondenceApi.Features.Documents
├── DocumentsEndpoints.cs                — маршрути /api/documents (ISender.Send → сервіс даних)
└── DeleteDocument.cs                    — локальний слайс backend (видалення + файли у сховищі)
```

Правила:
* один публічний DTO — один файл у `Dtos/`, з конкретною назвою (`RoleDto`, `UserOptionsDto`,
  `OverdueTaskRow`), а не загальною `Dto`;
* слайс у `Contracts` — статичний клас (`GetRoles`, `SaveUser`) з вкладеними `Query`/`Command`
  і `Validator`; його хендлер у `dataservice` — клас `<Слайс>Handler` (`GetRolesHandler`);
* проміжні рядки, потрібні лише одному запиту (напр. `RoleRow` для мапінгу Dapper), лишаються
  `private` усередині хендлера;
* у backend лишаються лише слайси, яким потрібне щось, крім БД: вхід (видача JWT) і вкладення
  (файли в MinIO) — вони викликають операції сервісу даних через `ISender`.

## Авторизація

Рольова модель із **динамічними пермішенами** (зберігаються в БД, керуються через
розділ «Ролі та доступ», застосовуються пер-запит). Зони видимості кореспонденції:
вся / свого підрозділу / лише задіяні документи.
