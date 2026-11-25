using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;

namespace CorrespondenceApi.Common;

/// <summary>
/// JWT користувача у cookie з HttpOnly: скрипти сторінки (і можлива XSS) не мають доступу до токена.
/// Cookie — SameSite=Strict, тож браузер не надсилає її з чужих сайтів. Додатково для змінюючих запитів
/// (POST/PUT/DELETE…) з cookie потрібен заголовок <see cref="CsrfHeader"/>: чужий сайт не може його
/// додати без CORS-дозволу. Заголовок Authorization: Bearer … як і раніше підтримується (Swagger, інструменти).
/// </summary>
public static class AuthCookie
{
    public const string Name = "cs_auth";
    public const string CsrfHeader = "X-Requested-With";

    public static void Append(HttpContext http, string token, DateTimeOffset expires) =>
        http.Response.Cookies.Append(Name, token, Options(http, expires));

    public static void Delete(HttpContext http) =>
        http.Response.Cookies.Delete(Name, Options(http, null));

    /// <summary>
    /// Secure: поза Development — завжди (за зворотним проксі з TLS запит до backend може бути HTTP,
    /// але браузер мусить надсилати cookie лише по HTTPS); у Development — лише для HTTPS-запитів.
    /// </summary>
    private static CookieOptions Options(HttpContext http, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps
                 || !http.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expires,
        IsEssential = true,
    };

    /// <summary>Токен з cookie, якщо немає заголовка Authorization (див. JwtBearerEvents.OnMessageReceived).</summary>
    public static Task ReadToken(MessageReceivedContext ctx)
    {
        var request = ctx.Request;
        if (!string.IsNullOrEmpty(ctx.Token) || request.Headers.Authorization.Count > 0) return Task.CompletedTask;
        if (!request.Cookies.TryGetValue(Name, out var token) || string.IsNullOrEmpty(token)) return Task.CompletedTask;

        // Захист від CSRF: змінюючі запити з cookie — лише з заголовком, який ставить наш фронтенд
        var safe = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
                   || HttpMethods.IsOptions(request.Method);
        if (safe || request.Headers.ContainsKey(CsrfHeader))
            ctx.Token = token;
        return Task.CompletedTask;
    }
}

/// <summary>Обмеження частоти спроб входу з однієї IP-адреси (секція <c>RateLimiting:Login</c>).</summary>
public sealed class LoginRateLimitOptions
{
    public const string Policy = "login";

    /// <summary>Скільки спроб входу дозволено за вікно.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Тривалість вікна, с.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Ліміт рахується за IP клієнта (Connection.RemoteIpAddress). За зворотним проксі налаштуйте
    /// ForwardedHeaders, інакше всі клієнти матимуть адресу проксі й спільний ліміт.
    /// </summary>
    public static IServiceCollection AddLoginRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("RateLimiting:Login").Get<LoginRateLimitOptions>() ?? new();
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
            o.AddPolicy(Policy, http => RateLimitPartition.GetFixedWindowLimiter(
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
