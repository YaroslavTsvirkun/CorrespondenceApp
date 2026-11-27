using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;

namespace CorrespondenceSystem.DataService.Features.Dictionaries;

/// <summary>
/// Відповідність «вид довідника» з URL → таблиця БД і право на зміну записів.
/// Білий список: ім'я таблиці ніколи не береться з запиту напряму (захист від SQL-ін'єкцій).
/// </summary>
internal static class DictionaryTables
{
    public sealed record Table(string Name, bool HasCode, string ManagePermission);

    private static readonly Dictionary<string, Table> Map = new(StringComparer.Ordinal)
    {
        // Сервіс «Реєстрація кореспонденції»
        ["document-types"] = new("dbo.DocumentType", true, Permissions.DictionariesManage),
        ["channels"] = new("dbo.DeliveryChannel", false, Permissions.DictionariesManage),
        // Сервіс «Контрагенти»
        ["legal-forms"] = new("dbo.LegalForm", true, Permissions.PartiesManage),
        ["party-categories"] = new("dbo.PartyCategory", false, Permissions.PartiesManage),
    };

    public static Table Resolve(string kind) =>
        Map.TryGetValue(kind, out var t) ? t : throw new NotFoundException($"Невідомий довідник: {kind}");

    /// <summary>Довідник для зміни: перевіряє право саме на цей вид довідника.</summary>
    public static Table ResolveForWrite(string kind, ICurrentUser user)
    {
        var t = Resolve(kind);
        if (!user.Has(t.ManagePermission)) throw new ForbiddenException("Недостатньо прав для зміни цього довідника");
        return t;
    }
}
