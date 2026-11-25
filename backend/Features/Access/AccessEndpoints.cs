using CorrespondenceSystem.Contracts.Access;
using CorrespondenceSystem.Contracts.Access.Dtos;
using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceApi.Features.Access;

/// <summary>Фіча «Ролі та доступ»: динамічне керування ролями та їх пермішенами (users.manage).</summary>
public static class AccessEndpoints
{
    public static void MapAccess(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/access").WithTags("Access")
            .RequireAuthorization(Permissions.UsersManage);

        g.MapGet("/permissions", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetPermissions.Query(), ct)));

        g.MapGet("/roles", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetRoles.Query(), ct)));

        // Ролі спільні для всіх організацій: змінює їх лише супер-адмін. Інакше адміністратор
        // організації міг би додати собі organizations.manage або змінити ролі інших організацій.
        g.MapPost("/roles", async (ISender m, RoleInput input, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveRole.Command(null, input), ct);
            return Results.Created($"/api/access/roles/{id}", new { id });
        }).RequireAuthorization(Permissions.OrganizationsManage);

        g.MapPut("/roles/{id:int}", async (ISender m, int id, RoleInput input, CancellationToken ct) =>
        {
            await m.Send(new SaveRole.Command(id, input), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.OrganizationsManage);

        g.MapDelete("/roles/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteRole.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.OrganizationsManage);
    }
}
