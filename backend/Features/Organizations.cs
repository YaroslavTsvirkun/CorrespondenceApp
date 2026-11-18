using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

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

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<Dto>("""
                SELECT o.Id, o.Name, o.Code, o.ParentOrganizationId, o.IsActive, o.RegistrationNumberPattern,
                       (SELECT COUNT(*) FROM dbo.Department d WHERE d.OrganizationId = o.Id) AS DepartmentCount,
                       (SELECT COUNT(*) FROM dbo.AppUser u WHERE u.OrganizationId = o.Id) AS UserCount
                FROM dbo.Organization o
                ORDER BY o.Name
                """, null, ct);
    }
}

public static class SaveOrganization
{
    public record Command(int? Id, OrganizationInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
    }

    public class Handler(Db db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var d = c.Data;
            var param = new
            {
                c.Id,
                d.Name,
                d.Code,
                d.IsActive,
                // Організація не може бути батьківською сама для себе
                ParentOrganizationId = c.Id is not null && d.ParentOrganizationId == c.Id ? null : d.ParentOrganizationId,
                Pattern = string.IsNullOrWhiteSpace(d.RegistrationNumberPattern) ? null : d.RegistrationNumberPattern.Trim(),
            };

            if (c.Id is null)
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Organization (Name, Code, IsActive, ParentOrganizationId, RegistrationNumberPattern)
                    VALUES (@Name, @Code, @IsActive, @ParentOrganizationId, ISNULL(@Pattern, N'{type}-{seq:000}/{year}'));
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, param, ct);

            var affected = await db.ExecuteAsync("""
                UPDATE dbo.Organization
                SET Name = @Name, Code = @Code, IsActive = @IsActive,
                    ParentOrganizationId = @ParentOrganizationId,
                    RegistrationNumberPattern = ISNULL(@Pattern, RegistrationNumberPattern)
                WHERE Id = @Id
                """, param, ct);
            if (affected == 0) throw new NotFoundException($"Організацію #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}

// Видалення: процедура dbo.usp_Organization_Delete перевіряє залежності
public static class DeleteOrganization
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct) =>
            db.ExecuteProcAsync("dbo.usp_Organization_Delete", new { c.Id }, ct);
    }
}
