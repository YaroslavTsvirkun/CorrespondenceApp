using CorrespondenceApi.Common;

namespace CorrespondenceApi.Features.Dictionaries;

/// <summary>
/// Відповідність «вид довідника» з URL → таблиця БД.
/// Білий список: ім'я таблиці ніколи не береться з запиту напряму (захист від SQL-ін'єкцій).
/// </summary>
internal static class DictionaryTables
{
    public sealed record Table(string Name, bool HasCode);

    private static readonly Dictionary<string, Table> Map = new(StringComparer.Ordinal)
    {
        ["document-types"] = new("dbo.DocumentType", true),
        ["channels"] = new("dbo.DeliveryChannel", false),
        ["correspondent-types"] = new("dbo.CorrespondentType", false),
    };

    public static Table Resolve(string kind) =>
        Map.TryGetValue(kind, out var t) ? t : throw new NotFoundException($"Невідомий довідник: {kind}");
}
