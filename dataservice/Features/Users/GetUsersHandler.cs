using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users;
using CorrespondenceSystem.Contracts.Users.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Users;

/// <summary>Дані слайсу: користувачі (vw_UserAccount) з ролями; адмін організації бачить лише своїх.</summary>
public sealed class GetUsersHandler(Db db, ICurrentUser current) : IRequestHandler<GetUsers.Query, IReadOnlyList<UserDto>>
{
    // Проміжні рядки двох наборів результатів
    private sealed record UserRow(int Id, string UserName, string FullName, int? OrganizationId,
        string? OrganizationName, int? EmployeeId, string? EmployeeName, bool IsActive, bool IsSuperAdmin);

    private sealed record UserRoleRow(int UserId, string Name);

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsers.Query q, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        using var grid = await c.QueryMultipleAsync(new CommandDefinition($"""
            SELECT u.Id, u.UserName, u.FullName, u.OrganizationId, u.OrganizationName,
                   u.EmployeeId, u.EmployeeName, u.IsActive, u.IsSuperAdmin
            FROM dbo.vw_UserAccount u
            WHERE {SqlScope.TenantFilter("u")}
            ORDER BY u.FullName;

            SELECT ur.UserId, r.Name
            FROM dbo.UserRole ur
            JOIN dbo.Role r ON r.Id = ur.RoleId;
            """, SqlScope.Tenant(current), cancellationToken: ct));

        var users = await grid.ReadAsync<UserRow>();
        var roles = (await grid.ReadAsync<UserRoleRow>()).ToLookup(x => x.UserId, x => x.Name);
        return users.Select(u => new UserDto(u.Id, u.UserName, u.FullName, u.OrganizationId, u.OrganizationName,
                u.EmployeeId, u.EmployeeName, u.IsActive, u.IsSuperAdmin, roles[u.Id].ToList()))
            .ToList();
    }
}
