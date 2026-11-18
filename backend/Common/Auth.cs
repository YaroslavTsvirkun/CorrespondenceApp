using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CorrespondenceApi.Common;

// ---- Каталог пермішенів ----
public static class Permissions
{
    public const string DocumentsReadAll = "documents.read.all";
    public const string DocumentsReadDepartment = "documents.read.department";
    public const string DocumentsReadInvolved = "documents.read.involved";
    public const string DocumentsCreate = "documents.create";
    public const string DocumentsUpdate = "documents.update";
    public const string DocumentsDelete = "documents.delete";
    public const string ResolutionsCreate = "resolutions.create";
    public const string TasksAssign = "tasks.assign";
    public const string TasksExecute = "tasks.execute";
    public const string CorrespondentsManage = "correspondents.manage";
    public const string DictionariesManage = "dictionaries.manage";
    public const string EmployeesManage = "employees.manage";
    public const string DepartmentsManage = "departments.manage";
    public const string ReportsView = "reports.view";
    public const string DashboardOverview = "dashboard.overview";
    public const string DashboardControl = "dashboard.control";
    public const string DashboardMyTasks = "dashboard.my-tasks";
    public const string UsersManage = "users.manage";
    public const string OrganizationsManage = "organizations.manage";
    public const string SettingsManage = "settings.manage";

    // Композитні політики (будь-який зі складових пермішенів задовольняє)
    public const string DocumentsReadAny = "documents.read";
    public const string TasksWrite = "tasks.write";

    public static readonly string[] ReadScopes =
        [DocumentsReadAll, DocumentsReadDepartment, DocumentsReadInvolved];

    public static readonly Dictionary<string, string[]> Composite = new()
    {
        [DocumentsReadAny] = ReadScopes,
        [TasksWrite] = [TasksAssign, TasksExecute],
    };

    // Каталог пермішенів (опис, категорія) зберігається в БД: Database/Seed/001_ReferenceData.sql
}

// ---- Поточний користувач (з клеймів JWT) ----
public interface ICurrentUser
{
    int? UserId { get; }
    int? EmployeeId { get; }
    int? DepartmentId { get; }
    int? OrganizationId { get; }
    bool IsAuthenticated { get; }
    bool Has(string permission);
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public int? UserId => GetInt(ClaimTypes.NameIdentifier);
    public int? EmployeeId => GetInt("employeeId");
    public int? DepartmentId => GetInt("departmentId");
    public int? OrganizationId => GetInt("organizationId");
    public bool Has(string permission) => User?.HasClaim("permission", permission) ?? false;

    private int? GetInt(string type) =>
        int.TryParse(User?.FindFirst(type)?.Value, out var i) ? i : null;
}

// ---- Політики пермішенів (будь-яка назва пермішена -> політика) ----
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (Permissions.Composite.TryGetValue(requirement.Permission, out var parts))
        {
            if (parts.Any(p => context.User.HasClaim("permission", p)))
                context.Succeed(requirement);
        }
        else if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await _fallback.GetPolicyAsync(policyName);
        if (existing is not null) return existing;
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}

// ---- Генерація JWT ----
public class TokenService(IConfiguration config)
{
    // Токен містить лише ідентифікатор користувача; employeeId/departmentId та
    // пермішени додаються до принципала ПЕР-ЗАПИТ із БД (PermissionClaimsTransformation),
    // тож зміни прав діють одразу, без перевидання токена.
    public string Create(int userId, string userName, string fullName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new("fullName", fullName),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ---- Хешування паролів (PBKDF2, без зовнішніх пакетів) ----
public static class PasswordHasher
{
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

// ---- Зона видимості документів (за пермішенами поточного користувача) ----
// Рівень передається у скалярну функцію БД dbo.fn_CanReadDocument:
//   3 — уся кореспонденція, 2 — свого підрозділу, 1 — лише задіяні документи, 0 — немає доступу.
public static class DocumentScope
{
    public static byte Level(ICurrentUser u)
    {
        if (u.Has(Permissions.DocumentsReadAll)) return 3;
        if (u.Has(Permissions.DocumentsReadDepartment) && u.DepartmentId is not null) return 2;
        if (u.Has(Permissions.DocumentsReadInvolved) && u.EmployeeId is not null) return 1;
        return 0;
    }
}
