using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Users.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Users;

/// <summary>
/// Фіча «Користувачі»: облікові записи та їх прив'язка до організації.
/// Супер-адмін (право organizations.manage) бачить і редагує всіх, може призначати будь-яку
/// організацію та будь-яку роль. Адміністратор організації обмежений своєю організацією:
/// бачить лише своїх користувачів, нові створює у власній організації й не може видавати
/// «супер-» ролі (ті, що містять organizations.manage).
/// </summary>
public static class UsersEndpoints
{
    public static void MapUsers(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/users").WithTags("Users")
            .RequireAuthorization(Permissions.UsersManage);

        g.MapGet("", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetUsers.Query(), ct)));

        g.MapGet("/options", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetUserOptions.Query(), ct)));

        g.MapPost("", async (ISender m, UserInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveUser.Command(null, i), ct);
            return Results.Created($"/api/users/{id}", new { id });
        });

        g.MapPut("/{id:int}", async (ISender m, int id, UserInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveUser.Command(id, i), ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteUser.Command(id), ct);
            return Results.NoContent();
        });
    }
}
