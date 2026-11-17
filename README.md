# Інформаційна система «Відділ реєстрації кореспонденції»

Рішення (solution) на базі **.NET Aspire**:

| Проєкт | Технології |
|---|---|
| **AppHost** (`AppHost/`) | .NET Aspire — оркестрація: запускає бекенд і фронтенд разом, дашборд, телеметрія, service discovery. |
| **ServiceDefaults** (`ServiceDefaults/`) | Спільні налаштування Aspire: OpenTelemetry, health checks, resilience. |
| **backend** (`backend/CorrespondenceApi.csproj`) | ASP.NET Core 10, EF Core 10 (MS SQL Server LocalDB), MediatR, FluentValidation, JWT. Архітектура — Vertical Slice. |
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

> Фронтенд більше не потребує `.esproj` — його запускає Aspire. БД (LocalDB) і тестові
> дані створюються автоматично під час старту бекенда.

### Окремий запуск (без Aspire)
`backend`: `dotnet run` (→ `http://localhost:5080`, Swagger `/swagger`).
`frontend`: `npm install && npm run dev` (→ `http://localhost:5180`, проксі на :5080).

## Демо-облікові записи

| Логін | Пароль | Роль |
|---|---|---|
| admin | admin123 | Адміністратор |
| registrar | registrar123 | Реєстратор |
| manager | manager123 | Керівник |
| executor | executor123 | Виконавець |

## Авторизація

Рольова модель із **динамічними пермішенами** (зберігаються в БД, керуються через
розділ «Ролі та доступ», застосовуються пер-запит). Зони видимості кореспонденції:
вся / свого підрозділу / лише задіяні документи.
