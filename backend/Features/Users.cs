using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public class Handler(AppDbContext db, ICurrentUser current) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct)
        {
            var query = db.Users.IgnoreQueryFilters().AsNoTracking();
            if (!current.Has(Permissions.OrganizationsManage))
                query = query.Where(u => u.OrganizationId == current.OrganizationId);

            return await query.OrderBy(u => u.FullName)
                .Select(u => new Dto(u.Id, u.UserName, u.FullName, u.OrganizationId,
                    u.Organization != null ? u.Organization.Name : null,
                    u.EmployeeId, u.Employee != null ? u.Employee.FullName : null,
                    u.IsActive, u.OrganizationId == null,
                    u.UserRoles.Select(ur => ur.Role.Name).ToList()))
                .ToListAsync(ct);
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

    public class Handler(AppDbContext db, ICurrentUser current) : IRequestHandler<Query, Dto>
    {
        public async Task<Dto> Handle(Query q, CancellationToken ct)
        {
            var isSuper = current.Has(Permissions.OrganizationsManage);

            var orgsQ = db.Organizations.Where(o => o.IsActive);
            if (!isSuper) orgsQ = orgsQ.Where(o => o.Id == current.OrganizationId);
            var orgs = await orgsQ.OrderBy(o => o.Name)
                .Select(o => new OrgOpt(o.Id, o.Name)).ToListAsync(ct);

            // Роль вважається «супер», якщо надає право керування організаціями
            var roles = await db.Roles.OrderBy(r => r.Name)
                .Select(r => new RoleOpt(r.Id, r.Name,
                    r.RolePermissions.Any(rp => rp.Permission.Code == Permissions.OrganizationsManage)))
                .ToListAsync(ct);
            if (!isSuper) roles = roles.Where(r => !r.IsSuper).ToList();

            // db.Employees вже відфільтровано тенант-фільтром (супер бачить усіх)
            var emps = await db.Employees.OrderBy(e => e.FullName)
                .Select(e => new EmpOpt(e.Id, e.FullName, e.Department.Name, e.OrganizationId))
                .ToListAsync(ct);

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

    public class Handler(AppDbContext db, ICurrentUser current) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var isSuper = current.Has(Permissions.OrganizationsManage);
            var d = c.Data;

            static ValidationException Fail(string field, string msg) =>
                new([new ValidationFailure(field, msg)]);

            // Цільова організація: супер-адмін задає вручну (null = ще один супер-адмін),
            // адміністратор організації — завжди своя.
            int? targetOrg = isSuper ? d.OrganizationId : current.OrganizationId;
            if (!isSuper && targetOrg is null)
                throw Fail("organization", "Вашого облікового запису не прив'язано до організації");

            // Унікальність логіна
            var nameTaken = await db.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.UserName == d.UserName && u.Id != (c.Id ?? 0), ct);
            if (nameTaken) throw Fail("userName", "Користувач із таким логіном уже існує");

            // Ролі: перевіряємо існування й заборону «супер-» ролей для не-супер-адміна
            var roles = await db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .Where(r => d.RoleIds.Contains(r.Id)).ToListAsync(ct);
            if (roles.Count != d.RoleIds.Distinct().Count())
                throw Fail("roles", "Деяких ролей не знайдено");
            if (!isSuper && roles.Any(r => r.RolePermissions.Any(rp => rp.Permission.Code == Permissions.OrganizationsManage)))
                throw Fail("roles", "Недостатньо прав для призначення цієї ролі");

            // Працівник: має належати цільовій організації
            if (d.EmployeeId is int empId)
            {
                var empOrg = await db.Employees.IgnoreQueryFilters()
                    .Where(e => e.Id == empId).Select(e => (int?)e.OrganizationId).FirstOrDefaultAsync(ct);
                if (empOrg is null) throw Fail("employee", "Працівника не знайдено");
                if (empOrg != targetOrg) throw Fail("employee", "Працівник належить іншій організації");
            }

            AppUser user;
            if (c.Id is int id)
            {
                user = await db.Users.IgnoreQueryFilters().Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == id, ct)
                    ?? throw new NotFoundException($"Користувача #{id} не знайдено");
                // Адміністратор організації не може чіпати чужих/супер-адмінів
                if (!isSuper && user.OrganizationId != current.OrganizationId)
                    throw Fail("user", "Користувач належить іншій організації");
                user.UserRoles.Clear();
            }
            else
            {
                user = new AppUser { UserName = d.UserName };
                db.Users.Add(user);
            }

            user.UserName = d.UserName;
            user.FullName = d.FullName;
            user.OrganizationId = targetOrg;
            user.EmployeeId = d.EmployeeId;
            user.IsActive = d.IsActive;
            if (!string.IsNullOrEmpty(d.Password))
                user.PasswordHash = PasswordHasher.Hash(d.Password);

            foreach (var r in roles)
                user.UserRoles.Add(new UserRole { User = user, RoleId = r.Id });

            await db.SaveChangesAsync(ct);
            return user.Id;
        }
    }
}

public static class DeleteUser
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db, ICurrentUser current) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            if (c.Id == current.UserId)
                throw new ValidationException([new ValidationFailure("user", "Не можна видалити власний обліковий запис")]);

            var user = await db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == c.Id, ct)
                ?? throw new NotFoundException($"Користувача #{c.Id} не знайдено");

            var isSuper = current.Has(Permissions.OrganizationsManage);
            if (!isSuper && user.OrganizationId != current.OrganizationId)
                throw new ValidationException([new ValidationFailure("user", "Користувач належить іншій організації")]);

            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
        }
    }
}
