using CorrespondenceApi.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>
/// Підписані посилання для Document Server (видача файлу й callback збереження): JWT з тим самим
/// ключем, що й токени користувачів, але з окремою аудиторією — тож ні посилання не придатне як токен
/// користувача, ні навпаки. Посилання прив'язане до вкладення, користувача й ключа вмісту.
/// </summary>
public sealed class EditorLinkTokens(JwtOptions jwt)
{
    private const string Audience = "document-editor";
    private static readonly JsonWebTokenHandler Handler = new();

    public sealed record Link(int UserId, string Key);

    public string Create(int attachmentId, int userId, string key, TimeSpan lifetime) =>
        Handler.CreateToken(new SecurityTokenDescriptor
        {
            Audience = Audience,
            Issuer = jwt.Issuer,
            Expires = DateTime.UtcNow.Add(lifetime),
            Claims = new Dictionary<string, object> { ["att"] = attachmentId, ["uid"] = userId, ["key"] = key },
            SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256),
        });

    /// <summary>Дані посилання, якщо токен дійсний і виданий саме для цього вкладення; інакше null.</summary>
    public async Task<Link?> ValidateAsync(string? token, int attachmentId)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var result = await Handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKey = jwt.SigningKey,
            ValidAudience = Audience,
            ValidIssuer = jwt.Issuer,
        });
        if (!result.IsValid || result.SecurityToken is not JsonWebToken parsed) return null;
        if (!parsed.TryGetPayloadValue<int>("att", out var att) || att != attachmentId) return null;
        if (!parsed.TryGetPayloadValue<int>("uid", out var uid) || !parsed.TryGetPayloadValue<string>("key", out var key))
            return null;
        return new Link(uid, key);
    }
}
