using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Auth;

/// <summary>Слайс: дані поточного користувача (ролі, пермішени, прив'язка до працівника й організації).</summary>
public static class Me
{
    /// <param name="TokenId">
    /// Ідентифікатор JWT (claim <c>jti</c>). Якщо задано й токен відкликано (вихід із системи) — 401.
    /// </param>
    [RequirePermission]
    public record Query(string? TokenId = null) : IRequest<UserInfo>;
}
