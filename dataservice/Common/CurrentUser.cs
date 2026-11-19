using System.Security.Cryptography;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Auth;
using FluentValidation;
using FluentValidation.Results;

namespace CorrespondenceSystem.DataService.Common;

/// <summary>Користувач, від імені якого backend викликає операцію Data API.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    int? EmployeeId { get; }
    int? DepartmentId { get; }
    int? OrganizationId { get; }
    bool IsAuthenticated { get; }
    bool Has(string permission);
}

/// <summary>
/// Поточний користувач запиту. Заповнюється фільтром Data API із заголовка <c>X-User-Id</c>:
/// актуальні дані доступу (працівник, підрозділ, організація, пермішени) щоразу читаються з БД,
/// тож зміни прав діють одразу. Неактивний або невідомий користувач — анонімний.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private HashSet<string> _permissions = new(StringComparer.Ordinal);

    internal UserAccess? Access { get; private set; }

    public int? UserId => Access?.User.Id;
    public int? EmployeeId => Access?.User.EmployeeId;
    public int? DepartmentId => Access?.User.DepartmentId;
    public int? OrganizationId => Access?.User.OrganizationId;
    public bool IsAuthenticated => Access is not null;
    public bool Has(string permission) => _permissions.Contains(permission);

    internal void Set(UserAccess access)
    {
        Access = access;
        _permissions = new HashSet<string>(access.Permissions, StringComparer.Ordinal);
    }
}

// ---- Зона видимості документів (за пермішенами поточного користувача) ----
// Рівень передається у скалярну функцію БД dbo.fn_CanReadDocument:
//   3 — уся кореспонденція, 2 — свого підрозділу, 1 — лише задіяні документи, 0 — немає доступу.
public static class DocumentScope
{
    public static byte Level(ICurrentUser u)
    {
        if (u.Has(Permissions.DocumentsReadAll)) return 3;
        if (u.Has(Permissions.DocumentsReadDepartment) && u.DepartmentId is not null) return 2;
        if (u.Has(Permissions.DocumentsReadInvolved) && u.EmployeeId is not null) return 1;
        return 0;
    }
}

/// <summary>Організація поточного користувача для налаштувань рівня організації.</summary>
public static class OrganizationScope
{
    public static int RequireOrg(ICurrentUser user) =>
        user.OrganizationId ?? throw new ValidationException([new ValidationFailure("organization",
            "Супер-адміністратор не прив'язаний до організації — задайте патерн у картці організації")]);
}

// ---- Хешування паролів (PBKDF2, без зовнішніх пакетів) ----
public static class PasswordHasher
{
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
