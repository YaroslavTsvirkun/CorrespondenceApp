var builder = DistributedApplication.CreateBuilder(args);

// Файлове сховище вкладень документів — S3-сумісний MinIO у контейнері (потрібен Docker).
// Дані зберігаються в іменованому томі, тож переживають перезапуск AppHost.
// Логін/пароль — параметри Aspire (генеруються й зберігаються в user secrets AppHost).
// Офіційні образи minio/minio більше не публікуються (з кінця 2025 р.), тому використовується
// спільнотна збірка MinIO з вихідного коду: https://github.com/pgsty/minio (версія зафіксована).
var minio = builder.AddMinioContainer("minio")
    .WithImage("pgsty/minio", "RELEASE.2026-08-04T00-00-00Z")
    .WithDataVolume("correspondence-minio-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Конвертер документів для перегляду вкладень (DOC/DOCX/XLS/XLSX… -> PDF): Gotenberg з LibreOffice.
// Назовні не публікується, викликається лише backend (service discovery: http://gotenberg).
var gotenberg = builder.AddContainer("gotenberg", "gotenberg/gotenberg", "8.38.1-libreoffice")
    .WithHttpEndpoint(targetPort: 3000, name: "http")
    .WithArgs("gotenberg", "--api-timeout=120s", "--libreoffice-restart-after=50")
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithLifetime(ContainerLifetime.Persistent);

// Спільний секрет для викликів backend -> dataservice (генерується й зберігається в user secrets AppHost).
var dataApiKey = builder.AddParameter("data-api-key",
    new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

// Сервіс даних (ASP.NET Core, Dapper) — єдиний компонент із доступом до MS SQL Server LocalDB.
// Під час старту сам розгортає SQL-код із dataservice/Database (схема, функції, процедури, тригери, дані).
// Назовні не публікується: його викликає лише backend через внутрішній Data API (POST /rpc/{операція}).
var dataservice = builder.AddDotnetProject("dataservice", "../dataservice/CorrespondenceSystem.DataService.csproj")
    .WithEnvironment("DataApi__ApiKey", dataApiKey)
    .WithHttpHealthCheck("/health");

// Бекенд (ASP.NET Core API): автентифікація (JWT), авторизація, файли вкладень у MinIO.
// Дані читає й пише через сервіс даних (адреса — service discovery, ключ — DataApi__ApiKey).
// Рядок підключення до MinIO приходить як ConnectionStrings__minio.
var backend = builder.AddDotnetProject("backend", "../backend/CorrespondenceApi.csproj")
    .WithReference(dataservice)
    .WaitFor(dataservice)
    .WithEnvironment("DataApi__ApiKey", dataApiKey)
    .WithReference(minio)
    .WaitFor(minio)
    .WithReference(gotenberg.GetEndpoint("http"))
    .WaitFor(gotenberg)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Фронтенд (Vite/React). Aspire запускає `npm run dev` і прокидає URL бекенда
// як env-змінні BACKEND_HTTP/BACKEND_HTTPS (їх використовує проксі у vite.config).
builder.AddViteApp("frontend", "../frontend")
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();
