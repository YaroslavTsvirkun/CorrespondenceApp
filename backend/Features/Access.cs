using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            await db.Permissions.OrderBy(p => p.Category).ThenBy(p => p.Id)
                .Select(p => new Dto(p.Code, p.Description, p.Category)).ToListAsync(ct);
    }
}

public static class GetRoles
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string Name, string? Description, int UserCount, IReadOnlyList<string> Permissions);

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            await db.Roles.OrderBy(r => r.Name)
                .Select(r => new Dto(
                    r.Id, r.Name, r.Description, r.UserRoles.Count,
                    r.RolePermissions.Select(rp => rp.Permission.Code).ToList()))
                .ToListAsync(ct);
    }
}

// Створення / оновлення ролі разом із набором її пермішенів.
public static class SaveRole
{
    public record Input(string Name, string? Description, List<string> Permissions);
    public record Command(int? Id, Input Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(50);
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var permIds = await db.Permissions
                .Where(p => c.Data.Permissions.Contains(p.Code))
                .Select(p => p.Id).ToListAsync(ct);

            Role role;
            if (c.Id is int id)
            {
                role = await db.Roles.Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.Id == id, ct)
                    ?? throw new NotFoundException($"Роль #{id} не знайдено");
                role.Name = c.Data.Name;
                role.Description = c.Data.Description;
                role.RolePermissions.Clear();
            }
            else
            {
                role = new Role { Name = c.Data.Name, Description = c.Data.Description };
                db.Roles.Add(role);
            }

            foreach (var pid in permIds)
                role.RolePermissions.Add(new RolePermission { Role = role, PermissionId = pid });

            await db.SaveChangesAsync(ct);
            return role.Id;
        }
    }
}

public static class DeleteRole
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var role = await db.Roles.Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == c.Id, ct)
                ?? throw new NotFoundException($"Роль #{c.Id} не знайдено");

            if (role.UserRoles.Count > 0)
                throw new ValidationException(
                    [new ValidationFailure("role", "Роль призначена користувачам — спочатку зніміть призначення")]);

            db.Roles.Remove(role);
            await db.SaveChangesAsync(ct);
        }
    }
}
