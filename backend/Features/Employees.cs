using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Employees;

public record EmployeeInput(int DepartmentId, string FullName, string PositionName,
    string? Phone, string? Email, bool IsActive);

public static class EmployeesEndpoints
{
    public static void MapEmployees(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employees").WithTags("Employees");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetEmployees.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, EmployeeInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveEmployee.Command(null, i), ct);
            return Results.Created($"/api/employees/{id}", new { id });
        }).RequireAuthorization(Permissions.EmployeesManage);

        g.MapPut("/{id:int}", async (ISender m, int id, EmployeeInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveEmployee.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.EmployeesManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteEmployee.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.EmployeesManage);
    }
}

public static class GetEmployees
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string FullName, string PositionName, int DepartmentId,
        string DepartmentName, string? Phone, string? Email, bool IsActive);

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            await db.Employees.OrderBy(e => e.FullName)
                .Select(e => new Dto(e.Id, e.FullName, e.PositionName, e.DepartmentId,
                    e.Department.Name, e.Phone, e.Email, e.IsActive))
                .ToListAsync(ct);
    }
}

public static class SaveEmployee
{
    public record Command(int? Id, EmployeeInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.FullName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Data.PositionName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Data.DepartmentId).GreaterThan(0).WithMessage("Оберіть підрозділ");
        }
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var e = c.Id is int id
                ? await db.Employees.FindAsync([id], ct) ?? throw new NotFoundException($"Працівника #{id} не знайдено")
                : new Employee();
            e.DepartmentId = c.Data.DepartmentId;
            if (c.Id is null)
                e.OrganizationId = await db.Departments.Where(d => d.Id == c.Data.DepartmentId)
                    .Select(d => (int?)d.OrganizationId).FirstOrDefaultAsync(ct)
                    ?? throw new ValidationException([new ValidationFailure("department", "Підрозділ не знайдено")]);
            e.FullName = c.Data.FullName;
            e.PositionName = c.Data.PositionName;
            e.Phone = c.Data.Phone;
            e.Email = c.Data.Email;
            e.IsActive = c.Data.IsActive;
            if (c.Id is null) db.Employees.Add(e);
            await db.SaveChangesAsync(ct);
            return e.Id;
        }
    }
}

public static class DeleteEmployee
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var e = await db.Employees.FindAsync([c.Id], ct)
                ?? throw new NotFoundException($"Працівника #{c.Id} не знайдено");
            db.Employees.Remove(e);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new ValidationException(
                    [new ValidationFailure("employee", "Працівник використовується в документах/дорученнях — видалення заборонено")]);
            }
        }
    }
}
