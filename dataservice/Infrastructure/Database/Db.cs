using System.Data;
using CorrespondenceSystem.DataService.Common;
using Dapper;
using Microsoft.Data.SqlClient;
namespace CorrespondenceSystem.DataService.Infrastructure.Database;

/// <summary>
/// Фабрика підключень Dapper. Кожне відкрите підключення отримує в SESSION_CONTEXT
/// ідентифікатори поточного користувача (UserId) і його працівника (EmployeeId) — їх використовують
/// тригери БД (історія статусів документа, журнал змін банківських рахунків фіксують, хто змінив).
/// </summary>
public sealed class Db(DatabaseOptions options, ICurrentUser user)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        if (user.UserId is not null)
            await connection.ExecuteAsync(new CommandDefinition("""
                EXEC sp_set_session_context @key = N'UserId', @value = @userId;
                IF @employeeId IS NOT NULL EXEC sp_set_session_context @key = N'EmployeeId', @value = @employeeId;
                """, new { userId = user.UserId, employeeId = user.EmployeeId }, cancellationToken: ct));
        return connection;
    }

    /// <summary>Виклик збереженої процедури без результату.</summary>
    public async Task ExecuteProcAsync(string procedure, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        await c.ExecuteAsync(Proc(procedure, param, ct));
    }

    /// <summary>Виклик збереженої процедури, що повертає одне скалярне значення.</summary>
    public async Task<T> ScalarProcAsync<T>(string procedure, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        return (await c.ExecuteScalarAsync<T>(Proc(procedure, param, ct)))!;
    }

    /// <summary>Виклик збереженої процедури, що повертає набір рядків.</summary>
    public async Task<IReadOnlyList<T>> QueryProcAsync<T>(string procedure, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        return (await c.QueryAsync<T>(Proc(procedure, param, ct))).AsList();
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        return (await c.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct))).AsList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        return await c.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    public async Task<T> ScalarAsync<T>(string sql, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        return (await c.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, cancellationToken: ct)))!;
    }

    public async Task<int> ExecuteAsync(string sql, object? param, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        return await c.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct));
    }

    public static CommandDefinition Proc(string procedure, object? param, CancellationToken ct) =>
        new(procedure, param, commandType: CommandType.StoredProcedure, cancellationToken: ct);
}
