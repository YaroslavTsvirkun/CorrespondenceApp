using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CorrespondenceApi.Infrastructure.Editor;

/// <summary>
/// Налаштування онлайн-редактора документів ONLYOFFICE Docs (секція <c>DocumentEditor</c>).
/// Під Aspire <c>PublicUrl</c> і <c>JwtSecret</c> приходять з AppHost.
/// </summary>
public sealed class DocumentEditorOptions
{
    /// <summary>Адреса Document Server для браузера (звідти вантажиться api.js), напр. http://localhost:8085.</summary>
    public string PublicUrl { get; set; } = "";

    /// <summary>Спільний секрет JWT з Document Server (JWT_SECRET контейнера).</summary>
    public string JwtSecret { get; set; } = "";

    /// <summary>
    /// Адреса backend, як її бачить Document Server (для завантаження файлу й callback збереження).
    /// Контейнер Docker звертається до хоста через host.docker.internal.
    /// </summary>
    public string CallbackBaseUrl { get; set; } = "http://host.docker.internal:5080";

    /// <summary>
    /// Необов'язково: внутрішня адреса Document Server для backend. Якщо задано, нею замінюється
    /// origin посилання на збережений файл з callback (коли браузер і backend бачать сервер по-різному).
    /// </summary>
    public string? InternalUrl { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicUrl) && JwtSecret.Length >= 32;
}

/// <summary>JWT (HS256) для обміну з Document Server: підпис конфігурації й перевірка callback.</summary>
public sealed class OnlyOfficeJwt(DocumentEditorOptions options)
{
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    private SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(options.JwtSecret));

    /// <summary>Підписує об'єкт як payload JWT (так Document Server перевіряє конфігурацію редактора).</summary>
    public string Sign(object payload) =>
        Handler.CreateToken(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));

    /// <summary>Перевіряє підпис і повертає payload як JSON (або null, якщо токен недійсний).</summary>
    public async Task<JsonElement?> ValidateAsync(string token)
    {
        var result = await Handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKey = Key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = false,
        });
        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt) return null;
        return JsonDocument.Parse(Base64UrlEncoder.Decode(jwt.EncodedPayload)).RootElement.Clone();
    }
}
