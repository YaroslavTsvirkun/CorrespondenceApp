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

    /// <summary>
    /// Таблична функція «документи, видимі користувачу» (тенант + зона видимості) для JOIN у списках:
    /// <c>FROM dbo.vw_Document d JOIN {VisibleDocuments} v ON v.Id = d.Id</c>.
    /// Параметри — з <see cref="Documents"/>.
    /// </summary>
    public const string VisibleDocuments =
        "dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, @Bypass, @TenantOrganizationId)";

    /// <summary>
    /// Умова «конкретний документ видимий користувачу» (тенант + скалярна функція
    /// dbo.fn_CanReadDocument) — для вибірки одного документа за Id.
    /// </summary>
    public static string DocumentFilter(string alias) =>
        $"{TenantFilter(alias)} AND dbo.fn_CanReadDocument({alias}.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1";
}

/// <summary>
/// Табличні параметри (User-Defined Table Types з Database/Types) для передачі наборів у процедури.
/// </summary>
public static class SqlTvp
{
    /// <summary>dbo.IdList — унікальні Id.</summary>
    public static SqlMapper.ICustomQueryParameter IdList(IEnumerable<int> ids)
    {
        var t = new DataTable();
        t.Columns.Add("Id", typeof(int));
        foreach (var id in ids.Distinct()) t.Rows.Add(id);
        return t.AsTableValuedParameter("dbo.IdList");
    }

    /// <summary>dbo.CodeList — унікальні непорожні коди.</summary>
    public static SqlMapper.ICustomQueryParameter CodeList(IEnumerable<string> codes)
    {
        var t = new DataTable();
        t.Columns.Add("Code", typeof(string));
        foreach (var code in codes.Where(c => !string.IsNullOrWhiteSpace(c))
                     .Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            t.Rows.Add(code);
        return t.AsTableValuedParameter("dbo.CodeList");
    }

    /// <summary>dbo.TaskAssignmentList — набір доручень (рядки нумеруються за порядком).</summary>
    public static SqlMapper.ICustomQueryParameter TaskAssignments(
        IEnumerable<(int ExecutorEmployeeId, int TaskStatusId, string TaskText, DateOnly DueDate)> items)
    {
        var t = new DataTable();
        t.Columns.Add("RowNo", typeof(int));
        t.Columns.Add("ExecutorEmployeeId", typeof(int));
        t.Columns.Add("TaskStatusId", typeof(int));
        t.Columns.Add("TaskText", typeof(string));
        t.Columns.Add("DueDate", typeof(DateTime));
        var row = 0;
        foreach (var i in items)
            t.Rows.Add(++row, i.ExecutorEmployeeId, i.TaskStatusId, i.TaskText, i.DueDate.ToDateTime(TimeOnly.MinValue));
        return t.AsTableValuedParameter("dbo.TaskAssignmentList");
    }
}
