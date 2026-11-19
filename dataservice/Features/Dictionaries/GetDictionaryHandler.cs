using CorrespondenceSystem.Contracts.Dictionaries;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Dictionaries;

/// <summary>Дані слайсу: записи довідника заданого виду.</summary>
public sealed class GetDictionaryHandler(Db db) : IRequestHandler<GetDictionary.Query, IReadOnlyList<DictionaryItemDto>>
{
    public Task<IReadOnlyList<DictionaryItemDto>> Handle(GetDictionary.Query q, CancellationToken ct)
    {
        var t = DictionaryTables.Resolve(q.Kind);
        var code = t.HasCode ? "Code" : "CAST(NULL AS nvarchar(20)) AS Code";
        return db.QueryAsync<DictionaryItemDto>(
            $"SELECT Id, Name, Description, {code} FROM {t.Name} ORDER BY Name", null, ct);
    }
}
