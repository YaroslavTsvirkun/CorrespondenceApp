namespace CorrespondenceApi.Infrastructure.Cors;

/// <summary>
/// Налаштування CORS для фронтенду (секція <c>Cors</c> в appsettings.json).
/// Порожні списки методів/заголовків означають «будь-які».
/// </summary>
public sealed class CorsPolicyOptions
{
    public const string SectionName = "Cors";

    /// <summary>Ім'я політики CORS.</summary>
    public string PolicyName { get; set; } = "frontend";

    /// <summary>Дозволені джерела (origin), напр. <c>http://localhost:5173</c>.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Дозволені HTTP-методи; порожньо — будь-які.</summary>
    public string[] AllowedMethods { get; set; } = [];

    /// <summary>Дозволені заголовки запиту; порожньо — будь-які.</summary>
    public string[] AllowedHeaders { get; set; } = [];

    /// <summary>Заголовки відповіді, доступні з JS (напр. <c>Content-Disposition</c> для завантажень).</summary>
    public string[] ExposedHeaders { get; set; } = [];

    /// <summary>Дозволити cookies / Authorization з credentials (несумісно з origin "*").</summary>
    public bool AllowCredentials { get; set; }

    /// <summary>Скільки секунд браузер кешує preflight-відповідь; null — не задавати.</summary>
    public int? PreflightMaxAgeSeconds { get; set; }
}
