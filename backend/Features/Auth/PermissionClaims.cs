using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CorrespondenceApi.Features.Auth;

/// <summary>
/// Динамічні права: після перевірки підпису JWT (подія <c>OnTokenValidated</c>) отримуємо актуальні дані
/// користувача із сервісу даних і додаємо пермішени до принципала — зміни прав діють одразу,
/// без перевидання токена. Якщо користувача видалено чи деактивовано або токен відкликано (вихід) —
/// автентифікація не вдається (401), а не просто «без прав».
/// </summary>
public static class PermissionClaims
{
    public static async Task OnTokenValidated(TokenValidatedContext ctx)
    {
        if (ctx.Principal?.Identity is not ClaimsIdentity identity
            || !int.TryParse(ctx.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            ctx.Fail("Токен без ідентифікатора користувача");
            return;
        }

        var dataApi = ctx.HttpContext.RequestServices.GetRequiredService<DataApiClient>();
        var ct = ctx.HttpContext.RequestAborted;
        UserInfo user;
        try
        {
            // Принципал ще не встановлено в HttpContext — користувача передаємо явно
            user = await dataApi.SendAsync(new Me.Query(TokenId(ctx.Principal)), userId, ct);
        }
        catch (DataApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            ctx.Fail("Користувача деактивовано або сесію завершено");
            return;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Клієнт розірвав з'єднання — відповідь уже нікому не потрібна
            ctx.Fail("Запит скасовано клієнтом");
            return;
        }

        if (user.EmployeeId is int eid) identity.AddClaim(new Claim("employeeId", eid.ToString()));
        if (user.DepartmentId is int did) identity.AddClaim(new Claim("departmentId", did.ToString()));
        if (user.OrganizationId is int orgId) identity.AddClaim(new Claim("organizationId", orgId.ToString()));
        foreach (var code in user.Permissions)
            identity.AddClaim(new Claim("permission", code));
    }

    /// <summary>Ідентифікатор токена (jti); токени, видані до появи jti, його не мають.</summary>
    public static string? TokenId(ClaimsPrincipal principal) =>
        principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
}
