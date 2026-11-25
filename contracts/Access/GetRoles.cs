using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Access.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Access;

/// <summary>Слайс: ролі з кількістю користувачів і пермішенами.</summary>
public static class GetRoles
{
    [RequirePermission(Permissions.UsersManage)]
    public record Query : IRequest<IReadOnlyList<RoleDto>>;
}
