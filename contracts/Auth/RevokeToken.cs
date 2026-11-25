using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Auth;

/// <summary>
/// Слайс: відкликання JWT поточного користувача під час виходу (dbo.RevokedToken).
/// Відкликаний токен більше не автентифікує запити, навіть якщо його скопійовано до виходу.
/// Запис зберігається до моменту, коли токен однаково закінчився б (<c>ExpiresAtUtc</c>).
/// </summary>
public static class RevokeToken
{
    [RequirePermission]
    public record Command(string TokenId, DateTime ExpiresAtUtc) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.TokenId).NotEmpty().MaximumLength(64);
    }
}
