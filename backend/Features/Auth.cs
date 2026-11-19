using System.Security.Claims;
using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;

namespace CorrespondenceApi.Features;

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

// Дані доступу користувача: процедура dbo.usp_User_GetAccess (користувач + ролі + пермішени)
internal sealed record UserAccessRow(int Id, string UserName, string PasswordHash, string FullName,
    int? EmployeeId, int? DepartmentId, int? OrganizationId, string? OrganizationName, bool IsActive);

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

    public class Handler(Db db, TokenService tokens) : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command c, CancellationToken ct)
        {
            var access = await UserAccess.LoadAsync(db, null, c.UserName, ct);

            if (access is null || !access.User.IsActive || !PasswordHasher.Verify(c.Password, access.User.PasswordHash))
                throw new UnauthorizedException("Невірний логін або пароль");

            var u = access.User;
            return new Response(tokens.Create(u.Id, u.UserName, u.FullName), access.ToInfo());
        }
    }
}

public static class Me
{
    public record Query : IRequest<UserInfo>;

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Query, UserInfo>
    {
        public async Task<UserInfo> Handle(Query q, CancellationToken ct)
        {
            var access = current.UserId is int id ? await UserAccess.LoadAsync(db, id, null, ct) : null;
            return access?.ToInfo() ?? throw new UnauthorizedException("Користувача не знайдено");
        }
    }
}

// Динамічні права: на кожен запит вантажимо актуальні пермішени користувача з БД
// і додаємо їх до принципала (зміни прав діють одразу, без перевидання токена).
public class PermissionClaimsTransformation(Db db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;
        if (principal.HasClaim(c => c.Type == "perm_loaded"))
            return principal;
        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return principal;

        var access = await UserAccess.LoadAsync(db, userId, null, CancellationToken.None);
        if (access is null || !access.User.IsActive) return principal;

        identity.AddClaim(new Claim("perm_loaded", "1"));
        var u = access.User;
        if (u.EmployeeId is int eid) identity.AddClaim(new Claim("employeeId", eid.ToString()));
        if (u.DepartmentId is int did) identity.AddClaim(new Claim("departmentId", did.ToString()));
        if (u.OrganizationId is int orgId) identity.AddClaim(new Claim("organizationId", orgId.ToString()));
        foreach (var code in access.Permissions)
            identity.AddClaim(new Claim("permission", code));
        return principal;
    }
}
