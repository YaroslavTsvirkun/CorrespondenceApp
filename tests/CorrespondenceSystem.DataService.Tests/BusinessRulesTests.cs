using System.Net;
using CorrespondenceSystem.Contracts.Correspondents;
using CorrespondenceSystem.Contracts.Correspondents.Dtos;
using CorrespondenceSystem.Contracts.Dictionaries;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.Contracts.Lookups;
using CorrespondenceSystem.Contracts.Lookups.Dtos;
using CorrespondenceSystem.Contracts.Reports;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using static CorrespondenceSystem.DataService.Tests.DataServiceFactory;

namespace CorrespondenceSystem.DataService.Tests;

/// <summary>Бізнес-правила поза авторизацією: реєстраційні номери, звіти, довідники, записи в межах організації.</summary>
[Collection(DemoDatabaseCollection.Name)]
public sealed class BusinessRulesTests(DataServiceFactory app)
{
    private async Task<int> UserId(string userName, string password) => (await app.LoginAsync(userName, password)).UserId;

    // ---- Реєстрація документів ----

    [Fact]
    public async Task Registration_number_is_generated_and_advances_after_registration()
    {
        var registrar = await app.LoginAsync("registrar", "registrar123");
        var lookups = await app.RpcOkAsync<LookupsDto>(new GetLookups.Query(), registrar.UserId);
        var direction = lookups.Directions[0].Id;
        var type = lookups.DocumentTypes[0].Id;
        var year = DateTime.UtcNow.Year + 1; // окремий рік — лічильник не перетинається з демо-даними

        var first = await app.RpcOkAsync<NextNumberResult>(new GetNextNumber.Query(direction, type, year), registrar.UserId);
        Assert.False(string.IsNullOrWhiteSpace(first.Number));

        var documentId = await app.RpcOkAsync<int>(new CreateDocument.Command(new DocumentInput(
            direction, type, lookups.DocumentStatuses[0].Id, null, null, null, null, null,
            "Тест реєстрації", null, DateOnly.FromDateTime(DateTime.Today), null, false, null,
            first.Number, year, registrar.EmployeeId!.Value)), registrar.UserId);

        var next = await app.RpcOkAsync<NextNumberResult>(new GetNextNumber.Query(direction, type, year), registrar.UserId);
        Assert.NotEqual(first.Number, next.Number);

        // Той самий номер повторно не реєструється
        var duplicate = await app.RpcAsync(new CreateDocument.Command(new DocumentInput(
            direction, type, lookups.DocumentStatuses[0].Id, null, null, null, null, null,
            "Дубль", null, DateOnly.FromDateTime(DateTime.Today), null, false, null,
            first.Number, year, registrar.EmployeeId!.Value)), registrar.UserId);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        await EnsureStatusAsync(await app.RpcAsync(new DeleteDocumentRecord.Command(documentId),
            await UserId("admin", "admin123")), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Document_without_title_is_rejected_by_validation()
    {
        var registrar = await app.LoginAsync("registrar", "registrar123");
        var response = await app.RpcAsync(new CreateDocument.Command(new DocumentInput(
            1, 1, 1, null, null, null, null, null, "", null, DateOnly.FromDateTime(DateTime.Today), null, false, null,
            null, DateTime.UtcNow.Year, registrar.EmployeeId!.Value)), registrar.UserId);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Вкажіть тему документа", await response.Content.ReadAsStringAsync());
    }

    // ---- Звіти ----

    [Fact]
    public async Task Reports_count_only_own_organization_documents()
    {
        static int Total(IEnumerable<DocumentsByTypeRow> rows) => rows.Sum(r => r.Count);

        var org1 = Total(await app.RpcOkAsync<IReadOnlyList<DocumentsByTypeRow>>(new DocumentsByType.Query(),
            await UserId("registrar", "registrar123")));
        var all = Total(await app.RpcOkAsync<IReadOnlyList<DocumentsByTypeRow>>(new DocumentsByType.Query(),
            await UserId("admin", "admin123")));
        var org2 = Total(await app.RpcOkAsync<IReadOnlyList<DocumentsByTypeRow>>(new DocumentsByType.Query(),
            await UserId("orgadmin2", "orgadmin123")));

        Assert.True(org1 > 0);
        Assert.True(all >= org1 + org2);
        Assert.NotEqual(org1, org2);
    }

    [Fact]
    public async Task Reports_require_permission()
    {
        var response = await app.RpcAsync(new DocumentsByType.Query(), await UserId("executor", "executor123"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Довідники ----

    [Fact]
    public async Task Dictionary_items_are_managed_only_with_permission()
    {
        var registrar = await UserId("registrar", "registrar123");
        var input = new DictionaryItemInput($"Тестовий канал {Guid.NewGuid():N}"[..30], null, null);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await app.RpcAsync(new SaveDictionaryItem.Command("channels", null, input), await UserId("executor", "executor123"))).StatusCode);

        var id = await app.RpcOkAsync<int>(new SaveDictionaryItem.Command("channels", null, input), registrar);
        var items = await app.RpcOkAsync<IReadOnlyList<DictionaryItemDto>>(new GetDictionary.Query("channels"), registrar);
        Assert.Contains(items, i => i.Id == id && i.Name == input.Name);

        await EnsureStatusAsync(await app.RpcAsync(new DeleteDictionaryItem.Command("channels", id), registrar),
            HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unknown_dictionary_kind_is_not_found()
    {
        var response = await app.RpcAsync(new GetDictionary.Query("dbo.AppUser"), await UserId("registrar", "registrar123"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Записи в межах організації ----

    [Fact]
    public async Task Organization_admin_cannot_change_another_organizations_records()
    {
        var registrar = await UserId("registrar", "registrar123");
        var org1Correspondent = (await app.RpcOkAsync<IReadOnlyList<CorrespondentDto>>(new GetCorrespondents.Query(), registrar))[0];

        var orgAdmin2 = await UserId("orgadmin2", "orgadmin123");
        var update = await app.RpcAsync(new SaveCorrespondent.Command(org1Correspondent.Id, new CorrespondentInput(
            org1Correspondent.CorrespondentTypeId, "Підмінено", null, null, null, null, null)), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        var delete = await app.RpcAsync(new DeleteCorrespondent.Command(org1Correspondent.Id), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var unchanged = (await app.RpcOkAsync<IReadOnlyList<CorrespondentDto>>(new GetCorrespondents.Query(), registrar))
            .Single(c => c.Id == org1Correspondent.Id);
        Assert.Equal(org1Correspondent.Name, unchanged.Name);
    }
}
