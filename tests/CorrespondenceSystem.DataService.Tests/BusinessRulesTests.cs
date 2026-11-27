using System.Net;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Departments;
using CorrespondenceSystem.Contracts.Departments.Dtos;
using CorrespondenceSystem.Contracts.Employees;
using CorrespondenceSystem.Contracts.Employees.Dtos;
using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.Contracts.Parties.Dtos;
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
        var org1Party = (await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(), registrar))[0];

        var orgAdmin2 = await UserId("orgadmin2", "orgadmin123");
        var read = await app.RpcAsync(new GetParty.Query(org1Party.Id), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);

        var update = await app.RpcAsync(new SaveParty.Command(org1Party.Id, new PartyInput(
            PartyKind.LegalEntity, "Підмінено", null, null, null, null, true, null)), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        var account = await app.RpcAsync(new SaveBankAccount.Command(org1Party.Id, null, new BankAccountInput(
            "UA183052990000026007052612345", null, null, "UAH", null, null, false, null, null)), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, account.StatusCode);

        var delete = await app.RpcAsync(new DeleteParty.Command(org1Party.Id), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var unchanged = await app.RpcOkAsync<PartyDetail>(new GetParty.Query(org1Party.Id), registrar);
        Assert.Equal(org1Party.FullName, unchanged.FullName);
    }

    [Fact]
    public async Task Document_cannot_reference_another_organizations_records()
    {
        var registrar = await app.LoginAsync("registrar", "registrar123");
        var lookups = await app.RpcOkAsync<LookupsDto>(new GetLookups.Query(), registrar.UserId);

        // Записи організації «Вектор» (org2) — очима її адміністратора
        var orgAdmin2 = await UserId("orgadmin2", "orgadmin123");
        var foreignParty = (await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(), orgAdmin2))[0].Id;
        var foreignDepartment = (await app.RpcOkAsync<IReadOnlyList<DepartmentDto>>(new GetDepartments.Query(), orgAdmin2))[0].Id;
        var foreignEmployee = (await app.RpcOkAsync<IReadOnlyList<EmployeeDto>>(new GetEmployees.Query(), orgAdmin2))[0].Id;

        var valid = new DocumentInput(
            lookups.Directions[0].Id, lookups.DocumentTypes[0].Id, lookups.DocumentStatuses[0].Id, null, null,
            null, null, null, "Чужі записи", null, DateOnly.FromDateTime(DateTime.Today), null, false, null,
            null, DateTime.UtcNow.Year + 2, registrar.EmployeeId!.Value);
        DocumentInput[] invalid =
        [
            valid with { CorrespondentId = foreignParty },
            valid with { SenderDepartmentId = foreignDepartment },
            valid with { ReceiverDepartmentId = foreignDepartment },
            valid with { AuthorEmployeeId = foreignEmployee },
            valid with { RegistrarEmployeeId = foreignEmployee },
        ];

        foreach (var input in invalid)
        {
            var create = await app.RpcAsync(new CreateDocument.Command(input), registrar.UserId);
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
            Assert.Contains("організації документа", await create.Content.ReadAsStringAsync());
        }

        var documentId = (await app.RpcOkAsync<PagedResult<DocumentListItem>>(new GetDocuments.Query(), registrar.UserId)).Items[0].Id;
        var current = await app.RpcOkAsync<DocumentInput>(new GetDocumentForEdit.Query(documentId), registrar.UserId);
        DocumentInput[] invalidUpdates =
        [
            current with { CorrespondentId = foreignParty },
            current with { SenderDepartmentId = foreignDepartment },
            current with { ReceiverDepartmentId = foreignDepartment },
            current with { AuthorEmployeeId = foreignEmployee },
        ];
        foreach (var input in invalidUpdates)
        {
            var update = await app.RpcAsync(new UpdateDocument.Command(documentId, input), registrar.UserId);
            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
            Assert.Contains("організації документа", await update.Content.ReadAsStringAsync());
        }

        // Документ зі своїми записами зберігається як і раніше
        await EnsureStatusAsync(await app.RpcAsync(new UpdateDocument.Command(documentId, current), registrar.UserId),
            HttpStatusCode.NoContent);
    }
}
