using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Auth;

/// <summary>Налаштування блокування облікового запису після невдалих спроб входу (секція <c>Security:Lockout</c>).</summary>
public sealed class LockoutOptions
{
    /// <summary>Скільки невдалих спроб поспіль допускається до блокування.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>Тривалість блокування, хв.</summary>
    public int LockoutMinutes { get; set; } = 15;
}
/// <summary>
/// Дані слайсу: перевірка логіна й пароля (хеш PBKDF2 з dbo.AppUser не залишає сервіс).
/// Після <see cref="LockoutOptions.MaxFailedAttempts"/> невдалих спроб поспіль обліковий запис
/// блокується на <see cref="LockoutOptions.LockoutMinutes"/> хв (захист від перебору паролів;
/// частоту запитів з однієї адреси додатково обмежує backend).
/// Відповідь на будь-яку невдачу однакова (невідомий логін, неправильний пароль, блокування,
/// неактивний запис), і хеш пароля обчислюється в кожному випадку — ні текст, ні час відповіді
/// не показують, чи існує логін і чи заблоковано його.
/// </summary>
public sealed class VerifyCredentialsHandler(Db db, LockoutOptions lockout, ILogger<VerifyCredentialsHandler> logger)
    : IRequestHandler<VerifyCredentials.Query, UserInfo>
{
    internal const string FailedMessage =
        "Невірний логін або пароль. Після кількох невдалих спроб вхід тимчасово блокується";

    // Хеш для невідомого логіна: перевірка займає стільки ж часу, скільки для справжнього користувача
    private static readonly string DummyHash = PasswordHasher.Hash(Guid.NewGuid().ToString());

    private sealed record LockoutState(int FailedLoginCount, DateTime? LockoutEndUtc);

    public async Task<UserInfo> Handle(VerifyCredentials.Query q, CancellationToken ct)
    {
        var access = await UserAccess.LoadAsync(db, null, q.UserName, ct);
        if (access is null)
        {
            PasswordHasher.Verify(q.Password, DummyHash);
            throw new UnauthorizedException(FailedMessage);
        }

        var userId = access.User.Id;
        var state = await db.QuerySingleOrDefaultAsync<LockoutState>(
            "SELECT FailedLoginCount, LockoutEndUtc FROM dbo.AppUser WHERE Id = @userId", new { userId }, ct);
        if (state?.LockoutEndUtc is { } until && until > DateTime.UtcNow)
        {
            // Пароль не перевіряємо (навіть правильний не приймається), але час відповіді — той самий
            PasswordHasher.Verify(q.Password, DummyHash);
            logger.LogWarning("Спроба входу в заблокований обліковий запис #{UserId}", userId);
            throw new UnauthorizedException(FailedMessage);
        }

        if (!PasswordHasher.Verify(q.Password, access.User.PasswordHash))
        {
            // Атомарно: +1 до лічильника; на порозі — блокування і скидання лічильника
            var lockedUntil = await db.ScalarAsync<DateTime?>(
                """
                UPDATE dbo.AppUser
                SET FailedLoginCount = IIF(FailedLoginCount + 1 >= @max, 0, FailedLoginCount + 1),
                    LockoutEndUtc    = IIF(FailedLoginCount + 1 >= @max,
                                           DATEADD(minute, @minutes, SYSUTCDATETIME()), LockoutEndUtc)
                OUTPUT inserted.LockoutEndUtc
                WHERE Id = @userId;
                """,
                new { userId, max = Math.Max(1, lockout.MaxFailedAttempts), minutes = lockout.LockoutMinutes }, ct);
            if (lockedUntil is { } u && u > DateTime.UtcNow)
                logger.LogWarning("Обліковий запис #{UserId} заблоковано до {Until:u} після невдалих спроб входу", userId, u);
            throw new UnauthorizedException(FailedMessage);
        }

        if (!access.User.IsActive)
            throw new UnauthorizedException(FailedMessage);

        if (state is { FailedLoginCount: > 0 } or { LockoutEndUtc: not null })
            await db.ExecuteAsync(
                "UPDATE dbo.AppUser SET FailedLoginCount = 0, LockoutEndUtc = NULL WHERE Id = @userId",
                new { userId }, ct);

        return access.ToInfo();
    }
}
