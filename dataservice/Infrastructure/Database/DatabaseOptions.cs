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

    /// <summary>
    /// Розгортати демо-дані (<c>Database/DemoSeed</c>): організації, документи й користувачів
    /// з відомими паролями (admin/admin123…). Лише для розробки — у робочій БД має бути false.
    /// </summary>
    public bool SeedDemoData { get; set; }

    /// <summary>
    /// Перший супер-адміністратор робочої БД: створюється, якщо в БД ще немає жодного користувача.
    /// Пароль задавайте через секрети/змінні середовища (Database__InitialAdmin__Password), не в appsettings.
    /// </summary>
    public InitialAdminOptions InitialAdmin { get; set; } = new();

    public string ResolveScriptsPath() =>
        Path.IsPathRooted(ScriptsPath) ? ScriptsPath : Path.Combine(AppContext.BaseDirectory, ScriptsPath);
}

/// <summary>Перший супер-адміністратор (секція <c>Database:InitialAdmin</c>).</summary>
public sealed class InitialAdminOptions
{
    public string UserName { get; set; } = "admin";
    public string FullName { get; set; } = "Супер-адміністратор";

    /// <summary>Пароль (мінімум 12 символів). Порожній — користувач не створюється.</summary>
    public string Password { get; set; } = "";
}
