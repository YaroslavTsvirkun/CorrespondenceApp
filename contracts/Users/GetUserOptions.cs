using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Users;

/// <summary>Слайс: довідники для форми користувача (організації / ролі / працівники).</summary>
public static class GetUserOptions
{
    public record Query : IRequest<UserOptionsDto>;
}
