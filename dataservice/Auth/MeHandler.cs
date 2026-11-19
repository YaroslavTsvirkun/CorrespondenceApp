using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using MediatR;

namespace CorrespondenceSystem.DataService.Auth;

/// <summary>
/// Дані слайсу: поточний користувач. Дані доступу вже завантажені фільтром Data API
/// (<see cref="CurrentUser"/>), тож повторного звернення до БД немає.
/// </summary>
public sealed class MeHandler(CurrentUser current) : IRequestHandler<Me.Query, UserInfo>
{
    public Task<UserInfo> Handle(Me.Query q, CancellationToken ct) =>
        Task.FromResult(current.Access?.ToInfo() ?? throw new UnauthorizedException("Користувача не знайдено"));
}
