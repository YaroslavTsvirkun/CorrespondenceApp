using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Auth;

/// <summary>
/// Дані слайсу: поточний користувач. Дані доступу вже завантажені фільтром Data API
/// (<see cref="CurrentUser"/>); якщо передано ідентифікатор токена — перевіряється, що його не відкликано.
/// </summary>
public sealed class MeHandler(CurrentUser current, Db db) : IRequestHandler<Me.Query, UserInfo>
{
    public async Task<UserInfo> Handle(Me.Query q, CancellationToken ct)
    {
        var access = current.Access ?? throw new UnauthorizedException("Користувача не знайдено");

        if (!string.IsNullOrEmpty(q.TokenId)
            && await db.QuerySingleOrDefaultAsync<bool?>(
                "SELECT CAST(1 AS bit) FROM dbo.RevokedToken WHERE TokenId = @TokenId", new { q.TokenId }, ct) is not null)
            throw new UnauthorizedException("Сесію завершено — увійдіть знову");

        return access.ToInfo();
    }
}

/// <summary>Дані слайсу: відкликання JWT поточного користувача (вихід із системи).</summary>
public sealed class RevokeTokenHandler(CurrentUser current, Db db) : IRequestHandler<RevokeToken.Command>
{
    public Task Handle(RevokeToken.Command c, CancellationToken ct) => db.ExecuteAsync(
        """
        DELETE dbo.RevokedToken WHERE ExpiresAtUtc < SYSUTCDATETIME();

        IF @ExpiresAtUtc > SYSUTCDATETIME()
           AND NOT EXISTS (SELECT 1 FROM dbo.RevokedToken WHERE TokenId = @TokenId)
            INSERT dbo.RevokedToken (TokenId, UserId, ExpiresAtUtc) VALUES (@TokenId, @UserId, @ExpiresAtUtc);
        """,
        new { c.TokenId, UserId = current.UserId!.Value, c.ExpiresAtUtc }, ct);
}
