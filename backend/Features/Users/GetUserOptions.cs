using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Users.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceApi.Features.Users;

/// <summary>Слайс: довідники для форми користувача (організації / ролі / працівники).</summary>
public static class GetUserOptions
{
    public record Query : IRequest<UserOptionsDto>;

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Query, UserOptionsDto>
    {
        public async Task<UserOptionsDto> Handle(Query q, CancellationToken ct)
        {
            var isSuper = current.Has(Permissions.OrganizationsManage);
            var p = SqlScope.Tenant(current);
            p.Add("SuperPermission", Permissions.OrganizationsManage);

            await using var c = await db.OpenAsync(ct);
            using var grid = await c.QueryMultipleAsync(new CommandDefinition("""
                SELECT o.Id, o.Name
                FROM dbo.Organization o
                WHERE o.IsActive = 1 AND (@Bypass = 1 OR o.Id = @TenantOrganizationId)
                ORDER BY o.Name;

                -- Роль вважається «супер», якщо надає право керування організаціями
                SELECT r.Id, r.Name, CAST(CASE WHEN s.RoleId IS NULL THEN 0 ELSE 1 END AS bit) AS IsSuper
                FROM dbo.Role r
                LEFT JOIN dbo.fn_RolesWithPermission(@SuperPermission) s ON s.RoleId = r.Id
                ORDER BY r.Name;

                SELECT e.Id, e.FullName, d.Name AS DepartmentName, e.OrganizationId
                FROM dbo.Employee e
                JOIN dbo.Department d ON d.Id = e.DepartmentId
                WHERE (@Bypass = 1 OR e.OrganizationId = @TenantOrganizationId)
                ORDER BY e.FullName;
                """, p, cancellationToken: ct));

            var orgs = (await grid.ReadAsync<OrganizationOption>()).AsList();
            var roles = (await grid.ReadAsync<RoleOption>()).Where(r => isSuper || !r.IsSuper).ToList();
            var emps = (await grid.ReadAsync<EmployeeOption>()).AsList();
            return new UserOptionsDto(isSuper, orgs, roles, emps);
        }
    }
}
