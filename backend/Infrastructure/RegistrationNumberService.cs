using CorrespondenceApi.Common;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Infrastructure;

// Генерація наступного реєстраційного номера за патерном організації.
// Лічильник — за (організація, тип документа, рік); номер унікальний у межах організації та року.
public class RegistrationNumberService(AppDbContext db)
{
    public async Task<(string Number, int Seq)> NextAsync(
        int organizationId, int directionId, int documentTypeId, int year, CancellationToken ct = default)
    {
        var pattern = await db.Organizations.IgnoreQueryFilters()
            .Where(o => o.Id == organizationId)
            .Select(o => o.RegistrationNumberPattern).FirstOrDefaultAsync(ct);
        var typeCode = await db.DocumentTypes
            .Where(t => t.Id == documentTypeId).Select(t => t.Code).FirstOrDefaultAsync(ct);
        var dirCode = await db.Directions
            .Where(d => d.Id == directionId).Select(d => d.Code).FirstOrDefaultAsync(ct);

        // Наступний номер лічильника для (організація, тип, рік)
        var maxSeq = await db.Documents.IgnoreQueryFilters()
            .Where(d => d.OrganizationId == organizationId && d.DocumentTypeId == documentTypeId
                && d.Registration != null && d.Registration.RegistrationYear == year)
            .Select(d => d.Registration!.SequenceNumber)
            .MaxAsync(ct) ?? 0;

        var seq = maxSeq + 1;
        // Запас від колізій (ручні номери або патерн без {type}, що дає однакові рядки для різних типів)
        for (var i = 0; i < 1000; i++)
        {
            var number = RegistrationNumberFormatter.Format(pattern, seq, year, typeCode, dirCode);
            var taken = await db.Documents.IgnoreQueryFilters().AnyAsync(d =>
                d.OrganizationId == organizationId && d.Registration != null
                && d.Registration.RegistrationYear == year
                && d.Registration.RegistrationNumber == number, ct);
            if (!taken) return (number, seq);
            seq++;
        }
        throw new InvalidOperationException("Не вдалося згенерувати унікальний реєстраційний номер");
    }
}
