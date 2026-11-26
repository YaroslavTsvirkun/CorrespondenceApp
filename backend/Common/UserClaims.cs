using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CorrespondenceApi.Common;

/// <summary>Claim-и користувача (з JWT і завантажені із сервісу даних) і доступ до них.</summary>
public static class UserClaims
{
    public const string FullName = "fullName";
    public const string Permission = "permission";
    public const string EmployeeId = "employeeId";
    public const string DepartmentId = "departmentId";
    public const string OrganizationId = "organizationId";

    /// <summary>Id користувача або null (анонімний запит чи токен без ідентифікатора).</summary>
    public static int? FindUserId(this ClaimsPrincipal? user) =>
        int.TryParse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>Id автентифікованого користувача (для ендпоінтів з RequireAuthorization).</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        user.FindUserId() ?? throw new InvalidOperationException("Запит без ідентифікатора користувача");

    /// <summary>Ідентифікатор токена (jti); токени, видані до появи jti, його не мають.</summary>
    public static string? FindTokenId(this ClaimsPrincipal user) =>
        user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    public static bool HasPermission(this ClaimsPrincipal user, string permission) =>
        user.HasClaim(Permission, permission);

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirst(FullName)?.Value ?? user.Identity?.Name ?? "";
}
