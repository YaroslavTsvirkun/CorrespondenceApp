using System.Security.Cryptography;
using System.Text;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Auth;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Rpc;

/// <summary>Налаштування Data API (секція <c>DataApi</c>).</summary>
public sealed class DataApiOptions
{
    /// <summary>Спільний секрет із backend (у Aspire — параметр <c>data-api-key</c>).</summary>
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Внутрішній Data API: <c>POST /rpc/{операція}</c> (див. <see cref="DataApi"/>).
/// Запит виконується через MediatR відповідним хендлером із SQL-кодом.
/// </summary>
public static class DataApiEndpoints
{
    public static void MapDataApi(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup(DataApi.RoutePrefix)
            .AddEndpointFilter(AuthenticateAsync)
            .ExcludeFromDescription();

        g.MapPost("/{operation}", async (string operation, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            if (!DataApi.TryGetRequestType(operation, out var requestType))
                return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: $"Невідома операція: {operation}");

            var request = await http.Request.ReadFromJsonAsync(requestType, ct)
                          ?? throw new BadHttpRequestException("Порожнє тіло запиту");
            var result = await sender.Send(request, ct);

            return DataApi.ResponseType(requestType) is null ? Results.NoContent() : Results.Json(result);
        });
    }

    /// <summary>
    /// Перевірка спільного секрету та завантаження користувача із заголовка <c>X-User-Id</c>
    /// (процедура dbo.usp_User_GetAccess; неактивний/невідомий користувач — анонімний).
    /// </summary>
    private static async ValueTask<object?> AuthenticateAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var expected = http.RequestServices.GetRequiredService<DataApiOptions>().ApiKey;
        var actual = http.Request.Headers[DataApi.ApiKeyHeader].ToString();
        if (!FixedTimeEquals(expected, actual))
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Невірний ключ Data API");

        if (int.TryParse(http.Request.Headers[DataApi.UserIdHeader], out var userId))
        {
            var db = http.RequestServices.GetRequiredService<Db>();
            var access = await UserAccess.LoadAsync(db, userId, null, http.RequestAborted);
            if (access is { User.IsActive: true })
                http.RequestServices.GetRequiredService<CurrentUser>().Set(access);
        }

        return await next(ctx);
    }

    private static bool FixedTimeEquals(string expected, string actual) =>
        expected.Length > 0 && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}
