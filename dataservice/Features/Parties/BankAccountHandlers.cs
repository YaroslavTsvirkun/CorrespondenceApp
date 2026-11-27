using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;
using Microsoft.Data.SqlClient;

namespace CorrespondenceSystem.DataService.Features.Parties;

/// <summary>
/// Дані слайсу: створення / оновлення рахунку (процедура dbo.usp_PartyBankAccount_Save: банк за МФО з IBAN,
/// унікальність IBAN, один основний відкритий рахунок на валюту).
/// </summary>
public sealed class SaveBankAccountHandler(Db db, ICurrentUser user) : IRequestHandler<SaveBankAccount.Command, int>
{
    public Task<int> Handle(SaveBankAccount.Command c, CancellationToken ct)
    {
        var d = c.Data;
        var iban = PartyRules.NormalizeIban(d.Iban);
        var p = SqlScope.Tenant(user);
        p.AddDynamicParams(new
        {
            c.Id,
            c.PartyId,
            Iban = iban,
            Mfo = PartyRules.MfoFromIban(iban),
            // Назва вручну — лише для банку, якого немає в довіднику (іноземний або новий)
            BankName = Clean(d.BankName),
            SwiftBic = Clean(d.SwiftBic)?.ToUpperInvariant(),
            Currency = d.Currency.ToUpperInvariant(),
            Purpose = Clean(d.Purpose),
            AccountHolderName = Clean(d.AccountHolderName),
            d.IsPrimary,
            d.ClosedAt,
            Note = Clean(d.Note),
        });
        return db.ScalarProcAsync<int>("dbo.usp_PartyBankAccount_Save", p, ct);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>Дані слайсу: видалення рахунку контрагента організації користувача.</summary>
public sealed class DeleteBankAccountHandler(Db db, ICurrentUser user) : IRequestHandler<DeleteBankAccount.Command>
{
    public async Task Handle(DeleteBankAccount.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        p.Add("PartyId", c.PartyId);
        var affected = await db.ExecuteAsync($"""
            DELETE a FROM dbo.PartyBankAccount a
            JOIN dbo.Party p ON p.Id = a.PartyId
            WHERE a.Id = @Id AND a.PartyId = @PartyId AND {SqlScope.TenantFilter("p")}
            """, p, ct);
        if (affected == 0) throw new NotFoundException($"Рахунок #{c.Id} не знайдено");
    }
}

/// <summary>Дані слайсу: довідник банків із кількістю рахунків.</summary>
public sealed class GetBanksHandler(Db db) : IRequestHandler<GetBanks.Query, IReadOnlyList<BankDto>>
{
    public Task<IReadOnlyList<BankDto>> Handle(GetBanks.Query q, CancellationToken ct) =>
        db.QueryAsync<BankDto>("""
            SELECT b.Id, b.Mfo, b.Name, b.ShortName, b.Edrpou, b.SwiftBic, b.IsActive,
                   (SELECT COUNT(*) FROM dbo.PartyBankAccount a WHERE a.BankId = b.Id) AS AccountCount
            FROM dbo.Bank b
            ORDER BY COALESCE(b.ShortName, b.Name)
            """, null, ct);
}

/// <summary>Дані слайсу: створення / оновлення банку (процедура dbo.usp_Bank_Save).</summary>
public sealed class SaveBankHandler(Db db) : IRequestHandler<SaveBank.Command, int>
{
    public Task<int> Handle(SaveBank.Command c, CancellationToken ct)
    {
        var d = c.Data;
        return db.ScalarProcAsync<int>("dbo.usp_Bank_Save", new
        {
            c.Id,
            d.Mfo,
            Name = d.Name.Trim(),
            ShortName = Clean(d.ShortName),
            Edrpou = Clean(d.Edrpou),
            SwiftBic = Clean(d.SwiftBic)?.ToUpperInvariant(),
            d.IsActive,
        }, ct);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>Дані слайсу: видалення банку (заборонено, якщо в ньому є рахунки контрагентів).</summary>
public sealed class DeleteBankHandler(Db db) : IRequestHandler<DeleteBank.Command>
{
    public async Task Handle(DeleteBank.Command c, CancellationToken ct)
    {
        try
        {
            var affected = await db.ExecuteAsync("DELETE dbo.Bank WHERE Id = @Id", new { c.Id }, ct);
            if (affected == 0) throw new NotFoundException($"Банк #{c.Id} не знайдено");
        }
        catch (SqlException ex) when (SqlErrors.IsReferenceViolation(ex))
        {
            throw Errors.Validation("bank", "У банку є рахунки контрагентів — видалення заборонено. Позначте його як неактивний");
        }
    }
}
