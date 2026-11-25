using System.Reflection;
using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceSystem.DataService.Common;

/// <summary>
/// Перевірка прав на рівні сервісу даних (захист у глибину: backend перевіряє ті самі права на endpoint-ах,
/// але сервіс даних не покладається лише на нього). Права беруться з атрибута на типі запиту
/// (<see cref="RequirePermissionAttribute"/>); запит без атрибута відхиляється.
/// </summary>
public sealed class PermissionBehavior<TRequest, TResponse>(ICurrentUser user)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        switch (OperationAuthorization.For(typeof(TRequest)))
        {
            case AllowAnonymousOperationAttribute:
                break;
            case RequirePermissionAttribute required:
                if (!user.IsAuthenticated)
                    throw new UnauthorizedException("Потрібна автентифікація");
                if (required.AnyOf.Count > 0 && !required.AnyOf.Any(user.Has))
                    throw new ForbiddenException("Недостатньо прав для операції");
                break;
            default:
                throw new ForbiddenException("Операцію не позначено правами доступу");
        }
        return next(ct);
    }
}

public static class OperationAuthorization
{
    /// <summary>Атрибут доступу запиту: <see cref="RequirePermissionAttribute"/>, <see cref="AllowAnonymousOperationAttribute"/> або null.</summary>
    public static Attribute? For(Type requestType) =>
        (Attribute?)requestType.GetCustomAttribute<RequirePermissionAttribute>()
        ?? requestType.GetCustomAttribute<AllowAnonymousOperationAttribute>();

    /// <summary>Під час старту: кожна операція Data API має бути позначена правами доступу.</summary>
    public static void EnsureAllOperationsAnnotated()
    {
        var missing = DataApi.RequestTypes.Where(t => For(t) is null).Select(DataApi.OperationName).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Операції Data API без атрибута RequirePermission / AllowAnonymousOperation: " + string.Join(", ", missing));
    }
}
