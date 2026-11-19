using CorrespondenceSystem.Contracts.Access.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Access;

/// <summary>Слайс: каталог пермішенів.</summary>
public static class GetPermissions
{
    public record Query : IRequest<IReadOnlyList<PermissionDto>>;
}
