using System.Net.Mime;
using CorrespondenceApi.Infrastructure.Conversion;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CorrespondenceApi.Common;

/// <summary>
/// Глобальна обробка винятків -> коректні HTTP-коди (400 / 401 / 404).
/// Помилки сервісу даних (400/401/404 — валідація, бізнес-правила, «не знайдено»)
/// передаються клієнту з тим самим кодом і тілом; збої сервісу даних — 502/503.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
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

            case DocumentConversionException ce:
                logger.LogError(ex, "Помилка конвертації документа для перегляду");
                ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    title = ce.InnerException is null ? "Не вдалося підготувати документ до перегляду" : ce.Message,
                    status = 422,
                }, ct);
                return true;

            case DataApiException { StatusCode: var code } de when (int)code is >= 400 and < 500:
                ctx.Response.StatusCode = (int)code;
                ctx.Response.ContentType = MediaTypeNames.Application.Json;
                await ctx.Response.WriteAsync(de.Body ?? "", ct);
                return true;

            case DataApiException de:
                logger.LogError(ex, "Сервіс даних повернув помилку {Status}: {Body}", (int)de.StatusCode, de.Body);
                ctx.Response.StatusCode = StatusCodes.Status502BadGateway;
                await ctx.Response.WriteAsJsonAsync(new { title = "Помилка сервісу даних", status = 502 }, ct);
                return true;

            case HttpRequestException:
                logger.LogError(ex, "Сервіс даних недоступний");
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await ctx.Response.WriteAsJsonAsync(new { title = "Сервіс даних недоступний", status = 503 }, ct);
                return true;

            default:
                return false;
        }
    }
}
