using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CorrespondenceSystem.DataService.Infrastructure.Database;

public static class DatabaseDeploymentExtensions
{
    /// <summary>Аргумент командного рядка: лише розгорнути БД і завершити роботу.</summary>
    public const string DeployOnlyArgument = "--deploy-db";

    /// <summary>
    /// Автоматичне розгортання SQL-коду з директорії <c>Database/</c> (схема, функції, процедури, тригери, дані).
    /// Виконується, якщо <see cref="DatabaseOptions.DeployOnStartup"/> = true або передано <c>--deploy-db</c>.
    /// Після розгортання, якщо в БД немає користувачів, створюється перший супер-адміністратор
    /// (<see cref="DatabaseOptions.InitialAdmin"/>).
    /// </summary>
    /// <returns>
    /// <c>true</c>, якщо застосунок запущено з <c>--deploy-db</c> і після розгортання слід завершити роботу.
    /// </returns>
    public static async Task<bool> DeployDatabaseAsync(this WebApplication app, string[] args)
    {
        var deployOnly = args.Contains(DeployOnlyArgument);
        var options = app.Services.GetRequiredService<DatabaseOptions>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseDeployment");

        if (options.SeedDemoData && !app.Environment.IsDevelopment())
            logger.LogWarning("Увімкнено Database:SeedDemoData поза середовищем Development — у БД з'являться " +
                              "демо-користувачі з відомими паролями. Для робочої БД вимкніть це налаштування.");

        if (options.DeployOnStartup || deployOnly)
        {
            await app.Services.GetRequiredService<SqlScriptDeployer>().DeployAsync();
            await EnsureInitialAdminAsync(options, logger);
        }

        return deployOnly;
    }

    private static async Task EnsureInitialAdminAsync(DatabaseOptions options, ILogger logger)
    {
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync();
        if (await connection.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AppUser) THEN 1 ELSE 0 END AS bit)"))
            return;

        var admin = options.InitialAdmin;
        if (string.IsNullOrEmpty(admin.Password))
        {
            logger.LogWarning("У БД немає жодного користувача. Задайте Database:InitialAdmin:Password " +
                              "(напр. змінною середовища Database__InitialAdmin__Password), щоб створити супер-адміністратора.");
            return;
        }
        if (admin.Password.Length < 12)
            throw new InvalidOperationException("Database:InitialAdmin:Password має містити щонайменше 12 символів.");

        await connection.ExecuteAsync(
            """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WITH (UPDLOCK, HOLDLOCK))
            BEGIN
                INSERT dbo.AppUser (UserName, PasswordHash, FullName, OrganizationId, EmployeeId)
                VALUES (@UserName, @PasswordHash, @FullName, NULL, NULL);
                DECLARE @UserId int = CAST(SCOPE_IDENTITY() AS int);
                INSERT dbo.UserRole (UserId, RoleId)
                SELECT @UserId, r.RoleId FROM dbo.fn_RolesWithPermission(@SuperPermission) r;
            END;
            COMMIT TRANSACTION;
            """,
            new
            {
                admin.UserName,
                PasswordHash = PasswordHasher.Hash(admin.Password),
                admin.FullName,
                SuperPermission = Permissions.OrganizationsManage,
            });
        logger.LogInformation("Створено першого супер-адміністратора «{UserName}»", admin.UserName);
    }
}
