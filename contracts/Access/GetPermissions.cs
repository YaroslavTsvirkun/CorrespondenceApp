using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Access.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Access;

/// <summary>Слайс: каталог пермішенів.</summary>
public static class GetPermissions
{
    [RequirePermission(Permissions.UsersManage)]
    public record Query : IRequest<IReadOnlyList<PermissionDto>>;
}
