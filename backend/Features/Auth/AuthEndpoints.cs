using CorrespondenceApi.Features.Auth.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Auth;

/// <summary>Фіча «Автентифікація»: вхід і дані поточного користувача.</summary>
public static class AuthEndpoints
{
    public static void MapAuth(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/login", async (ISender m, LoginRequest request, CancellationToken ct) =>
            Results.Ok(await m.Send(new Login.Command(request), ct)))
            .AllowAnonymous();

        g.MapGet("/me", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new Me.Query(), ct)))
            .RequireAuthorization();
    }
}
