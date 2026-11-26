namespace CorrespondenceSystem.DataService.Infrastructure.Database;

/// <summary>Допоміжні функції для тексту SQL-запитів.</summary>
public static class SqlText
{
    /// <summary>Символ екранування для LIKE (… LIKE @p ESCAPE '!').</summary>
    public const string LikeEscape = "!";

    /// <summary>Екранує спецсимволи LIKE, щоб пошук ішов за буквальним входженням рядка.</summary>
    public static string EscapeLike(string value) => value
        .Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![");
}
