using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Dictionaries;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Data.SqlClient;

namespace CorrespondenceSystem.DataService.Features.Dictionaries;

/// <summary>Дані слайсу: видалення запису довідника (заборонено, якщо запис використовується).</summary>
public sealed class DeleteDictionaryItemHandler(Db db) : IRequestHandler<DeleteDictionaryItem.Command>
{
    public async Task Handle(DeleteDictionaryItem.Command c, CancellationToken ct)
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
