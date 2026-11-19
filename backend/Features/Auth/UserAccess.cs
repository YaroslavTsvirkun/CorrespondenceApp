using CorrespondenceApi.Features.Auth.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;

namespace CorrespondenceApi.Features.Auth;

// Рядок користувача з процедури dbo.usp_User_GetAccess (внутрішній, містить хеш пароля)
internal sealed record UserAccessRow(int Id, string UserName, string PasswordHash, string FullName,
    int? EmployeeId, int? DepartmentId, int? OrganizationId, string? OrganizationName, bool IsActive);

/// <summary>Дані доступу користувача: процедура dbo.usp_User_GetAccess (користувач + ролі + пермішени).</summary>
internal sealed record UserAccess(UserAccessRow User, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)
{
    public UserInfo ToInfo() => new(User.Id, User.UserName, User.FullName, User.EmployeeId,
        User.DepartmentId, User.OrganizationId, User.OrganizationName, Roles, Permissions);

    public static async Task<UserAccess?> LoadAsync(Db db, int? userId, string? userName, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        using var grid = await c.QueryMultipleAsync(Db.Proc("dbo.usp_User_GetAccess",
            new { UserId = userId, UserName = userName }, ct));
        var user = await grid.ReadSingleOrDefaultAsync<UserAccessRow>();
        var roles = (await grid.ReadAsync<string>()).AsList();
        var permissions = (await grid.ReadAsync<string>()).AsList();
        return user is null ? null : new UserAccess(user, roles, permissions);
    }
}
