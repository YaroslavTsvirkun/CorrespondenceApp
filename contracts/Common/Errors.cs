using FluentValidation;
using FluentValidation.Results;

namespace CorrespondenceSystem.Contracts.Common;

/// <summary>Створення помилок бізнес-правил і їх HTTP-відповідей — однаково в backend і сервісі даних.</summary>
public static class Errors
{
    /// <summary>Помилка валідації одного поля (-> 400): <c>throw Errors.Validation("user", "…")</c>.</summary>
    public static ValidationException Validation(string field, string message) =>
        new([new ValidationFailure(field, message)]);

    /// <summary>Відповідь на помилку: HTTP-код і тіло.</summary>
    public sealed record Problem(int Status, object Body);

    /// <summary>
    /// HTTP-відповідь для спільних винятків (валідація, «не знайдено», 401, 403) або null для решти.
    /// Тіло — <c>{ title, status, errors? }</c>: цей формат backend передає клієнту без змін,
    /// тож обидва сервіси мусять формувати його однаково.
    /// </summary>
    public static Problem? ToProblem(Exception ex) => ex switch
    {
        ValidationException ve => new(400, new
        {
            title = "Помилка валідації",
            status = 400,
            errors = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
        }),
        NotFoundException nf => Simple(404, nf.Message),
        UnauthorizedException ue => Simple(401, ue.Message),
        ForbiddenException fe => Simple(403, fe.Message),
        _ => null,
    };

    public static Problem Simple(int status, string title) => new(status, new { title, status });
}
