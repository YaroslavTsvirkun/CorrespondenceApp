using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceSystem.Contracts.Access;

/// <summary>Слайс: видалення ролі (dbo.usp_Role_Delete забороняє видаляти призначену роль).</summary>
public static class DeleteRole
{
    [RequirePermission(Permissions.OrganizationsManage)]
    public record Command(int Id) : IRequest;
}
