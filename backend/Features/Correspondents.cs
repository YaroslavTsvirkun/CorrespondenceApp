using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            await db.Correspondents.OrderBy(c => c.Name)
                .Select(c => new Dto(c.Id, c.Name, c.CorrespondentTypeId, c.CorrespondentType.Name,
                    c.Address, c.Phone, c.Email, c.ContactPerson, c.Note))
                .ToListAsync(ct);
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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var e = c.Id is int id
                ? await db.Correspondents.FindAsync([id], ct) ?? throw new NotFoundException($"Кореспондента #{id} не знайдено")
                : new Correspondent();
            if (c.Id is null)
                e.OrganizationId = user.OrganizationId
                    ?? throw new ValidationException([new ValidationFailure("organization", "Створення доступне лише в межах організації")]);
            e.CorrespondentTypeId = c.Data.CorrespondentTypeId;
            e.Name = c.Data.Name;
            e.Address = c.Data.Address;
            e.Phone = c.Data.Phone;
            e.Email = c.Data.Email;
            e.ContactPerson = c.Data.ContactPerson;
            e.Note = c.Data.Note;
            if (c.Id is null) db.Correspondents.Add(e);
            await db.SaveChangesAsync(ct);
            return e.Id;
        }
    }
}

public static class DeleteCorrespondent
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var e = await db.Correspondents.FindAsync([c.Id], ct)
                ?? throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
            db.Correspondents.Remove(e); // у документах посилання обнуляться (SET NULL)
            await db.SaveChangesAsync(ct);
        }
    }
}
