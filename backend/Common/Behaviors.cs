using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;

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

            default:
                return false;
        }
    }
}
