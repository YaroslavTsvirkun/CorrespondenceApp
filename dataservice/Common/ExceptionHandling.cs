using CorrespondenceSystem.Contracts.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;

namespace CorrespondenceSystem.DataService.Common;

/// <summary>
/// Коди помилок, які кидають збережені процедури через THROW (див. Database/Procedures).
/// </summary>
public static class SqlErrors
{
    public const int BusinessRule = 50400;   // порушення бізнес-правила -> 400
    public const int Forbidden = 50403;      // недостатньо прав на конкретний запис -> 403
    public const int NotFound = 50404;       // сутність не знайдено -> 404
    public const int ForeignKey = 547;       // запис використовується іншими даними
    public const int UniqueIndex = 2601;
    public const int UniqueConstraint = 2627;

    public static bool IsReferenceViolation(SqlException ex) => ex.Number == ForeignKey;
}

/// <summary>
/// Глобальна обробка винятків -> HTTP-коди. Спільні винятки (валідація, 401, 403, 404) — через
/// <see cref="Errors.ToProblem"/>, як і в backend; коди помилок SQL (THROW у процедурах, обмеження БД) — тут.
/// Формат тіла ({ title, status, errors? }) backend передає клієнту без змін.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if ((Errors.ToProblem(ex) ?? FromSql(ex)) is not { } problem)
            return false;

        ctx.Response.StatusCode = problem.Status;
        await ctx.Response.WriteAsJsonAsync(problem.Body, ct);
        return true;
    }

    private static Errors.Problem? FromSql(Exception ex) => ex switch
    {
        SqlException { Number: SqlErrors.BusinessRule } se => Errors.Simple(400, se.Message),
        SqlException { Number: SqlErrors.Forbidden } se => Errors.Simple(403, se.Message),
        SqlException { Number: SqlErrors.NotFound } se => Errors.Simple(404, se.Message),
        SqlException { Number: SqlErrors.ForeignKey } =>
            Errors.Simple(400, "Запис пов'язаний з іншими даними — операцію заборонено"),
        SqlException { Number: SqlErrors.UniqueIndex or SqlErrors.UniqueConstraint } =>
            Errors.Simple(400, "Запис із такими даними вже існує"),
        _ => null,
    };
}
