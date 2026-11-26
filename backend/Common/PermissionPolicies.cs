using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using CorrespondenceSystem.Contracts.Common;

namespace CorrespondenceApi.Common;

/// <summary>
/// Політики пермішенів: будь-яка назва пермішена — політика. Композитна політика (напр. documents.read)
/// задовольняється будь-яким зі своїх складових (<see cref="Permissions.Composite"/>).
/// </summary>
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
            if (parts.Any(p => context.User.HasPermission(p)))
                context.Succeed(requirement);
        }
        else if (context.User.HasPermission(requirement.Permission))
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

