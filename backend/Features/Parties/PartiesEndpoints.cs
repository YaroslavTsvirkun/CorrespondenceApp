using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Parties;

/// <summary>
/// Фіча «Контрагенти» (у межах організації користувача): реєстр, банківські рахунки контрагентів
/// (окреме право parties.bank.manage) і довідник банків.
/// </summary>
public static class PartiesEndpoints
{
    public static void MapParties(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/parties").WithTags("Parties");

        g.MapGet("", async (ISender m, string? search, bool? includeInactive, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetParties.Query(search, includeInactive ?? true), ct)))
            .RequireAuthorization();

        g.MapGet("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetParty.Query(id), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, PartyInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveParty.Command(null, i), ct);
            return Results.Created($"/api/parties/{id}", new { id });
        }).RequireAuthorization(Permissions.PartiesManage);

        g.MapPut("/{id:int}", async (ISender m, int id, PartyInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveParty.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PartiesManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteParty.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PartiesManage);

        // ---- Банківські рахунки контрагента ----
        g.MapGet("/{partyId:int}/bank-accounts/history", async (ISender m, int partyId, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetBankAccountHistory.Query(partyId), ct)))
            .RequireAuthorization(Permissions.PartiesRead);

        g.MapPost("/{partyId:int}/bank-accounts", async (ISender m, int partyId, BankAccountInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveBankAccount.Command(partyId, null, i), ct);
            return Results.Created($"/api/parties/{partyId}/bank-accounts/{id}", new { id });
        }).RequireAuthorization(Permissions.PartiesBankManage);

        g.MapPut("/{partyId:int}/bank-accounts/{id:int}", async (ISender m, int partyId, int id, BankAccountInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveBankAccount.Command(partyId, id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PartiesBankManage);

        g.MapDelete("/{partyId:int}/bank-accounts/{id:int}", async (ISender m, int partyId, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteBankAccount.Command(partyId, id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PartiesBankManage);

        // ---- Довідник банків ----
        var banks = app.MapGroup("/api/banks").WithTags("Parties");

        banks.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetBanks.Query(), ct)))
            .RequireAuthorization();

        banks.MapPost("", async (ISender m, BankInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveBank.Command(null, i), ct);
            return Results.Created($"/api/banks/{id}", new { id });
        }).RequireAuthorization(Permissions.PartiesBankManage);

        banks.MapPut("/{id:int}", async (ISender m, int id, BankInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveBank.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PartiesBankManage);

        banks.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteBank.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PartiesBankManage);
    }
}
