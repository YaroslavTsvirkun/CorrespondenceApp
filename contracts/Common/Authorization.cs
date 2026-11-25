namespace CorrespondenceSystem.Contracts.Common;

/// <summary>
/// Права, потрібні для операції Data API. Сервіс даних перевіряє їх сам (не покладаючись лише на backend):
/// користувач має мати хоча б один із перелічених пермішенів (композитні назви, напр.
/// <see cref="Permissions.DocumentsReadAny"/>, розгортаються). Без аргументів — достатньо автентифікації.
/// Кожен запит Contracts має бути позначений цим атрибутом або <see cref="AllowAnonymousOperationAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RequirePermissionAttribute(params string[] anyOf) : Attribute
{
    public IReadOnlyList<string> AnyOf { get; } = anyOf
        .SelectMany(p => Permissions.Composite.TryGetValue(p, out var parts) ? parts : [p])
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}

/// <summary>Операція Data API без користувача (напр. перевірка логіна й пароля).</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AllowAnonymousOperationAttribute : Attribute;

/// <summary>Користувач автентифікований, але не має потрібних прав -> 403.</summary>
public class ForbiddenException(string message) : Exception(message);
