using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Dictionaries;

/// <summary>Слайс: записи довідника заданого виду.</summary>
public static class GetDictionary
{
    [RequirePermission]
    public record Query(string Kind) : IRequest<IReadOnlyList<DictionaryItemDto>>;
}
