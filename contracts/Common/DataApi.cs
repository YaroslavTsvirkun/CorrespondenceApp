using System.Collections.Frozen;
using MediatR;

namespace CorrespondenceSystem.Contracts.Common;

/// <summary>
/// Протокол внутрішнього Data API (сервіс <c>dataservice</c> — єдиний, хто має доступ до БД).
/// Кожен запит MediatR із цієї збірки — окрема операція:
/// <c>POST /rpc/{операція}</c>, тіло — JSON запиту, відповідь — JSON результату (або 204 для команд без результату).
/// Ім'я операції — повне ім'я типу без префікса збірки, напр. <c>Documents.GetDocuments.Query</c>.
/// </summary>
public static class DataApi
{
    public const string RoutePrefix = "/rpc";

    /// <summary>Спільний секрет backend ↔ dataservice (сервіс не приймає запитів без нього).</summary>
    public const string ApiKeyHeader = "X-Data-Api-Key";

    /// <summary>Ідентифікатор користувача, від імені якого виконується операція (необов'язковий).</summary>
    public const string UserIdHeader = "X-User-Id";

    private const string NamespacePrefix = "CorrespondenceSystem.Contracts.";

    private static readonly FrozenDictionary<string, Type> Operations = typeof(DataApi).Assembly.GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
                    && typeof(IBaseRequest).IsAssignableFrom(t))
        .ToFrozenDictionary(OperationName, StringComparer.Ordinal);

    /// <summary>Усі типи запитів, які обслуговує Data API.</summary>
    public static IEnumerable<Type> RequestTypes => Operations.Values;

    public static string OperationName(Type requestType)
    {
        var name = requestType.FullName ?? throw new ArgumentException("Тип без імені", nameof(requestType));
        if (name.StartsWith(NamespacePrefix, StringComparison.Ordinal)) name = name[NamespacePrefix.Length..];
        return name.Replace('+', '.');
    }

    public static bool TryGetRequestType(string operation, out Type requestType) =>
        Operations.TryGetValue(operation, out requestType!);

    /// <summary>Тип результату запиту (<c>IRequest&lt;T&gt;</c>) або null для команд без результату.</summary>
    public static Type? ResponseType(Type requestType) => requestType.GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
        ?.GetGenericArguments()[0];
}
