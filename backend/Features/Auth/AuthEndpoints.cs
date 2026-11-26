using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Auth;

/// <summary>
/// Фіча «Автентифікація»: вхід, вихід і дані поточного користувача.
/// JWT видається в HttpOnly cookie (<see cref="AuthCookie"/>) і в тіло відповіді не потрапляє.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuth(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/login", async (ISender m, HttpContext http, LoginRequest request, CancellationToken ct) =>
            {
                var result = await m.Send(new Login.Command(request), ct);
                AuthCookie.Append(http, result.Token, DateTimeOffset.UtcNow.Add(TokenService.Lifetime));
                return Results.Ok(result.User);
            })
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitOptions.Policy);

        // Вихід: токен відкликається на сервері (навіть скопійований до виходу більше не діє) і cookie видаляється
        g.MapPost("/logout", async (ISender m, HttpContext http, ILoggerFactory loggers, CancellationToken ct) =>
            {
                var user = http.User;
                if (user.Identity?.IsAuthenticated == true
                    && user.FindTokenId() is { } tokenId
                    && long.TryParse(user.FindFirst("exp")?.Value, out var exp))
                {
                    try
                    {
                        await m.Send(new RevokeToken.Command(tokenId, DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime), ct);
                    }
                    catch (Exception e) when (e is DataApiException or HttpRequestException)
                    {
                        // Cookie однаково видаляємо: браузер вийде із системи, а токен спливе сам
                        loggers.CreateLogger("Auth").LogError(e, "Не вдалося відкликати токен під час виходу");
                    }
                }
                AuthCookie.Delete(http);
                return Results.NoContent();
            })
            .AllowAnonymous();

        g.MapGet("/me", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new Me.Query(), ct)))
            .RequireAuthorization();
    }
}
