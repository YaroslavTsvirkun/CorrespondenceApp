using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Data.SqlClient;

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

/// <summary>Відповідність «вид довідника» → таблиця БД (білий список — захист від SQL-ін'єкцій).</summary>
internal static class DictionaryTables
{
    public sealed record Table(string Name, bool HasCode);

    private static readonly Dictionary<string, Table> Map = new(StringComparer.Ordinal)
    {
        ["document-types"] = new("dbo.DocumentType", true),
        ["channels"] = new("dbo.DeliveryChannel", false),
        ["correspondent-types"] = new("dbo.CorrespondentType", false),
    };

    public static Table Resolve(string kind) =>
        Map.TryGetValue(kind, out var t) ? t : throw new NotFoundException($"Невідомий довідник: {kind}");
}

public static class GetDictionary
{
    public record Query(string Kind) : IRequest<IReadOnlyList<DictDto>>;

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<DictDto>>
    {
        public Task<IReadOnlyList<DictDto>> Handle(Query q, CancellationToken ct)
        {
            var t = DictionaryTables.Resolve(q.Kind);
            var code = t.HasCode ? "Code" : "CAST(NULL AS nvarchar(20)) AS Code";
            return db.QueryAsync<DictDto>($"SELECT Id, Name, Description, {code} FROM {t.Name} ORDER BY Name", null, ct);
        }
    }
}

public static class SaveDictionary
{
    public record Command(string Kind, int? Id, DictInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(100);
    }

    public class Handler(Db db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var t = DictionaryTables.Resolve(c.Kind);
            var param = new { c.Id, c.Data.Name, c.Data.Description, c.Data.Code };

            if (c.Id is null)
                return await db.ScalarAsync<int>(t.HasCode
                    ? $"INSERT {t.Name} (Name, Description, Code) VALUES (@Name, @Description, @Code); SELECT CAST(SCOPE_IDENTITY() AS int);"
                    : $"INSERT {t.Name} (Name, Description) VALUES (@Name, @Description); SELECT CAST(SCOPE_IDENTITY() AS int);",
                    param, ct);

            var affected = await db.ExecuteAsync(t.HasCode
                ? $"UPDATE {t.Name} SET Name = @Name, Description = @Description, Code = @Code WHERE Id = @Id"
                : $"UPDATE {t.Name} SET Name = @Name, Description = @Description WHERE Id = @Id",
                param, ct);
            if (affected == 0) throw new NotFoundException($"Запис #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}

public static class DeleteDictionary
{
    public record Command(string Kind, int Id) : IRequest;

    public class Handler(Db db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var t = DictionaryTables.Resolve(c.Kind);
            try
            {
                var affected = await db.ExecuteAsync($"DELETE {t.Name} WHERE Id = @Id", new { c.Id }, ct);
                if (affected == 0) throw new NotFoundException($"Запис #{c.Id} не знайдено");
            }
            catch (SqlException ex) when (SqlErrors.IsReferenceViolation(ex))
            {
                throw new ValidationException(
                    [new ValidationFailure("dictionary", "Запис використовується — видалення заборонено")]);
            }
        }
    }
}
