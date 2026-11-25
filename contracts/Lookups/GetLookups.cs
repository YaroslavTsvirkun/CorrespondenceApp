using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Lookups.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Lookups;

/// <summary>Слайс: усі довідники (один запит — кілька наборів результатів).</summary>
public static class GetLookups
{
    [RequirePermission]
    public record Query : IRequest<LookupsDto>;
}
