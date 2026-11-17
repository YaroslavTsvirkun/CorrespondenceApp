using System.Security.Claims;
using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Auth;

public record UserInfo(
    int UserId, string UserName, string FullName, int? EmployeeId, int? DepartmentId,
    int? OrganizationId, string? OrganizationName, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public static class AuthEndpoints
{
    public static void MapAuth(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/login", async (ISender m, Login.Command cmd, CancellationToken ct) =>
            Results.Ok(await m.Send(cmd, ct)))
            .AllowAnonymous();

        g.MapGet("/me", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new Me.Query(), ct)))
            .RequireAuthorization();
    }
}

public static class Login
{
    public record Command(string UserName, string Password) : IRequest<Response>;
    public record Response(string Token, UserInfo User);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class Handler(AppDbContext db, TokenService tokens) : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command c, CancellationToken ct)
        {
            var user = await db.Users.IgnoreQueryFilters()
                .Include(u => u.Employee)
                .Include(u => u.Organization)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserName == c.UserName, ct);

            if (user is null || !user.IsActive || !PasswordHasher.Verify(c.Password, user.PasswordHash))
                throw new UnauthorizedException("Невірний логін або пароль");

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct().ToList();

            var info = new UserInfo(user.Id, user.UserName, user.FullName,
                user.EmployeeId, user.Employee?.DepartmentId, user.OrganizationId,
                user.Organization?.Name, roles, permissions);

            return new Response(tokens.Create(user), info);
        }
    }
}

public static class Me
{
    public record Query : IRequest<UserInfo>;

    public class Handler(AppDbContext db, ICurrentUser current) : IRequestHandler<Query, UserInfo>
    {
        public async Task<UserInfo> Handle(Query q, CancellationToken ct)
        {
            var user = await db.Users.IgnoreQueryFilters()
                .Include(u => u.Employee)
                .Include(u => u.Organization)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == current.UserId, ct)
                ?? throw new UnauthorizedException("Користувача не знайдено");

            return new UserInfo(user.Id, user.UserName, user.FullName,
                user.EmployeeId, user.Employee?.DepartmentId, user.OrganizationId,
                user.Organization?.Name,
                user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                user.UserRoles.SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code).Distinct().ToList());
        }
    }
}

// Динамічні права: на кожен запит вантажимо актуальні пермішени користувача з БД
// і додаємо їх до принципала (зміни прав діють одразу, без перевидання токена).
public class PermissionClaimsTransformation(AppDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;
        if (principal.HasClaim(c => c.Type == "perm_loaded"))
            return principal;
        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return principal;

        // IgnoreQueryFilters: тенант-контекст ще не встановлено, вантажимо свого користувача без фільтра
        var data = await db.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new
            {
                u.EmployeeId,
                u.OrganizationId,
                DepartmentId = u.Employee != null ? (int?)u.Employee.DepartmentId : null,
                Permissions = u.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code)
                    .Distinct()
                    .ToList(),
            })
            .FirstOrDefaultAsync();
        if (data is null) return principal;

        identity.AddClaim(new Claim("perm_loaded", "1"));
        if (data.EmployeeId is int eid) identity.AddClaim(new Claim("employeeId", eid.ToString()));
        if (data.DepartmentId is int did) identity.AddClaim(new Claim("departmentId", did.ToString()));
        if (data.OrganizationId is int orgId) identity.AddClaim(new Claim("organizationId", orgId.ToString()));
        foreach (var code in data.Permissions)
            identity.AddClaim(new Claim("permission", code));
        return principal;
    }
}
