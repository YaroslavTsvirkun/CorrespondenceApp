using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Data.SqlClient;

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

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<Dto>($"""
                SELECT e.Id, e.FullName, e.PositionName, e.DepartmentId, d.Name AS DepartmentName,
                       e.Phone, e.Email, e.IsActive
                FROM dbo.Employee e
                JOIN dbo.Department d ON d.Id = e.DepartmentId
                WHERE {SqlScope.TenantFilter("e")}
                ORDER BY e.FullName
                """, SqlScope.Tenant(user), ct);
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

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(c.Data);
            p.Add("Id", c.Id);

            // Організація працівника = організація підрозділу (підрозділ має бути видимий користувачу)
            var departmentOrg = await db.QuerySingleOrDefaultAsync<int?>($"""
                SELECT d.OrganizationId FROM dbo.Department d
                WHERE d.Id = @DepartmentId AND {SqlScope.TenantFilter("d")}
                """, p, ct)
                ?? throw new ValidationException([new ValidationFailure("department", "Підрозділ не знайдено")]);
            p.Add("OrganizationId", departmentOrg);

            if (c.Id is null)
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Phone, Email, IsActive)
                    VALUES (@OrganizationId, @DepartmentId, @FullName, @PositionName, @Phone, @Email, @IsActive);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, p, ct);

            var affected = await db.ExecuteAsync($"""
                UPDATE e
                SET DepartmentId = @DepartmentId, FullName = @FullName, PositionName = @PositionName,
                    Phone = @Phone, Email = @Email, IsActive = @IsActive
                FROM dbo.Employee e
                WHERE e.Id = @Id AND {SqlScope.TenantFilter("e")}
                """, p, ct);
            if (affected == 0) throw new NotFoundException($"Працівника #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}

public static class DeleteEmployee
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            try
            {
                var affected = await db.ExecuteAsync(
                    $"DELETE e FROM dbo.Employee e WHERE e.Id = @Id AND {SqlScope.TenantFilter("e")}", p, ct);
                if (affected == 0) throw new NotFoundException($"Працівника #{c.Id} не знайдено");
            }
            catch (SqlException ex) when (SqlErrors.IsReferenceViolation(ex))
            {
                throw new ValidationException(
                    [new ValidationFailure("employee", "Працівник використовується в документах/дорученнях — видалення заборонено")]);
            }
        }
    }
}
