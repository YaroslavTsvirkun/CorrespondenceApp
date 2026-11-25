using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Users;

/// <summary>Слайс: користувачі (vw_UserAccount) з ролями; адмін організації бачить лише своїх.</summary>
public static class GetUsers
{
    [RequirePermission(Permissions.UsersManage)]
    public record Query : IRequest<IReadOnlyList<UserDto>>;
}
