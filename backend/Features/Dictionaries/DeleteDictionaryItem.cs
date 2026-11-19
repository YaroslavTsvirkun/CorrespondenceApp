using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Data.SqlClient;

namespace CorrespondenceApi.Features.Dictionaries;

/// <summary>Слайс: видалення запису довідника (заборонено, якщо запис використовується).</summary>
public static class DeleteDictionaryItem
{
    public record Command(string Kind, int Id) : IRequest;

    public class Handler(Db db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var t = DictionaryTables.Resolve(c.Kind);
            try
            {
                var affected = await db.ExecuteAsync($"DELETE {t.Name} WHERE Id = @Id", new { c.Id }, ct);
                if (affected == 0) throw new NotFoundException($"Запис #{c.Id} не знайдено");
            }
            catch (SqlException ex) when (SqlErrors.IsReferenceViolation(ex))
            {
                throw new ValidationException(
                    [new ValidationFailure("dictionary", "Запис використовується — видалення заборонено")]);
            }
        }
    }
}
