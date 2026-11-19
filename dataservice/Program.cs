using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using CorrespondenceSystem.DataService.Rpc;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// Сервіс даних: єдиний компонент, що має рядок підключення до БД.
// Backend звертається до нього через внутрішній Data API (POST /rpc/{операція}).
var builder = WebApplication.CreateBuilder(args);

// .NET Aspire: телеметрія, health checks, service discovery
builder.AddServiceDefaults();

// ---- Доступ до даних: Dapper + SQL-код (функції, процедури, тригери) у БД ----
var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
dbOptions.ConnectionString = builder.Configuration.GetConnectionString("Default") ?? dbOptions.ConnectionString;
builder.Services.AddSingleton(dbOptions);
builder.Services.AddSingleton<SqlScriptDeployer>();
builder.Services.AddScoped<Db>();
DapperConfig.Configure();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// ---- Data API: спільний секрет із backend + користувач, від імені якого виконується операція ----
var apiOptions = builder.Configuration.GetSection("DataApi").Get<DataApiOptions>() ?? new DataApiOptions();
if (string.IsNullOrWhiteSpace(apiOptions.ApiKey))
    throw new InvalidOperationException("Не задано DataApi:ApiKey — спільний секрет для викликів від backend.");
builder.Services.AddSingleton(apiOptions);
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());

// Хендлери запитів (SQL) — у цій збірці; запити, DTO і валідатори — у Contracts
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(DataApi).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints(); // /health, /alive (Aspire)
app.UseExceptionHandler();
app.MapDataApi();

// Розгортання SQL-коду БД; `dotnet run -- --deploy-db` — лише розгорнути БД і завершити роботу.
if (await app.DeployDatabaseAsync(args)) return;

app.Run();

/// <summary>Перевірка доступності БД для /health.</summary>
internal sealed class DatabaseHealthCheck(DatabaseOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(options.ConnectionString);
            await connection.OpenAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("БД недоступна", ex);
        }
    }
}
