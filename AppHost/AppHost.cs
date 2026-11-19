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

// Бекенд (ASP.NET Core API, Dapper). Працює з MS SQL Server LocalDB через рядок підключення;
// під час старту сам розгортає SQL-код із backend/Database (схема, функції, процедури, тригери, дані).
// Рядок підключення до MinIO приходить як ConnectionStrings__minio.
var backend = builder.AddDotnetProject("backend", "../backend/CorrespondenceApi.csproj")
    .WithReference(minio)
    .WaitFor(minio)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Фронтенд (Vite/React). Aspire запускає `npm run dev` і прокидає URL бекенда
// як env-змінні BACKEND_HTTP/BACKEND_HTTPS (їх використовує проксі у vite.config).
builder.AddViteApp("frontend", "../frontend")
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();
