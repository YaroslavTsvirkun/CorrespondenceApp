using System.Data;
using CorrespondenceApi.Common;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CorrespondenceApi.Infrastructure.Database;

/// <summary>
/// Фабрика підключень Dapper. Кожне відкрите підключення отримує в SESSION_CONTEXT
/// ідентифікатор працівника поточного користувача — його використовують тригери БД
/// (напр. історія статусів документа фіксує, хто змінив статус).
/// </summary>
public sealed class Db(DatabaseOptions options, ICurrentUser user)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        if (user.EmployeeId is int employeeId)
            await connection.ExecuteAsync(new CommandDefinition(
                "EXEC sp_set_session_context @key = N'EmployeeId', @value = @employeeId",
                new { employeeId }, cancellationToken: ct));
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

/// <summary>Допоміжні функції для тексту SQL-запитів.</summary>
public static class SqlText
{
    /// <summary>Символ екранування для LIKE (… LIKE @p ESCAPE '!').</summary>
    public const string LikeEscape = "!";

    /// <summary>Екранує спецсимволи LIKE, щоб пошук ішов за буквальним входженням рядка.</summary>
    public static string EscapeLike(string value) => value
        .Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![");
}

/// <summary>
/// Параметри тенант-ізоляції та зони видимості документів для SQL-запитів.
/// Імена властивостей збігаються з параметрами процедур (@Bypass, @TenantOrganizationId, …).
/// </summary>
public static class SqlScope
{
    /// <summary>Тенант: супер-адмін (organizations.manage) бачить усе, решта — лише свою організацію.</summary>
    public static DynamicParameters Tenant(ICurrentUser u)
    {
        var p = new DynamicParameters();
        p.Add("Bypass", u.Has(Permissions.OrganizationsManage));
        p.Add("TenantOrganizationId", u.OrganizationId);
        return p;
    }

    /// <summary>Тенант + зона видимості документів (для dbo.fn_CanReadDocument).</summary>
    public static DynamicParameters Documents(ICurrentUser u)
    {
        var p = Tenant(u);
        p.Add("ScopeLevel", DocumentScope.Level(u));
        p.Add("EmployeeId", u.EmployeeId);
        p.Add("DepartmentId", u.DepartmentId);
        return p;
    }

    /// <summary>Умова тенант-фільтра для таблиці з колонкою OrganizationId.</summary>
    public static string TenantFilter(string alias) =>
        $"(@Bypass = 1 OR {alias}.OrganizationId = @TenantOrganizationId)";

    /// <summary>Умова «документ видимий користувачу» (тенант + скалярна функція зони видимості).</summary>
    public static string DocumentFilter(string alias) =>
        $"{TenantFilter(alias)} AND dbo.fn_CanReadDocument({alias}.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1";
}
