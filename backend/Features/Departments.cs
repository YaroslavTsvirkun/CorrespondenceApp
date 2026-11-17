using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Departments;

public record DepartmentInput(string Name, string? ShortName, string? Description, bool IsActive,
    int? ParentDepartmentId, int? OrganizationId);

public static class DepartmentsEndpoints
{
    public static void MapDepartments(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/departments").WithTags("Departments");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetDepartments.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, DepartmentInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveDepartment.Command(null, i), ct);
            return Results.Created($"/api/departments/{id}", new { id });
        }).RequireAuthorization(Permissions.DepartmentsManage);

        g.MapPut("/{id:int}", async (ISender m, int id, DepartmentInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveDepartment.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DepartmentsManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteDepartment.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DepartmentsManage);
    }
}

public static class GetDepartments
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string Name, string? ShortName, string? Description, bool IsActive,
        int OrganizationId, string OrganizationName, int? ParentDepartmentId, int EmployeeCount);

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            await db.Departments.OrderBy(d => d.Name)
                .Select(d => new Dto(d.Id, d.Name, d.ShortName, d.Description, d.IsActive,
                    d.OrganizationId, d.Organization.Name, d.ParentDepartmentId, d.Employees.Count))
                .ToListAsync(ct);
    }
}

public static class SaveDepartment
{
    public record Command(int? Id, DepartmentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(150);
    }

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var e = c.Id is int id
                ? await db.Departments.FindAsync([id], ct) ?? throw new NotFoundException($"Підрозділ #{id} не знайдено")
                : new Department();
            if (c.Id is null)
                e.OrganizationId = user.OrganizationId ?? c.Data.OrganizationId
                    ?? throw new ValidationException([new ValidationFailure("organization", "Оберіть організацію")]);
            e.Name = c.Data.Name;
            e.ShortName = c.Data.ShortName;
            e.Description = c.Data.Description;
            e.IsActive = c.Data.IsActive;
            e.ParentDepartmentId = c.Data.ParentDepartmentId == e.Id ? null : c.Data.ParentDepartmentId;
            if (c.Id is null) db.Departments.Add(e);
            await db.SaveChangesAsync(ct);
            return e.Id;
        }
    }
}

public static class DeleteDepartment
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var e = await db.Departments.Include(d => d.Employees).Include(d => d.Children)
                .FirstOrDefaultAsync(d => d.Id == c.Id, ct)
                ?? throw new NotFoundException($"Підрозділ #{c.Id} не знайдено");
            if (e.Employees.Count > 0 || e.Children.Count > 0)
                throw new ValidationException([new ValidationFailure("department",
                    "У підрозділі є працівники або підпорядковані підрозділи — видалення заборонено")]);
            db.Departments.Remove(e);
            await db.SaveChangesAsync(ct);
        }
    }
}
