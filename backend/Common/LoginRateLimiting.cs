using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CorrespondenceApi.Common;

/// <summary>Обмеження частоти спроб входу з однієї IP-адреси (секція <c>RateLimiting:Login</c>).</summary>
public sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting:Login";
    public const string Policy = "login";

    /// <summary>Скільки спроб входу дозволено за вікно.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Тривалість вікна, с.</summary>
    public int WindowSeconds { get; set; } = 60;
}

public static class LoginRateLimitingExtensions
{
    /// <summary>
    /// Політика <see cref="LoginRateLimitOptions.Policy"/>: ліміт рахується за IP клієнта
    /// (Connection.RemoteIpAddress). За зворотним проксі налаштуйте ForwardedHeaders (довірені проксі),
    /// інакше всі клієнти матимуть адресу проксі й спільний ліміт.
    /// </summary>
    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(LoginRateLimitOptions.SectionName).Get<LoginRateLimitOptions>() ?? new();
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (ctx, ct) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                await ctx.HttpContext.Response.WriteAsJsonAsync(new
                {
                    title = "Забагато спроб входу. Зачекайте хвилину й спробуйте знову",
                    status = StatusCodes.Status429TooManyRequests,
                }, ct);
            };
            o.AddPolicy(LoginRateLimitOptions.Policy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, options.PermitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds)),
                    QueueLimit = 0,
                }));
        });
    }
}
