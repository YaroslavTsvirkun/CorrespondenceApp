using System.Net;
using System.Security.Claims;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using Microsoft.AspNetCore.Authentication;

namespace CorrespondenceApi.Features.Auth;

/// <summary>
/// Динамічні права: на кожен запит отримуємо актуальні пермішени користувача із сервісу даних
/// і додаємо їх до принципала (зміни прав діють одразу, без перевидання токена).
/// </summary>
public class PermissionClaimsTransformation(DataApiClient dataApi, IHttpContextAccessor accessor) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;
        if (principal.HasClaim(c => c.Type == "perm_loaded"))
            return principal;
        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return principal;

        var ct = accessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        UserInfo user;
        try
        {
            // Принципал ще не встановлено в HttpContext — користувача передаємо явно
            user = await dataApi.SendAsync(new Me.Query(), userId, ct);
        }
        catch (DataApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            return principal; // користувача видалено або деактивовано — без пермішенів
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Клієнт розірвав з'єднання (закрив вікно, перейшов на іншу сторінку, AbortController) —
            // відповідь уже нікому не потрібна; без пермішенів запит далі авторизацію не пройде.
            return principal;
        }

        identity.AddClaim(new Claim("perm_loaded", "1"));
        if (user.EmployeeId is int eid) identity.AddClaim(new Claim("employeeId", eid.ToString()));
        if (user.DepartmentId is int did) identity.AddClaim(new Claim("departmentId", did.ToString()));
        if (user.OrganizationId is int orgId) identity.AddClaim(new Claim("organizationId", orgId.ToString()));
        foreach (var code in user.Permissions)
            identity.AddClaim(new Claim("permission", code));
        return principal;
    }
}
