using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CorrespondenceApi.Common;

/// <summary>
/// Налаштування JWT (секція <c>Jwt</c>). Ключ підпису: під Aspire — параметр <c>jwt-key</c> з AppHost,
/// без Aspire — <c>appsettings.Development.json</c>. Без ключа (або з коротким) сервіс не стартує.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    private const int MinKeyBytes = 32;

    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    /// <summary>Ключ підпису (HS256) — для токенів користувачів і підписаних посилань редактора.</summary>
    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    public static JwtOptions Load(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(options.Key) || Encoding.UTF8.GetByteCount(options.Key) < MinKeyBytes)
            throw new InvalidOperationException($"Не задано Jwt:Key (мінімум {MinKeyBytes} байти) — ключ підпису токенів.");
        return options;
    }
}

/// <summary>
/// Видача JWT користувача. Токен містить лише ідентифікатор користувача; працівник, підрозділ і
/// пермішени додаються до принципала на кожен запит із сервісу даних (<c>PermissionClaims</c>),
/// тож зміни прав діють одразу, без перевидання токена.
/// </summary>
public sealed class TokenService(JwtOptions options)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public string Create(int userId, string userName, string fullName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            // Ідентифікатор токена — щоб відкликати саме його під час виходу (dbo.RevokedToken)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, userName),
            new(UserClaims.FullName, fullName),
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(Lifetime),
            signingCredentials: new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
