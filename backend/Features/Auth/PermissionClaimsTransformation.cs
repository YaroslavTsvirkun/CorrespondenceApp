using System.Security.Claims;
using CorrespondenceApi.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication;

namespace CorrespondenceApi.Features.Auth;

/// <summary>
/// Динамічні права: на кожен запит вантажимо актуальні пермішени користувача з БД
/// і додаємо їх до принципала (зміни прав діють одразу, без перевидання токена).
/// </summary>
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
