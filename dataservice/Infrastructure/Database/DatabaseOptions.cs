namespace CorrespondenceSystem.DataService.Infrastructure.Database;

/// <summary>Налаштування доступу до БД (секція <c>Database</c> + рядок підключення <c>Default</c>).</summary>
public sealed class DatabaseOptions
{
    public const string DefaultConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=CorrespondenceApiDb;Trusted_Connection=True;TrustServerCertificate=True";

    public string ConnectionString { get; set; } = DefaultConnectionString;

    /// <summary>Розгортати SQL-скрипти під час старту застосунку.</summary>
    public bool DeployOnStartup { get; set; } = true;

    /// <summary>Шлях до директорії SQL-скриптів (відносний — від директорії збірки).</summary>
    public string ScriptsPath { get; set; } = "Database";

    /// <summary>Таймаут виконання одного пакета SQL-скрипта, с.</summary>
    public int CommandTimeoutSeconds { get; set; } = 120;

    public string ResolveScriptsPath() =>
        Path.IsPathRooted(ScriptsPath) ? ScriptsPath : Path.Combine(AppContext.BaseDirectory, ScriptsPath);
}
