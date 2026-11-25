using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Users;

/// <summary>Слайс: видалення користувача (не себе; адмін організації — лише своїх).</summary>
public static class DeleteUser
{
    [RequirePermission(Permissions.UsersManage)]
    public record Command(int Id) : IRequest;
}
