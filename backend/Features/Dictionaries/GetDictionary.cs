using CorrespondenceApi.Features.Dictionaries.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Dictionaries;

/// <summary>Слайс: записи довідника заданого виду.</summary>
public static class GetDictionary
{
    public record Query(string Kind) : IRequest<IReadOnlyList<DictionaryItemDto>>;

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<DictionaryItemDto>>
    {
        public Task<IReadOnlyList<DictionaryItemDto>> Handle(Query q, CancellationToken ct)
        {
            var t = DictionaryTables.Resolve(q.Kind);
            var code = t.HasCode ? "Code" : "CAST(NULL AS nvarchar(20)) AS Code";
            return db.QueryAsync<DictionaryItemDto>(
                $"SELECT Id, Name, Description, {code} FROM {t.Name} ORDER BY Name", null, ct);
        }
    }
}
