using CorrespondenceApi.Common;
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

        g.MapPost("/logout", (HttpContext http) =>
            {
                AuthCookie.Delete(http);
                return Results.NoContent();
            })
            .AllowAnonymous();

        g.MapGet("/me", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new Me.Query(), ct)))
            .RequireAuthorization();
    }
}
