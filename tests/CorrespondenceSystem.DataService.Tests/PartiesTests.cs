using System.Net;
using CorrespondenceSystem.Contracts.Dictionaries;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using CorrespondenceSystem.Contracts.Lookups;
using CorrespondenceSystem.Contracts.Lookups.Dtos;
using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using static CorrespondenceSystem.DataService.Tests.DataServiceFactory;

namespace CorrespondenceSystem.DataService.Tests;

/// <summary>
/// Реєстр контрагентів на демо-даних: registrar веде контрагентів (parties.manage), але не рахунки;
/// orgadmin1 має й parties.bank.manage.
/// </summary>
[Collection(DemoDatabaseCollection.Name)]
public sealed class PartiesTests(DataServiceFactory app)
{
    // Вигадані реквізити з правильними контрольними сумами
    private const string ValidEdrpou = "40123454";
    private const string PrivatIban = "UA223052990000026001234567890";

    private async Task<int> UserId(string userName, string password) => (await app.LoginAsync(userName, password)).UserId;
    private Task<int> Registrar() => UserId("registrar", "registrar123");
    private Task<int> OrgAdmin() => UserId("orgadmin1", "orgadmin123");

    private static PartyInput NewParty(string name, string? code = null) =>
        new(PartyKind.LegalEntity, name, null, code, null, null, true, null);

    // ---- Правила кодів ----

    [Theory]
    [InlineData("32145678", true)]
    [InlineData("02142632", true)]
    [InlineData("40123454", true)]
    [InlineData("32145679", false)]
    [InlineData("3214567", false)]
    [InlineData("3214567a", false)]
    public void Edrpou_checksum(string code, bool valid) => Assert.Equal(valid, PartyRules.IsValidEdrpou(code));

    [Theory]
    [InlineData("3124567896", true)]
    [InlineData("2987654320", true)]
    [InlineData("3124567895", false)]
    [InlineData("312456789", false)]
    public void Rnokpp_checksum(string code, bool valid) => Assert.Equal(valid, PartyRules.IsValidRnokpp(code));

    [Theory]
    [InlineData("UA183052990000026007052612345", true)]
    [InlineData("ua18 3052 9900 0002 6007 0526 1234 5", true)]
    [InlineData("UA183052990000026007052612346", false)]
    [InlineData("UA1830529900000260070526123", false)]
    [InlineData("DE89370400440532013000", true)]
    [InlineData("DE89370400440532013001", false)]
    public void Iban_checksum(string iban, bool valid) => Assert.Equal(valid, PartyRules.IsValidIban(iban));

    [Fact]
    public void Mfo_is_taken_from_ukrainian_iban()
    {
        Assert.Equal("305299", PartyRules.MfoFromIban("UA18 3052 9900 0002 6007 0526 1234 5"));
        Assert.Null(PartyRules.MfoFromIban("DE89370400440532013000"));
    }

    // ---- Картка контрагента ----

    [Fact]
    public async Task Party_is_saved_with_addresses_contacts_persons_and_categories()
    {
        var registrar = await Registrar();
        var category = (await app.RpcOkAsync<IReadOnlyList<DictionaryItemDto>>(new GetDictionary.Query("party-categories"), registrar))[0];
        var input = NewParty("ТОВ «Тест-картка»", ValidEdrpou) with
        {
            CategoryIds = [category.Id],
            Addresses = [new PartyAddressItem(1, "Україна", "61000", "Харківська", "Харків", "вул. Тестова, 1")],
            Contacts = [new PartyContactItem(1, "057-000-00-00", true, null), new PartyContactItem(2, "test@example.com", true, null)],
            Persons = [new PartyPersonItem("Тестенко Т.Т.", "Директор", null, null, true, null)],
        };

        var id = await app.RpcOkAsync<int>(new SaveParty.Command(null, input), registrar);
        try
        {
            var saved = await app.RpcOkAsync<PartyDetail>(new GetParty.Query(id), registrar);
            Assert.Equal(ValidEdrpou, saved.Code);
            Assert.Equal([category.Id], saved.CategoryIds);
            Assert.Single(saved.Addresses);
            Assert.Equal(2, saved.Contacts.Count);
            Assert.True(saved.Persons.Single().IsSignatory);

            // Повторне збереження замінює дочірні записи, а не додає
            await EnsureStatusAsync(await app.RpcAsync(new SaveParty.Command(id, input with { Contacts = [] }), registrar),
                HttpStatusCode.OK);
            var updated = await app.RpcOkAsync<PartyDetail>(new GetParty.Query(id), registrar);
            Assert.Empty(updated.Contacts);
            Assert.Single(updated.Addresses);

            var listed = await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(ValidEdrpou), registrar);
            Assert.Equal(id, listed.Single().Id);
        }
        finally
        {
            await EnsureStatusAsync(await app.RpcAsync(new DeleteParty.Command(id), registrar), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Invalid_code_and_duplicate_code_are_rejected()
    {
        var registrar = await Registrar();

        var invalid = await app.RpcAsync(new SaveParty.Command(null, NewParty("Невірний код", "12345679")), registrar);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        // ФОП має РНОКПП, а не ЄДРПОУ
        var wrongKind = await app.RpcAsync(new SaveParty.Command(null,
            NewParty("ФОП із ЄДРПОУ", ValidEdrpou) with { Kind = PartyKind.Entrepreneur }), registrar);
        Assert.Equal(HttpStatusCode.BadRequest, wrongKind.StatusCode);

        // Код демо-контрагента «Промтехбуд»
        var duplicate = await app.RpcAsync(new SaveParty.Command(null, NewParty("Дублікат", "32145678")), registrar);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Contains("Промтехбуд", await duplicate.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Party_used_in_documents_cannot_be_deleted()
    {
        var registrar = await Registrar();
        var used = (await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(), registrar))
            .First(p => p.DocumentCount > 0);

        var delete = await app.RpcAsync(new DeleteParty.Command(used.Id), registrar);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    [Fact]
    public async Task Branch_cannot_become_parent_of_its_parent()
    {
        var registrar = await Registrar();
        var parties = await app.RpcOkAsync<IReadOnlyList<PartyListItem>>(new GetParties.Query(), registrar);
        var branch = parties.First(p => p.ParentPartyId is not null);
        var parent = await app.RpcOkAsync<PartyDetail>(new GetParty.Query(branch.ParentPartyId!.Value), registrar);

        var cycle = await app.RpcAsync(new SaveParty.Command(parent.Id, new PartyInput(
            parent.Kind, parent.FullName, parent.ShortName, parent.Code, parent.LegalFormId, branch.Id, true, null)), registrar);
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
    }

    [Fact]
    public async Task Lookups_offer_parties_as_document_correspondents()
    {
        var lookups = await app.RpcOkAsync<LookupsDto>(new GetLookups.Query(), await Registrar());
        Assert.Contains(lookups.Correspondents, c => c.Name.Contains("Промтехбуд") && c.Name.Contains("32145678"));
    }

    // ---- Банківські рахунки ----

    [Fact]
    public async Task Bank_accounts_need_their_own_permission()
    {
        var registrar = await Registrar();
        var partyId = await app.RpcOkAsync<int>(new SaveParty.Command(null, NewParty("ТОВ «Тест-рахунки»")), registrar);
        try
        {
            var account = new BankAccountInput(PrivatIban, null, null, "UAH", "Поточний", null, true, null, null);

            // Реєстратор веде контрагентів, але не банківські реквізити
            var forbidden = await app.RpcAsync(new SaveBankAccount.Command(partyId, null, account), registrar);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

            var orgAdmin = await OrgAdmin();
            var first = await app.RpcOkAsync<int>(new SaveBankAccount.Command(partyId, null, account), orgAdmin);

            // Банк визначено за МФО з IBAN
            var saved = (await app.RpcOkAsync<PartyDetail>(new GetParty.Query(partyId), registrar)).BankAccounts.Single();
            Assert.Equal("305299", saved.Mfo);
            Assert.NotNull(saved.BankId);

            // Той самий IBAN удруге — помилка; новий основний UAH-рахунок знімає позначку з попереднього
            var duplicate = await app.RpcAsync(new SaveBankAccount.Command(partyId, null, account), orgAdmin);
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

            var secondIban = "UA433004650000026004500123456";
            await app.RpcOkAsync<int>(new SaveBankAccount.Command(partyId, null, account with { Iban = secondIban }), orgAdmin);
            var accounts = (await app.RpcOkAsync<PartyDetail>(new GetParty.Query(partyId), registrar)).BankAccounts;
            Assert.Equal(secondIban, accounts.Single(a => a.IsPrimary).Iban);
            Assert.False(accounts.Single(a => a.Id == first).IsPrimary);

            var invalid = await app.RpcAsync(new SaveBankAccount.Command(partyId, null,
                account with { Iban = "UA183052990000026007052612346" }), orgAdmin);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

            // Іноземний рахунок — без назви банку чи SWIFT не приймається
            var foreign = await app.RpcAsync(new SaveBankAccount.Command(partyId, null,
                account with { Iban = "DE89370400440532013000", Currency = "EUR" }), orgAdmin);
            Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
            await app.RpcOkAsync<int>(new SaveBankAccount.Command(partyId, null,
                account with { Iban = "DE89370400440532013000", Currency = "EUR", SwiftBic = "COBADEFFXXX" }), orgAdmin);
        }
        finally
        {
            await EnsureStatusAsync(await app.RpcAsync(new DeleteParty.Command(partyId), registrar), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Bank_account_changes_are_logged_with_user_and_values()
    {
        var registrar = await Registrar();
        var orgAdmin = await OrgAdmin();
        var partyId = await app.RpcOkAsync<int>(new SaveParty.Command(null, NewParty("ТОВ «Тест-журнал»")), registrar);
        try
        {
            var account = new BankAccountInput("UA423052990000026001111222233", null, null, "UAH", "Поточний", null, true, null, null);
            var id = await app.RpcOkAsync<int>(new SaveBankAccount.Command(partyId, null, account), orgAdmin);
            await EnsureStatusAsync(await app.RpcAsync(new SaveBankAccount.Command(partyId, id,
                account with { Iban = "UA953052990000026004444555566" }), orgAdmin), HttpStatusCode.OK);

            // Повторне збереження без змін журнал не засмічує
            await EnsureStatusAsync(await app.RpcAsync(new SaveBankAccount.Command(partyId, id,
                account with { Iban = "UA953052990000026004444555566" }), orgAdmin), HttpStatusCode.OK);

            var saved = (await app.RpcOkAsync<PartyDetail>(new GetParty.Query(partyId), registrar)).BankAccounts.Single();
            Assert.Equal("Адмін «Промінь»", saved.LastChangedBy);
            Assert.NotNull(saved.LastChangedAt);

            await EnsureStatusAsync(await app.RpcAsync(new DeleteBankAccount.Command(partyId, id), orgAdmin), HttpStatusCode.NoContent);

            // Реєстратор (parties.manage) бачить журнал, хоча змінювати рахунки не може
            var history = await app.RpcOkAsync<IReadOnlyList<BankAccountHistoryItem>>(new GetBankAccountHistory.Query(partyId), registrar);
            Assert.Equal([(byte)3, (byte)2, (byte)1], history.Select(h => h.Action));
            Assert.All(history, h => Assert.Equal("Адмін «Промінь»", h.ChangedBy));
            var change = history[1];
            Assert.Contains("UA423052990000026001111222233", change.OldValues);
            Assert.Contains("UA953052990000026004444555566", change.NewValues);
            Assert.Contains("ПриватБанк", change.NewValues);
            Assert.Null(history[0].NewValues);

            var executor = await UserId("executor", "executor123");
            var forbidden = await app.RpcAsync(new GetBankAccountHistory.Query(partyId), executor);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }
        finally
        {
            await EnsureStatusAsync(await app.RpcAsync(new DeleteParty.Command(partyId), registrar), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Linking_account_to_new_bank_is_not_logged_as_change()
    {
        var registrar = await Registrar();
        var orgAdmin = await OrgAdmin();
        var partyId = await app.RpcOkAsync<int>(new SaveParty.Command(null, NewParty("ТОВ «Тест-новий банк»")), registrar);
        int? bankId = null;
        try
        {
            // МФО 399999 немає в довіднику — рахунок без банку
            await app.RpcOkAsync<int>(new SaveBankAccount.Command(partyId, null, new BankAccountInput(
                "UA033999990000026007777888899", "Тестовий банк", null, "UAH", null, null, true, null, null)), orgAdmin);
            bankId = await app.RpcOkAsync<int>(new SaveBank.Command(null,
                new BankInput("399999", "АТ «Тестовий банк»", null, null, null, true)), orgAdmin);

            var account = (await app.RpcOkAsync<PartyDetail>(new GetParty.Query(partyId), registrar)).BankAccounts.Single();
            Assert.Equal(bankId, account.BankId);
            var history = await app.RpcOkAsync<IReadOnlyList<BankAccountHistoryItem>>(new GetBankAccountHistory.Query(partyId), registrar);
            Assert.Single(history);
        }
        finally
        {
            await EnsureStatusAsync(await app.RpcAsync(new DeleteParty.Command(partyId), registrar), HttpStatusCode.NoContent);
            if (bankId is int b)
                await EnsureStatusAsync(await app.RpcAsync(new DeleteBank.Command(b), orgAdmin), HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Bank_with_accounts_cannot_be_deleted()
    {
        var orgAdmin = await OrgAdmin();
        var bank = (await app.RpcOkAsync<IReadOnlyList<BankDto>>(new GetBanks.Query(), orgAdmin)).First(b => b.AccountCount > 0);

        var delete = await app.RpcAsync(new DeleteBank.Command(bank.Id), orgAdmin);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);

        var registrarDelete = await app.RpcAsync(new DeleteBank.Command(bank.Id), await Registrar());
        Assert.Equal(HttpStatusCode.Forbidden, registrarDelete.StatusCode);
    }

    // ---- Довідники контрагентів ----

    [Fact]
    public async Task Party_dictionaries_are_managed_with_parties_permission()
    {
        var registrar = await Registrar();
        var id = await app.RpcOkAsync<int>(new SaveDictionaryItem.Command("party-categories", null,
            new DictionaryItemInput("Тестова категорія", null, null)), registrar);
        await EnsureStatusAsync(await app.RpcAsync(new DeleteDictionaryItem.Command("party-categories", id), registrar),
            HttpStatusCode.NoContent);

        // Виконавець не має ні dictionaries.manage, ні parties.manage
        var executor = await UserId("executor", "executor123");
        var forbidden = await app.RpcAsync(new SaveDictionaryItem.Command("legal-forms", null,
            new DictionaryItemInput("Нова форма", null, "НФ")), executor);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}
