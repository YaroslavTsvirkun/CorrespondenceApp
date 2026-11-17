using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Dictionaries;

// Узагальнений CRUD для простих довідників {id, name, description}:
//   kind = document-types | channels | correspondent-types
// Code заповнюється лише для document-types (використовується у патерні номера {type})
public record DictInput(string Name, string? Description, string? Code);
public record DictDto(int Id, string Name, string? Description, string? Code);

public static class DictionariesEndpoints
{
    public static void MapDictionaries(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/dictionaries").WithTags("Dictionaries");

        g.MapGet("/{kind}", async (ISender m, string kind, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDictionary.Query(kind), ct))).RequireAuthorization();

        g.MapPost("/{kind}", async (ISender m, string kind, DictInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveDictionary.Command(kind, null, i), ct);
            return Results.Created($"/api/dictionaries/{kind}/{id}", new { id });
        }).RequireAuthorization(Permissions.DictionariesManage);

        g.MapPut("/{kind}/{id:int}", async (ISender m, string kind, int id, DictInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveDictionary.Command(kind, id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DictionariesManage);

        g.MapDelete("/{kind}/{id:int}", async (ISender m, string kind, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteDictionary.Command(kind, id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DictionariesManage);
    }
}

public static class GetDictionary
{
    public record Query(string Kind) : IRequest<IReadOnlyList<DictDto>>;

    public class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<DictDto>>
    {
        public async Task<IReadOnlyList<DictDto>> Handle(Query q, CancellationToken ct) => q.Kind switch
        {
            "document-types" => await db.DocumentTypes.OrderBy(x => x.Name)
                .Select(x => new DictDto(x.Id, x.Name, x.Description, x.Code)).ToListAsync(ct),
            "channels" => await db.Channels.OrderBy(x => x.Name)
                .Select(x => new DictDto(x.Id, x.Name, x.Description, null)).ToListAsync(ct),
            "correspondent-types" => await db.CorrespondentTypes.OrderBy(x => x.Name)
                .Select(x => new DictDto(x.Id, x.Name, x.Description, null)).ToListAsync(ct),
            _ => throw new NotFoundException($"Невідомий довідник: {q.Kind}"),
        };
    }
}

public static class SaveDictionary
{
    public record Command(string Kind, int? Id, DictInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(100);
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            switch (c.Kind)
            {
                case "document-types":
                    return await Upsert(db.DocumentTypes, c, () => new DocumentType(),
                        (e, d) => { e.Name = d.Name; e.Description = d.Description; e.Code = d.Code; }, ct);
                case "channels":
                    return await Upsert(db.Channels, c, () => new DeliveryChannel(),
                        (e, d) => { e.Name = d.Name; e.Description = d.Description; }, ct);
                case "correspondent-types":
                    return await Upsert(db.CorrespondentTypes, c, () => new CorrespondentType(),
                        (e, d) => { e.Name = d.Name; e.Description = d.Description; }, ct);
                default:
                    throw new NotFoundException($"Невідомий довідник: {c.Kind}");
            }
        }

        private async Task<int> Upsert<T>(DbSet<T> set, Command c, Func<T> create,
            Action<T, DictInput> apply, CancellationToken ct) where T : class
        {
            var e = c.Id is int id
                ? await set.FindAsync([id], ct) ?? throw new NotFoundException($"Запис #{id} не знайдено")
                : create();
            apply(e, c.Data);
            if (c.Id is null) set.Add(e);
            await db.SaveChangesAsync(ct);
            return (int)typeof(T).GetProperty("Id")!.GetValue(e)!;
        }
    }
}

public static class DeleteDictionary
{
    public record Command(string Kind, int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            object? e = c.Kind switch
            {
                "document-types" => await db.DocumentTypes.FindAsync([c.Id], ct),
                "channels" => await db.Channels.FindAsync([c.Id], ct),
                "correspondent-types" => await db.CorrespondentTypes.FindAsync([c.Id], ct),
                _ => throw new NotFoundException($"Невідомий довідник: {c.Kind}"),
            };
            if (e is null) throw new NotFoundException($"Запис #{c.Id} не знайдено");
            db.Remove(e);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new ValidationException(
                    [new ValidationFailure("dictionary", "Запис використовується — видалення заборонено")]);
            }
        }
    }
}
