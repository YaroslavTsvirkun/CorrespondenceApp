using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Correspondents;
using CorrespondenceSystem.Contracts.Correspondents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Correspondents;

/// <summary>Дані слайсу: створення / оновлення кореспондента.</summary>
public sealed class SaveCorrespondentHandler(Db db, ICurrentUser user) : IRequestHandler<SaveCorrespondent.Command, int>
{
    public async Task<int> Handle(SaveCorrespondent.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.AddDynamicParams(c.Data);
        p.Add("Id", c.Id);

        if (c.Id is null)
        {
            var orgId = user.OrganizationId
                ?? throw Errors.Validation("organization", "Створення доступне лише в межах організації");
            p.Add("OrganizationId", orgId);
            return await db.ScalarAsync<int>("""
                INSERT dbo.Correspondent (OrganizationId, CorrespondentTypeId, Name, Address, Phone, Email, ContactPerson, Note)
                VALUES (@OrganizationId, @CorrespondentTypeId, @Name, @Address, @Phone, @Email, @ContactPerson, @Note);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """, p, ct);
        }

        var affected = await db.ExecuteAsync($"""
            UPDATE c
            SET CorrespondentTypeId = @CorrespondentTypeId, Name = @Name, Address = @Address,
                Phone = @Phone, Email = @Email, ContactPerson = @ContactPerson, Note = @Note
            FROM dbo.Correspondent c
            WHERE c.Id = @Id AND {SqlScope.TenantFilter("c")}
            """, p, ct);
        if (affected == 0) throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
        return c.Id.Value;
    }
}
