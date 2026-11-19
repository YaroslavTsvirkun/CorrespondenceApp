using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Users.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Users;

/// <summary>
/// Слайс: створення / оновлення користувача з перевіркою прав адміністратора
/// (запис — процедура dbo.usp_User_Save, ролі — табличний параметр dbo.IdList).
/// </summary>
public static class SaveUser
{
    public record Command(int? Id, UserInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.UserName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Data.FullName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Data.Password)
                .NotEmpty().MinimumLength(5)
                .When(x => x.Id is null)
                .WithMessage("Вкажіть пароль (мінімум 5 символів)");
        }
    }

    private sealed record RoleCheck(int Id, bool IsSuper);

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var isSuper = current.Has(Permissions.OrganizationsManage);
            var d = c.Data;
            var roleIds = (d.RoleIds ?? []).Distinct().ToList();

            static ValidationException Fail(string field, string msg) =>
                new([new ValidationFailure(field, msg)]);

            // Цільова організація: супер-адмін задає вручну (null = ще один супер-адмін),
            // адміністратор організації — завжди своя.
            int? targetOrg = isSuper ? d.OrganizationId : current.OrganizationId;
            if (!isSuper && targetOrg is null)
                throw Fail("organization", "Вашого облікового запису не прив'язано до організації");

            await using (var conn = await db.OpenAsync(ct))
            {
                // Унікальність логіна
                var nameTaken = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AppUser WHERE UserName = @UserName AND Id <> @Id) THEN 1 ELSE 0 END AS bit)",
                    new { d.UserName, Id = c.Id ?? 0 }, cancellationToken: ct));
                if (nameTaken) throw Fail("userName", "Користувач із таким логіном уже існує");

                // Ролі: перевіряємо існування й заборону «супер-» ролей для не-супер-адміна
                var roles = (await conn.QueryAsync<RoleCheck>(new CommandDefinition("""
                    SELECT r.Id, CAST(CASE WHEN s.RoleId IS NULL THEN 0 ELSE 1 END AS bit) AS IsSuper
                    FROM dbo.Role r
                    JOIN @RoleIds x ON x.Id = r.Id
                    LEFT JOIN dbo.fn_RolesWithPermission(@SuperPermission) s ON s.RoleId = r.Id
                    """, new { RoleIds = SqlTvp.IdList(roleIds), SuperPermission = Permissions.OrganizationsManage },
                    cancellationToken: ct))).AsList();
                if (roles.Count != roleIds.Count)
                    throw Fail("roles", "Деяких ролей не знайдено");
                if (!isSuper && roles.Any(r => r.IsSuper))
                    throw Fail("roles", "Недостатньо прав для призначення цієї ролі");

                // Працівник: має належати цільовій організації
                if (d.EmployeeId is int empId)
                {
                    var empOrg = await conn.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
                        "SELECT OrganizationId FROM dbo.Employee WHERE Id = @empId", new { empId }, cancellationToken: ct));
                    if (empOrg is null) throw Fail("employee", "Працівника не знайдено");
                    if (empOrg != targetOrg) throw Fail("employee", "Працівник належить іншій організації");
                }

                // Адміністратор організації не може чіпати чужих/супер-адмінів
                if (c.Id is int id && !isSuper)
                {
                    var userOrg = await conn.QuerySingleOrDefaultAsync<(bool Exists, int? OrganizationId)>(
                        new CommandDefinition("SELECT CAST(1 AS bit), OrganizationId FROM dbo.AppUser WHERE Id = @id",
                            new { id }, cancellationToken: ct));
                    if (!userOrg.Exists) throw new NotFoundException($"Користувача #{id} не знайдено");
                    if (userOrg.OrganizationId != current.OrganizationId)
                        throw Fail("user", "Користувач належить іншій організації");
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
}
