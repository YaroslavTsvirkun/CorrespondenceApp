using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Correspondents;

public record CorrespondentInput(int CorrespondentTypeId, string Name, string? Address,
    string? Phone, string? Email, string? ContactPerson, string? Note);

public static class CorrespondentsEndpoints
{
    public static void MapCorrespondents(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/correspondents").WithTags("Correspondents");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetCorrespondents.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, CorrespondentInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveCorrespondent.Command(null, i), ct);
            return Results.Created($"/api/correspondents/{id}", new { id });
        }).RequireAuthorization(Permissions.CorrespondentsManage);

        g.MapPut("/{id:int}", async (ISender m, int id, CorrespondentInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveCorrespondent.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.CorrespondentsManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteCorrespondent.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.CorrespondentsManage);
    }
}

public static class GetCorrespondents
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string Name, int CorrespondentTypeId, string TypeName,
        string? Address, string? Phone, string? Email, string? ContactPerson, string? Note);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<Dto>($"""
                SELECT c.Id, c.Name, c.CorrespondentTypeId, t.Name AS TypeName,
                       c.Address, c.Phone, c.Email, c.ContactPerson, c.Note
                FROM dbo.Correspondent c
                JOIN dbo.CorrespondentType t ON t.Id = c.CorrespondentTypeId
                WHERE {SqlScope.TenantFilter("c")}
                ORDER BY c.Name
                """, SqlScope.Tenant(user), ct);
    }
}

public static class SaveCorrespondent
{
    public record Command(int? Id, CorrespondentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Data.CorrespondentTypeId).GreaterThan(0).WithMessage("Оберіть тип кореспондента");
        }
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(c.Data);
            p.Add("Id", c.Id);

            if (c.Id is null)
            {
                var orgId = user.OrganizationId
                    ?? throw new ValidationException([new ValidationFailure("organization", "Створення доступне лише в межах організації")]);
                p.Add("OrganizationId", orgId);
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Correspondent (OrganizationId, CorrespondentTypeId, Name, Address, Phone, Email, ContactPerson, Note)
                    VALUES (@OrganizationId, @CorrespondentTypeId, @Name, @Address, @Phone, @Email, @ContactPerson, @Note);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, p, ct);
            }

            var affected = await db.ExecuteAsync($"""
                UPDATE c
                SET CorrespondentTypeId = @CorrespondentTypeId, Name = @Name, Address = @Address,
                    Phone = @Phone, Email = @Email, ContactPerson = @ContactPerson, Note = @Note
                FROM dbo.Correspondent c
                WHERE c.Id = @Id AND {SqlScope.TenantFilter("c")}
                """, p, ct);
            if (affected == 0) throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}

public static class DeleteCorrespondent
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            // у документах посилання обнуляться (FK ON DELETE SET NULL)
            var affected = await db.ExecuteAsync(
                $"DELETE c FROM dbo.Correspondent c WHERE c.Id = @Id AND {SqlScope.TenantFilter("c")}", p, ct);
            if (affected == 0) throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
        }
    }
}
