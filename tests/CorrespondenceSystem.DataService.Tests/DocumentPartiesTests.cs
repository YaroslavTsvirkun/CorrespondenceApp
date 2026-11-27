using System.Net;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.Contracts.Lookups;
using CorrespondenceSystem.Contracts.Lookups.Dtos;
using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using static CorrespondenceSystem.DataService.Tests.DataServiceFactory;

namespace CorrespondenceSystem.DataService.Tests;

/// <summary>Контрагенти документа: знімок назви кореспондента й додаткові адресати / копії.</summary>
[Collection(DemoDatabaseCollection.Name)]
public sealed class DocumentPartiesTests(DataServiceFactory app)
{
    private sealed record Ctx(int UserId, DocumentInput Blank);

    private async Task<Ctx> RegistrarAsync()
    {
        var registrar = await app.LoginAsync("registrar", "registrar123");
        var lookups = await app.RpcOkAsync<LookupsDto>(new GetLookups.Query(), registrar.UserId);
        var blank = new DocumentInput(
            lookups.Directions[0].Id, lookups.DocumentTypes[0].Id, lookups.DocumentStatuses[0].Id, null, null,
            null, null, null, "Тест адресатів", null, DateOnly.FromDateTime(DateTime.Today), null, false, null,
            null, DateTime.UtcNow.Year + 3, registrar.EmployeeId!.Value);
        return new Ctx(registrar.UserId, blank);
    }

    private async Task<int> NewPartyAsync(int userId, string name) =>
        await app.RpcOkAsync<int>(new SaveParty.Command(null,
            new PartyInput(PartyKind.LegalEntity, name, null, null, null, null, true, null)), userId);

    private async Task DeleteDocumentAsync(int documentId) =>
        await EnsureStatusAsync(await app.RpcAsync(new DeleteDocumentRecord.Command(documentId),
            (await app.LoginAsync("admin", "admin123")).UserId), HttpStatusCode.OK);

    [Fact]
    public async Task Correspondent_name_is_kept_as_it_was_at_registration()
    {
        var (userId, blank) = await RegistrarAsync();
        var partyId = await NewPartyAsync(userId, "ТОВ «Стара назва»");
        var documentId = await app.RpcOkAsync<int>(new CreateDocument.Command(blank with { CorrespondentId = partyId }), userId);
        try
        {
            // Контрагента перейменовано після реєстрації
            await app.RpcOkAsync<int>(new SaveParty.Command(partyId,
                new PartyInput(PartyKind.LegalEntity, "ТОВ «Нова назва»", null, null, null, null, true, null)), userId);

            var card = await app.RpcOkAsync<DocumentDetail>(new GetDocumentById.Query(documentId), userId);
            Assert.Equal("ТОВ «Стара назва»", card.Correspondent);
            Assert.Equal("ТОВ «Нова назва»", card.CorrespondentCurrentName);

            // Редагування документа без зміни кореспондента знімок не оновлює
            var input = await app.RpcOkAsync<DocumentInput>(new GetDocumentForEdit.Query(documentId), userId);
            await EnsureStatusAsync(await app.RpcAsync(new UpdateDocument.Command(documentId, input with { Title = "Змінено" }), userId),
                HttpStatusCode.NoContent);
            Assert.Equal("ТОВ «Стара назва»",
                (await app.RpcOkAsync<DocumentDetail>(new GetDocumentById.Query(documentId), userId)).Correspondent);

            // Пошук у реєстрі документів — за назвою з документа
            var found = await app.RpcOkAsync<Contracts.Common.PagedResult<DocumentListItem>>(
                new GetDocuments.Query { Search = "Стара назва" }, userId);
            Assert.Contains(found.Items, d => d.Id == documentId);
        }
        finally
        {
            await DeleteDocumentAsync(documentId);
            await EnsureStatusAsync(await app.RpcAsync(new DeleteParty.Command(partyId), userId), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Addressees_and_copies_are_saved_replaced_and_protect_parties()
    {
        var (userId, blank) = await RegistrarAsync();
        var main = await NewPartyAsync(userId, "ТОВ «Головний адресат»");
        var second = await NewPartyAsync(userId, "ТОВ «Другий адресат»");
        var copy = await NewPartyAsync(userId, "ТОВ «Копія»");

        var documentId = await app.RpcOkAsync<int>(new CreateDocument.Command(blank with
        {
            CorrespondentId = main,
            Addressees = [new DocumentPartyInput(second, 1), new DocumentPartyInput(copy, 2)],
        }), userId);
        try
        {
            var card = await app.RpcOkAsync<DocumentDetail>(new GetDocumentById.Query(documentId), userId);
            Assert.Equal([(second, (byte)1), (copy, (byte)2)], card.Addressees.Select(a => (a.PartyId, a.Role)));

            var input = await app.RpcOkAsync<DocumentInput>(new GetDocumentForEdit.Query(documentId), userId);
            Assert.Equal(2, input.Addressees!.Count);

            // Контрагента-адресата не можна видалити
            var delete = await app.RpcAsync(new DeleteParty.Command(copy), userId);
            Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
            var listed = await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query("Копія"), userId);
            Assert.Equal(1, listed.Single().DocumentCount);

            // Адресати не передані — лишаються; порожній список — прибирає всіх
            await EnsureStatusAsync(await app.RpcAsync(new UpdateDocument.Command(documentId, input with { Addressees = null }), userId),
                HttpStatusCode.NoContent);
            Assert.Equal(2, (await app.RpcOkAsync<DocumentDetail>(new GetDocumentById.Query(documentId), userId)).Addressees.Count);

            await EnsureStatusAsync(await app.RpcAsync(new UpdateDocument.Command(documentId,
                input with { Addressees = [new DocumentPartyInput(copy, 2)] }), userId), HttpStatusCode.NoContent);
            Assert.Equal(copy, (await app.RpcOkAsync<DocumentDetail>(new GetDocumentById.Query(documentId), userId)).Addressees.Single().PartyId);

            await EnsureStatusAsync(await app.RpcAsync(new UpdateDocument.Command(documentId, input with { Addressees = [] }), userId),
                HttpStatusCode.NoContent);
            Assert.Empty((await app.RpcOkAsync<DocumentDetail>(new GetDocumentById.Query(documentId), userId)).Addressees);
        }
        finally
        {
            await DeleteDocumentAsync(documentId);
            foreach (var id in new[] { main, second, copy })
                await EnsureStatusAsync(await app.RpcAsync(new DeleteParty.Command(id), userId), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Invalid_addressees_are_rejected()
    {
        var (userId, blank) = await RegistrarAsync();
        var orgAdmin2 = (await app.LoginAsync("orgadmin2", "orgadmin123")).UserId;
        var foreign = (await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(), orgAdmin2))[0].Id;
        var own = (await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(), userId))[0].Id;

        DocumentInput[] invalid =
        [
            blank with { Addressees = [new DocumentPartyInput(foreign, 1)] },                              // чужа організація
            blank with { Addressees = [new DocumentPartyInput(own, 1), new DocumentPartyInput(own, 2)] },  // двічі
            blank with { CorrespondentId = own, Addressees = [new DocumentPartyInput(own, 2)] },           // головний ще й копія
            blank with { Addressees = [new DocumentPartyInput(own, 5)] },                                  // невідома роль
        ];
        foreach (var input in invalid)
        {
            var response = await app.RpcAsync(new CreateDocument.Command(input), userId);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
