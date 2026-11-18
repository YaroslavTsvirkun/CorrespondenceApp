using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;

namespace CorrespondenceApi.Common;

/// <summary>Сторінковий результат вибірки.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>Кидається слайсом, коли сутність не знайдено -> 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Невдала автентифікація -> 401.</summary>
public class UnauthorizedException(string message) : Exception(message);

/// <summary>MediatR-пайплайн: автоматична валідація запитів через FluentValidation.</summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
            var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
            if (failures.Count != 0)
                throw new ValidationException(failures);
        }
        return await next(ct);
    }
}

/// <summary>
/// Коди помилок, які кидають збережені процедури через THROW (див. Database/Procedures).
/// </summary>
public static class SqlErrors
{
    public const int BusinessRule = 50400;   // порушення бізнес-правила -> 400
    public const int NotFound = 50404;       // сутність не знайдено -> 404
    public const int ForeignKey = 547;       // запис використовується іншими даними
    public const int UniqueIndex = 2601;
    public const int UniqueConstraint = 2627;

    public static bool IsReferenceViolation(SqlException ex) => ex.Number == ForeignKey;
}

/// <summary>Глобальна обробка винятків -> коректні HTTP-коди (400 / 404).</summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        switch (ex)
        {
            case ValidationException ve:
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    title = "Помилка валідації",
                    status = 400,
                    errors = ve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                }, ct);
                return true;

            case NotFoundException nf:
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                await ctx.Response.WriteAsJsonAsync(new { title = nf.Message, status = 404 }, ct);
                return true;

            case UnauthorizedException ue:
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { title = ue.Message, status = 401 }, ct);
                return true;

            case SqlException { Number: SqlErrors.BusinessRule } se:
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new { title = se.Message, status = 400 }, ct);
                return true;

            case SqlException { Number: SqlErrors.NotFound } se:
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                await ctx.Response.WriteAsJsonAsync(new { title = se.Message, status = 404 }, ct);
                return true;

            case SqlException { Number: SqlErrors.ForeignKey }:
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    title = "Запис пов'язаний з іншими даними — операцію заборонено",
                    status = 400,
                }, ct);
                return true;

            case SqlException { Number: SqlErrors.UniqueIndex or SqlErrors.UniqueConstraint }:
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsJsonAsync(new { title = "Запис із такими даними вже існує", status = 400 }, ct);
                return true;

            default:
                return false;
        }
    }
}
