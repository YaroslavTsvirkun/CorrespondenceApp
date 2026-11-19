using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Dictionaries.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Dictionaries;

/// <summary>Слайс: створення / оновлення запису довідника.</summary>
public static class SaveDictionaryItem
{
    public record Command(string Kind, int? Id, DictionaryItemInput Data) : IRequest<int>;

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
