using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Parties;

/// <summary>
/// Дані слайсу: створення / оновлення контрагента разом з адресами, контактами, особами й категоріями
/// (процедура dbo.usp_Party_Save: тенант, головна юрособа без циклів, унікальність коду).
/// </summary>
public sealed class SavePartyHandler(Db db, ICurrentUser user) : IRequestHandler<SaveParty.Command, int>
{
    public Task<int> Handle(SaveParty.Command c, CancellationToken ct)
    {
        var d = c.Data;
        var p = SqlScope.Tenant(user);
        p.AddDynamicParams(new
        {
            c.Id,
            user.OrganizationId,
            d.Kind,
            FullName = d.FullName.Trim(),
            ShortName = Clean(d.ShortName),
            Code = Clean(d.Code),
            d.LegalFormId,
            d.ParentPartyId,
            d.IsActive,
            Note = Clean(d.Note),
            CategoryIds = SqlTvp.IdList(d.CategoryIds ?? []),
            Addresses = SqlTvp.PartyAddresses(d.Addresses ?? []),
            Contacts = SqlTvp.PartyContacts(d.Contacts ?? []),
            Persons = SqlTvp.PartyPersons(d.Persons ?? []),
        });
        return db.ScalarProcAsync<int>("dbo.usp_Party_Save", p, ct);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
