using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Users.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceApi.Features.Users;

/// <summary>Слайс: користувачі (vw_UserAccount) з ролями; адмін організації бачить лише своїх.</summary>
public static class GetUsers
{
    public record Query : IRequest<IReadOnlyList<UserDto>>;

    // Проміжні рядки двох наборів результатів
    private sealed record UserRow(int Id, string UserName, string FullName, int? OrganizationId,
        string? OrganizationName, int? EmployeeId, string? EmployeeName, bool IsActive, bool IsSuperAdmin);
    private sealed record UserRoleRow(int UserId, string Name);

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Query, IReadOnlyList<UserDto>>
    {
        public async Task<IReadOnlyList<UserDto>> Handle(Query q, CancellationToken ct)
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
}
