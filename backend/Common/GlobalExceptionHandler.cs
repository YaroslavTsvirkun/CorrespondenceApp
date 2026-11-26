using System.Net.Mime;
using CorrespondenceApi.Infrastructure.Conversion;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceSystem.Contracts.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace CorrespondenceApi.Common;

/// <summary>
/// Глобальна обробка винятків -> HTTP-коди. Спільні винятки (валідація, 401, 403, 404) — через
/// <see cref="Errors.ToProblem"/>, як і в сервісі даних. Помилки сервісу даних 4xx передаються клієнту
/// з тим самим кодом і тілом; збої сервісу даних — 502/503.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (Errors.ToProblem(ex) is { } common)
            return await WriteAsync(ctx, common, ct);

        switch (ex)
        {
            // Клієнт розірвав з'єднання під час обробки — це не помилка сервера
            case OperationCanceledException when ctx.RequestAborted.IsCancellationRequested:
                logger.LogDebug("Запит {Method} {Path} скасовано клієнтом", ctx.Request.Method, ctx.Request.Path);
                ctx.Response.StatusCode = 499; // Client Closed Request
                return true;

            case DocumentConversionException ce:
                logger.LogError(ex, "Помилка конвертації документа для перегляду");
                return await WriteAsync(ctx, Errors.Simple(StatusCodes.Status422UnprocessableEntity,
                    ce.InnerException is null ? "Не вдалося підготувати документ до перегляду" : ce.Message), ct);

            case DataApiException { StatusCode: var code } de when (int)code is >= 400 and < 500:
                ctx.Response.StatusCode = (int)code;
                ctx.Response.ContentType = MediaTypeNames.Application.Json;
                await ctx.Response.WriteAsync(de.Body ?? "", ct);
                return true;

            case DataApiException de:
                logger.LogError(ex, "Сервіс даних повернув помилку {Status}: {Body}", (int)de.StatusCode, de.Body);
                return await WriteAsync(ctx, Errors.Simple(StatusCodes.Status502BadGateway, "Помилка сервісу даних"), ct);

            case HttpRequestException:
                logger.LogError(ex, "Сервіс даних недоступний");
                return await WriteAsync(ctx, Errors.Simple(StatusCodes.Status503ServiceUnavailable, "Сервіс даних недоступний"), ct);

            default:
                return false;
        }
    }

    private static async ValueTask<bool> WriteAsync(HttpContext ctx, Errors.Problem problem, CancellationToken ct)
    {
        ctx.Response.StatusCode = problem.Status;
        await ctx.Response.WriteAsJsonAsync(problem.Body, ct);
        return true;
    }
}
