using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Users;

// Керування користувачами та їх прив'язкою до організації.
// Супер-адмін (право organizations.manage) бачить і редагує всіх, може призначати будь-яку
// організацію та будь-яку роль. Адміністратор організації обмежений своєю організацією:
// бачить лише своїх користувачів, нові створює у власній організації й не може видавати
// «супер-» ролі (ті, що містять organizations.manage).
public record UserInput(string UserName, string? Password, string FullName,
    int? OrganizationId, int? EmployeeId, bool IsActive, List<int> RoleIds);

public static class UsersEndpoints
{
    public static void MapUsers(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/users").WithTags("Users")
            .RequireAuthorization(Permissions.UsersManage);

        g.MapGet("", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetUsers.Query(), ct)));

        g.MapGet("/options", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetUserOptions.Query(), ct)));

        g.MapPost("", async (ISender m, UserInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveUser.Command(null, i), ct);
            return Results.Created($"/api/users/{id}", new { id });
        });

        g.MapPut("/{id:int}", async (ISender m, int id, UserInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveUser.Command(id, i), ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteUser.Command(id), ct);
            return Results.NoContent();
        });
    }
}

public static class GetUsers
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, string UserName, string FullName, int? OrganizationId,
        string? OrganizationName, int? EmployeeId, string? EmployeeName, bool IsActive,
        bool IsSuperAdmin, IReadOnlyList<string> Roles);

    private sealed record UserRow(int Id, string UserName, string FullName, int? OrganizationId,
        string? OrganizationName, int? EmployeeId, string? EmployeeName, bool IsActive, bool IsSuperAdmin);
    private sealed record UserRoleRow(int UserId, string Name);

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct)
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
            return users.Select(u => new Dto(u.Id, u.UserName, u.FullName, u.OrganizationId, u.OrganizationName,
                    u.EmployeeId, u.EmployeeName, u.IsActive, u.IsSuperAdmin, roles[u.Id].ToList()))
                .ToList();
        }
    }
}

// Довідники для форми користувача (організації / ролі / працівники) з урахуванням зони видимості.
public static class GetUserOptions
{
    public record Query : IRequest<Dto>;
    public record OrgOpt(int Id, string Name);
    public record RoleOpt(int Id, string Name, bool IsSuper);
    public record EmpOpt(int Id, string FullName, string DepartmentName, int OrganizationId);
    public record Dto(bool IsSuperAdmin, IReadOnlyList<OrgOpt> Organizations,
        IReadOnlyList<RoleOpt> Roles, IReadOnlyList<EmpOpt> Employees);

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Query, Dto>
    {
        public async Task<Dto> Handle(Query q, CancellationToken ct)
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

            var orgs = (await grid.ReadAsync<OrgOpt>()).AsList();
            var roles = (await grid.ReadAsync<RoleOpt>()).Where(r => isSuper || !r.IsSuper).ToList();
            var emps = (await grid.ReadAsync<EmpOpt>()).AsList();
            return new Dto(isSuper, orgs, roles, emps);
        }
    }
}

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

public static class DeleteUser
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            if (c.Id == current.UserId)
                throw new ValidationException([new ValidationFailure("user", "Не можна видалити власний обліковий запис")]);

            var user = await db.QuerySingleOrDefaultAsync<(bool Exists, int? OrganizationId)>(
                "SELECT CAST(1 AS bit), OrganizationId FROM dbo.AppUser WHERE Id = @Id", new { c.Id }, ct);
            if (!user.Exists) throw new NotFoundException($"Користувача #{c.Id} не знайдено");

            var isSuper = current.Has(Permissions.OrganizationsManage);
            if (!isSuper && user.OrganizationId != current.OrganizationId)
                throw new ValidationException([new ValidationFailure("user", "Користувач належить іншій організації")]);

            await db.ExecuteAsync("DELETE dbo.AppUser WHERE Id = @Id", new { c.Id }, ct);
        }
    }
}
