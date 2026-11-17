using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Organizations;

public record OrganizationInput(string Name, string? Code, int? ParentOrganizationId, bool IsActive,
    string? RegistrationNumberPattern);

public static class OrganizationsEndpoints
{
    public static void MapOrganizations(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/organizations").WithTags("Organizations")
            .RequireAuthorization(Permissions.OrganizationsManage);

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetOrganizations.Query(), ct)));

        g.MapPost("", async (ISender m, OrganizationInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveOrganization.Command(null, i), ct);
            return Results.Created($"/api/organizations/{id}", new { id });
        });

        g.MapPut("/{id:int}", async (ISender m, int id, OrganizationInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveOrganization.Command(id, i), ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteOrganization.Command(id), ct);
            return Results.NoContent();
        });
    }
}

public static class GetOrganizations
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string Name, string? Code, int? ParentOrganizationId, bool IsActive,
        string RegistrationNumberPattern, int DepartmentCount, int UserCount);

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            await db.Organizations.IgnoreQueryFilters().OrderBy(o => o.Name)
                .Select(o => new Dto(o.Id, o.Name, o.Code, o.ParentOrganizationId, o.IsActive,
                    o.RegistrationNumberPattern, o.Departments.Count, o.Users.Count))
                .ToListAsync(ct);
    }
}

public static class SaveOrganization
{
    public record Command(int? Id, OrganizationInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var e = c.Id is int id
                ? await db.Organizations.FindAsync([id], ct) ?? throw new NotFoundException($"Організацію #{id} не знайдено")
                : new Organization();
            e.Name = c.Data.Name;
            e.Code = c.Data.Code;
            e.IsActive = c.Data.IsActive;
            e.ParentOrganizationId = c.Data.ParentOrganizationId == e.Id ? null : c.Data.ParentOrganizationId;
            if (!string.IsNullOrWhiteSpace(c.Data.RegistrationNumberPattern))
                e.RegistrationNumberPattern = c.Data.RegistrationNumberPattern.Trim();
            if (c.Id is null) db.Organizations.Add(e);
            await db.SaveChangesAsync(ct);
            return e.Id;
        }
    }
}

public static class DeleteOrganization
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var e = await db.Organizations.IgnoreQueryFilters()
                .Include(o => o.Children).Include(o => o.Departments).Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == c.Id, ct)
                ?? throw new NotFoundException($"Організацію #{c.Id} не знайдено");
            if (e.Children.Count > 0 || e.Departments.Count > 0 || e.Users.Count > 0)
                throw new ValidationException([new ValidationFailure("organization",
                    "Організація має підрозділи, користувачів або дочірні організації — видалення заборонено")]);
            db.Organizations.Remove(e);
            await db.SaveChangesAsync(ct);
        }
    }
}
