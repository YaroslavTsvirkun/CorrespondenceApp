using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Dictionaries;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Dictionaries;

/// <summary>Дані слайсу: створення / оновлення запису довідника.</summary>
public sealed class SaveDictionaryItemHandler(Db db) : IRequestHandler<SaveDictionaryItem.Command, int>
{
    public async Task<int> Handle(SaveDictionaryItem.Command c, CancellationToken ct)
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
