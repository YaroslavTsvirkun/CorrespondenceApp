using CorrespondenceSystem.Contracts.Auth.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Auth;

/// <summary>Слайс: дані поточного користувача (ролі, пермішени, прив'язка до працівника й організації).</summary>
public static class Me
{
    public record Query : IRequest<UserInfo>;
}
