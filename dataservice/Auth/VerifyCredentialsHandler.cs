using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Auth;

/// <summary>Дані слайсу: перевірка логіна й пароля (хеш PBKDF2 з dbo.AppUser не залишає сервіс).</summary>
public sealed class VerifyCredentialsHandler(Db db) : IRequestHandler<VerifyCredentials.Query, UserInfo>
{
    public async Task<UserInfo> Handle(VerifyCredentials.Query q, CancellationToken ct)
    {
        var access = await UserAccess.LoadAsync(db, null, q.UserName, ct);

        if (access is null || !access.User.IsActive
            || !PasswordHasher.Verify(q.Password, access.User.PasswordHash))
            throw new UnauthorizedException("Невірний логін або пароль");

        return access.ToInfo();
    }
}
