namespace CorrespondenceSystem.DataService.Infrastructure.Database;

public static class DatabaseDeploymentExtensions
{
    /// <summary>Аргумент командного рядка: лише розгорнути БД і завершити роботу.</summary>
    public const string DeployOnlyArgument = "--deploy-db";

    /// <summary>
    /// Автоматичне розгортання SQL-коду з директорії <c>Database/</c> (схема, функції, процедури, тригери, дані).
    /// Виконується, якщо <see cref="DatabaseOptions.DeployOnStartup"/> = true або передано <c>--deploy-db</c>.
    /// </summary>
    /// <returns>
    /// <c>true</c>, якщо застосунок запущено з <c>--deploy-db</c> і після розгортання слід завершити роботу.
    /// </returns>
    public static async Task<bool> DeployDatabaseAsync(this WebApplication app, string[] args)
    {
        var deployOnly = args.Contains(DeployOnlyArgument);
        var options = app.Services.GetRequiredService<DatabaseOptions>();

        if (options.DeployOnStartup || deployOnly)
            await app.Services.GetRequiredService<SqlScriptDeployer>().DeployAsync();

        return deployOnly;
    }
}
