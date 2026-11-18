var builder = DistributedApplication.CreateBuilder(args);

// Бекенд (ASP.NET Core API, Dapper). Працює з MS SQL Server LocalDB через рядок підключення;
// під час старту сам розгортає SQL-код із backend/Database (схема, функції, процедури, тригери, дані).
var backend = builder.AddProject<Projects.CorrespondenceApi>("backend")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Фронтенд (Vite/React). Aspire запускає `npm run dev` і прокидає URL бекенда
// як env-змінні BACKEND_HTTP/BACKEND_HTTPS (їх використовує проксі у vite.config).
builder.AddViteApp("frontend", "../frontend")
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();
