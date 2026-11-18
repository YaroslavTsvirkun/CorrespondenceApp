using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CorrespondenceApi.Infrastructure.Database;

/// <summary>
/// Автоматичне розгортання SQL-коду з директорії проєкту <c>Database/</c> у БД.
/// Порядок етапів:
///   1. <c>Schema</c>         — одноразові скрипти (таблиці, індекси), журнал у dbo.__SchemaVersions;
///   2. <c>Types</c>          — табличні типи (IF TYPE_ID(...) IS NULL CREATE TYPE, при кожному старті);
///   3. <c>TableFunctions</c> — табличні функції (CREATE OR ALTER, при кожному старті);
///   4. <c>Functions</c>      — скалярні функції  (CREATE OR ALTER, при кожному старті);
///   5. <c>Views</c>          — представлення      (CREATE OR ALTER, при кожному старті);
///   6. <c>Procedures</c>     — збережені процедури (CREATE OR ALTER, при кожному старті);
///   7. <c>Triggers</c>       — тригери            (CREATE OR ALTER, при кожному старті);
///   8. <c>Seed</c>           — одноразові скрипти початкових даних (журналюються).
/// Усередині етапу скрипти виконуються в алфавітному порядку імен файлів.
/// Скрипт ділиться на пакети за рядками <c>GO</c>; кожен скрипт виконується в окремій транзакції.
/// </summary>
public sealed class SqlScriptDeployer(DatabaseOptions options, ILogger<SqlScriptDeployer> logger)
{
    private const string JournalTable = "dbo.__SchemaVersions";

    private static readonly (string Folder, bool RunOnce)[] Stages =
    [
        ("Schema", true),
        ("Types", false),
        ("TableFunctions", false),
        ("Functions", false),
        ("Views", false),
        ("Procedures", false),
        ("Triggers", false),
        ("Seed", true),
    ];

    private static readonly Regex BatchSeparator = new(
        @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task DeployAsync(CancellationToken ct = default)
    {
        var root = options.ResolveScriptsPath();
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Директорію SQL-скриптів не знайдено: {root}");

        logger.LogInformation("Розгортання SQL-коду з {Path}", root);
        await EnsureDatabaseAsync(ct);

        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        await EnsureJournalAsync(connection);

        var applied = (await connection.QueryAsync<(string ScriptName, string Checksum)>(
                $"SELECT ScriptName, Checksum FROM {JournalTable}"))
            .ToDictionary(x => x.ScriptName, x => x.Checksum, StringComparer.OrdinalIgnoreCase);

        int executed = 0, skipped = 0;
        foreach (var (folder, runOnce) in Stages)
        {
            var dir = Path.Combine(root, folder);
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.GetFiles(dir, "*.sql").Order(StringComparer.OrdinalIgnoreCase))
            {
                var name = $"{folder}/{Path.GetFileName(file)}";
                var sql = await File.ReadAllTextAsync(file, Encoding.UTF8, ct);
                var checksum = Checksum(sql);

                if (runOnce && applied.TryGetValue(name, out var oldChecksum))
                {
                    if (!string.Equals(oldChecksum, checksum, StringComparison.OrdinalIgnoreCase))
                        logger.LogWarning(
                            "Одноразовий скрипт {Script} змінено після застосування — повторно не виконується. " +
                            "Для змін схеми додайте новий скрипт.", name);
                    skipped++;
                    continue;
                }

                await ExecuteScriptAsync(connection, name, sql, checksum, runOnce, ct);
                executed++;
            }
        }

        logger.LogInformation("SQL-розгортання завершено: виконано {Executed}, пропущено {Skipped}", executed, skipped);
    }

    private async Task ExecuteScriptAsync(SqlConnection connection, string name, string sql, string checksum,
        bool journal, CancellationToken ct)
    {
        logger.LogDebug("Виконання {Script}", name);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        try
        {
            foreach (var batch in SplitBatches(sql))
                await connection.ExecuteAsync(new CommandDefinition(
                    batch, transaction: tx, commandTimeout: options.CommandTimeoutSeconds, cancellationToken: ct));

            if (journal)
                await connection.ExecuteAsync(new CommandDefinition(
                    $"INSERT {JournalTable} (ScriptName, Checksum, AppliedAt) VALUES (@name, @checksum, SYSUTCDATETIME())",
                    new { name, checksum }, tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            if (tx.Connection is not null) await tx.RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException($"Помилка виконання SQL-скрипта {name}: {ex.Message}", ex);
        }
    }

    private async Task EnsureDatabaseAsync(CancellationToken ct)
    {
        var builder = new SqlConnectionStringBuilder(options.ConnectionString);
        var database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) return;

        builder.InitialCatalog = "master";
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync(ct);
        var created = await master.ExecuteScalarAsync<int>(
            """
            IF DB_ID(@database) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@database);
                EXEC (@sql);
                SELECT 1;
            END
            ELSE SELECT 0;
            """, new { database });
        if (created == 1) logger.LogInformation("Створено базу даних {Database}", database);
    }

    private static Task EnsureJournalAsync(SqlConnection connection) => connection.ExecuteAsync(
        $"""
        IF OBJECT_ID(N'{JournalTable}', N'U') IS NULL
            CREATE TABLE {JournalTable}
            (
                ScriptName nvarchar(255) NOT NULL CONSTRAINT PK___SchemaVersions PRIMARY KEY,
                Checksum   char(64)      NOT NULL,
                AppliedAt  datetime2     NOT NULL
            );
        """);

    internal static IEnumerable<string> SplitBatches(string sql) =>
        BatchSeparator.Split(sql).Where(b => !string.IsNullOrWhiteSpace(b));

    private static string Checksum(string sql) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql.Replace("\r\n", "\n"))));
}
