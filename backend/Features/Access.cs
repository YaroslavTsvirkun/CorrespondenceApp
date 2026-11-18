using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Access;

// Динамічне керування ролями та їх пермішенами (усе в БД, доступ: users.manage).
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

        g.MapPost("/roles", async (ISender m, SaveRole.Input input, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveRole.Command(null, input), ct);
            return Results.Created($"/api/access/roles/{id}", new { id });
        });

        g.MapPut("/roles/{id:int}", async (ISender m, int id, SaveRole.Input input, CancellationToken ct) =>
        {
            await m.Send(new SaveRole.Command(id, input), ct);
            return Results.NoContent();
        });

        g.MapDelete("/roles/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteRole.Command(id), ct);
            return Results.NoContent();
        });
    }
}

public static class GetPermissions
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(string Code, string? Description, string? Category);

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<Dto>(
                "SELECT Code, Description, Category FROM dbo.Permission ORDER BY Category, Id", null, ct);
    }
}

public static class GetRoles
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string Name, string? Description, int UserCount, IReadOnlyList<string> Permissions);

    private sealed record RoleRow(int Id, string Name, string? Description, int UserCount);
    private sealed record RolePermissionRow(int RoleId, string Code);

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct)
        {
            await using var c = await db.OpenAsync(ct);
            using var grid = await c.QueryMultipleAsync(new CommandDefinition("""
                SELECT r.Id, r.Name, r.Description,
                       (SELECT COUNT(*) FROM dbo.UserRole ur WHERE ur.RoleId = r.Id) AS UserCount
                FROM dbo.Role r
                ORDER BY r.Name;

                SELECT rp.RoleId, p.Code
                FROM dbo.RolePermission rp
                JOIN dbo.Permission p ON p.Id = rp.PermissionId;
                """, cancellationToken: ct));
            var roles = await grid.ReadAsync<RoleRow>();
            var perms = (await grid.ReadAsync<RolePermissionRow>()).ToLookup(x => x.RoleId, x => x.Code);

            return roles.Select(r => new Dto(r.Id, r.Name, r.Description, r.UserCount, perms[r.Id].ToList()))
                .ToList();
        }
    }
}

// Створення / оновлення ролі разом із набором її пермішенів (процедура dbo.usp_Role_Save).
public static class SaveRole
{
    public record Input(string Name, string? Description, List<string> Permissions);
    public record Command(int? Id, Input Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(50);
    }

    public class Handler(Db db) : IRequestHandler<Command, int>
    {
        public Task<int> Handle(Command c, CancellationToken ct) =>
            db.ScalarProcAsync<int>("dbo.usp_Role_Save", new
            {
                c.Id,
                c.Data.Name,
                c.Data.Description,
                PermissionCodes = SqlTvp.CodeList(c.Data.Permissions ?? []),
            }, ct);
    }
}

// Видалення ролі (процедура dbo.usp_Role_Delete забороняє видаляти призначену роль).
public static class DeleteRole
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct) =>
            db.ExecuteProcAsync("dbo.usp_Role_Delete", new { c.Id }, ct);
    }
}
