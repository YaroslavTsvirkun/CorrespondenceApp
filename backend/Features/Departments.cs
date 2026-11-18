using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

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

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<Dto>($"""
                SELECT d.Id, d.Name, d.ShortName, d.Description, d.IsActive,
                       d.OrganizationId, o.Name AS OrganizationName, d.ParentDepartmentId,
                       (SELECT COUNT(*) FROM dbo.Employee e WHERE e.DepartmentId = d.Id) AS EmployeeCount
                FROM dbo.Department d
                JOIN dbo.Organization o ON o.Id = d.OrganizationId
                WHERE {SqlScope.TenantFilter("d")}
                ORDER BY d.Name
                """, SqlScope.Tenant(user), ct);
    }
}

public static class SaveDepartment
{
    public record Command(int? Id, DepartmentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(150);
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var d = c.Data;
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(new
            {
                c.Id,
                d.Name,
                d.ShortName,
                d.Description,
                d.IsActive,
                // Підрозділ не може бути батьківським сам для себе
                ParentDepartmentId = c.Id is not null && d.ParentDepartmentId == c.Id ? null : d.ParentDepartmentId,
            });

            if (c.Id is null)
            {
                var orgId = user.OrganizationId ?? d.OrganizationId
                    ?? throw new ValidationException([new ValidationFailure("organization", "Оберіть організацію")]);
                p.Add("OrganizationId", orgId);
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Department (OrganizationId, Name, ShortName, Description, IsActive, ParentDepartmentId)
                    VALUES (@OrganizationId, @Name, @ShortName, @Description, @IsActive, @ParentDepartmentId);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, p, ct);
            }

            var affected = await db.ExecuteAsync($"""
                UPDATE d
                SET Name = @Name, ShortName = @ShortName, Description = @Description,
                    IsActive = @IsActive, ParentDepartmentId = @ParentDepartmentId
                FROM dbo.Department d
                WHERE d.Id = @Id AND {SqlScope.TenantFilter("d")}
                """, p, ct);
            if (affected == 0) throw new NotFoundException($"Підрозділ #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}

// Видалення: процедура dbo.usp_Department_Delete перевіряє наявність працівників і дочірніх підрозділів
public static class DeleteDepartment
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            return db.ExecuteProcAsync("dbo.usp_Department_Delete", p, ct);
        }
    }
}
