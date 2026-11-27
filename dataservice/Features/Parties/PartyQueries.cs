using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Parties;

/// <summary>Дані слайсу: реєстр контрагентів організації користувача.</summary>
public sealed class GetPartiesHandler(Db db, ICurrentUser user) : IRequestHandler<GetParties.Query, IReadOnlyList<PartyListItem>>
{
    public Task<IReadOnlyList<PartyListItem>> Handle(GetParties.Query q, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("IncludeInactive", q.IncludeInactive);
        p.Add("Pattern", string.IsNullOrWhiteSpace(q.Search) ? null : $"%{SqlText.EscapeLike(q.Search.Trim())}%");

        return db.QueryAsync<PartyListItem>($"""
            SELECT p.Id, p.Kind, p.FullName, p.ShortName, p.Code, lf.Code AS LegalForm,
                   p.ParentPartyId, COALESCE(pp.ShortName, pp.FullName) AS ParentName, p.IsActive,
                   addr.Address, phone.Value AS Phone, email.Value AS Email, cats.Categories,
                   (SELECT COUNT(*) FROM dbo.Document d WHERE d.CorrespondentId = p.Id) AS DocumentCount,
                   (SELECT COUNT(*) FROM dbo.PartyBankAccount a WHERE a.PartyId = p.Id AND a.ClosedAt IS NULL) AS BankAccountCount
            FROM dbo.Party p
            LEFT JOIN dbo.LegalForm lf ON lf.Id = p.LegalFormId
            LEFT JOIN dbo.Party pp     ON pp.Id = p.ParentPartyId
            OUTER APPLY (SELECT TOP (1) CONCAT_WS(N', ', NULLIF(a.PostalCode, N''), NULLIF(a.City, N''), NULLIF(a.Line, N'')) AS Address
                         FROM dbo.PartyAddress a WHERE a.PartyId = p.Id ORDER BY a.Kind) addr
            OUTER APPLY (SELECT TOP (1) c.Value FROM dbo.PartyContact c
                         WHERE c.PartyId = p.Id AND c.Kind = 1 ORDER BY c.IsPrimary DESC, c.Id) phone
            OUTER APPLY (SELECT TOP (1) c.Value FROM dbo.PartyContact c
                         WHERE c.PartyId = p.Id AND c.Kind = 2 ORDER BY c.IsPrimary DESC, c.Id) email
            OUTER APPLY (SELECT STRING_AGG(pc.Name, N', ') WITHIN GROUP (ORDER BY pc.Name) AS Categories
                         FROM dbo.PartyCategoryLink l JOIN dbo.PartyCategory pc ON pc.Id = l.CategoryId
                         WHERE l.PartyId = p.Id) cats
            WHERE {SqlScope.TenantFilter("p")}
              AND (@IncludeInactive = 1 OR p.IsActive = 1)
              AND (@Pattern IS NULL OR p.FullName LIKE @Pattern ESCAPE '!' OR p.ShortName LIKE @Pattern ESCAPE '!'
                   OR p.Code LIKE @Pattern ESCAPE '!')
            ORDER BY COALESCE(p.ShortName, p.FullName)
            """, p, ct);
    }
}

/// <summary>Дані слайсу: картка контрагента (реквізити, категорії, адреси, контакти, особи, рахунки).</summary>
public sealed class GetPartyHandler(Db db, ICurrentUser user) : IRequestHandler<GetParty.Query, PartyDetail>
{
    public async Task<PartyDetail> Handle(GetParty.Query q, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", q.Id);

        await using var c = await db.OpenAsync(ct);
        using var grid = await c.QueryMultipleAsync(new CommandDefinition($"""
            SELECT p.Id, p.Kind, p.FullName, p.ShortName, p.Code, p.LegalFormId, p.ParentPartyId, p.IsActive, p.Note,
                   (SELECT COUNT(*) FROM dbo.Document d WHERE d.CorrespondentId = p.Id) AS DocumentCount
            FROM dbo.Party p WHERE p.Id = @Id AND {SqlScope.TenantFilter("p")};
            SELECT CategoryId FROM dbo.PartyCategoryLink WHERE PartyId = @Id;
            SELECT Kind, Country, PostalCode, Region, City, Line FROM dbo.PartyAddress WHERE PartyId = @Id ORDER BY Kind;
            SELECT Kind, Value, IsPrimary, Note FROM dbo.PartyContact WHERE PartyId = @Id ORDER BY Kind, IsPrimary DESC, Id;
            SELECT FullName, PositionName, Phone, Email, IsSignatory, Note FROM dbo.PartyPerson WHERE PartyId = @Id
            ORDER BY IsSignatory DESC, Id;
            {BankAccountSql.Select} WHERE a.PartyId = @Id
            ORDER BY CASE WHEN a.ClosedAt IS NULL THEN 0 ELSE 1 END, a.IsPrimary DESC, a.Currency, a.Id;
            """, p, cancellationToken: ct));

        var party = await grid.ReadSingleOrDefaultAsync<PartyDetail>()
                    ?? throw new NotFoundException($"Контрагента #{q.Id} не знайдено");
        return party with
        {
            CategoryIds = (await grid.ReadAsync<int>()).AsList(),
            Addresses = (await grid.ReadAsync<PartyAddressItem>()).AsList(),
            Contacts = (await grid.ReadAsync<PartyContactItem>()).AsList(),
            Persons = (await grid.ReadAsync<PartyPersonItem>()).AsList(),
            BankAccounts = (await grid.ReadAsync<BankAccountDto>()).AsList(),
        };
    }
}

/// <summary>Вибірка рахунку з назвою банку з довідника (або введеною вручну для іноземного банку).</summary>
internal static class BankAccountSql
{
    public const string Select = """
        SELECT a.Id, a.Iban, a.BankId,
               COALESCE(b.Mfo, CASE WHEN a.Iban LIKE 'UA%' THEN SUBSTRING(a.Iban, 5, 6) END) AS Mfo,
               COALESCE(b.ShortName, b.Name, a.BankName) AS BankName,
               COALESCE(a.SwiftBic, b.SwiftBic) AS SwiftBic,
               a.Currency, a.Purpose, a.AccountHolderName, a.IsPrimary, a.ClosedAt, a.Note
        FROM dbo.PartyBankAccount a
        LEFT JOIN dbo.Bank b ON b.Id = a.BankId
        """;
}
