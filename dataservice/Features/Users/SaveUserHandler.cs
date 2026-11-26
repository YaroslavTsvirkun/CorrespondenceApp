using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users;
using CorrespondenceSystem.Contracts.Users.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Users;

/// <summary>
/// Дані слайсу: створення / оновлення користувача з перевіркою прав адміністратора
/// (запис — процедура dbo.usp_User_Save, ролі — табличний параметр dbo.IdList).
/// </summary>
public sealed class SaveUserHandler(Db db, ICurrentUser current) : IRequestHandler<SaveUser.Command, int>
{
    private sealed record RoleCheck(int Id, bool IsSuper);

    public async Task<int> Handle(SaveUser.Command c, CancellationToken ct)
    {
        var isSuper = current.Has(Permissions.OrganizationsManage);
        var d = c.Data;
        var roleIds = (d.RoleIds ?? []).Distinct().ToList();

        // Цільова організація: супер-адмін задає вручну (null = ще один супер-адмін),
        // адміністратор організації — завжди своя.
        int? targetOrg = isSuper ? d.OrganizationId : current.OrganizationId;
        if (!isSuper && targetOrg is null)
            throw Errors.Validation("organization", "Вашого облікового запису не прив'язано до організації");

        await using (var conn = await db.OpenAsync(ct))
        {
            // Унікальність логіна
            var nameTaken = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AppUser WHERE UserName = @UserName AND Id <> @Id) THEN 1 ELSE 0 END AS bit)",
                new { d.UserName, Id = c.Id ?? 0 }, cancellationToken: ct));
            if (nameTaken) throw Errors.Validation("userName", "Користувач із таким логіном уже існує");

            // Ролі: перевіряємо існування й заборону «супер-» ролей для не-супер-адміна
            var roles = (await conn.QueryAsync<RoleCheck>(new CommandDefinition("""
                SELECT r.Id, CAST(CASE WHEN s.RoleId IS NULL THEN 0 ELSE 1 END AS bit) AS IsSuper
                FROM dbo.Role r
                JOIN @RoleIds x ON x.Id = r.Id
                LEFT JOIN dbo.fn_RolesWithPermission(@SuperPermission) s ON s.RoleId = r.Id
                """, new { RoleIds = SqlTvp.IdList(roleIds), SuperPermission = Permissions.OrganizationsManage },
                cancellationToken: ct))).AsList();
            if (roles.Count != roleIds.Count)
                throw Errors.Validation("roles", "Деяких ролей не знайдено");
            if (!isSuper && roles.Any(r => r.IsSuper))
                throw Errors.Validation("roles", "Недостатньо прав для призначення цієї ролі");

            // Працівник: має належати цільовій організації
            if (d.EmployeeId is int empId)
            {
                var empOrg = await conn.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
                    "SELECT OrganizationId FROM dbo.Employee WHERE Id = @empId", new { empId }, cancellationToken: ct));
                if (empOrg is null) throw Errors.Validation("employee", "Працівника не знайдено");
                if (empOrg != targetOrg) throw Errors.Validation("employee", "Працівник належить іншій організації");
            }

            // Адміністратор організації не може чіпати чужих/супер-адмінів
            if (c.Id is int id && !isSuper)
            {
                var userOrg = await conn.QuerySingleOrDefaultAsync<(bool Exists, int? OrganizationId)>(
                    new CommandDefinition("SELECT CAST(1 AS bit), OrganizationId FROM dbo.AppUser WHERE Id = @id",
                        new { id }, cancellationToken: ct));
                if (!userOrg.Exists) throw new NotFoundException($"Користувача #{id} не знайдено");
                if (userOrg.OrganizationId != current.OrganizationId)
                    throw Errors.Validation("user", "Користувач належить іншій організації");
            }
        }

        return await db.ScalarProcAsync<int>("dbo.usp_User_Save", new
        {
            c.Id,
            d.UserName,
            PasswordHash = string.IsNullOrEmpty(d.Password) ? null : PasswordHasher.Hash(d.Password),
            d.FullName,
            OrganizationId = targetOrg,
            d.EmployeeId,
            d.IsActive,
            RoleIds = SqlTvp.IdList(roleIds),
        }, ct);
    }
}
