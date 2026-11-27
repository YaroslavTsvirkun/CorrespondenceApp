using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Parties;

/// <summary>
/// Слайс: створення / оновлення банківського рахунку контрагента. Окреме право parties.bank.manage:
/// підміна реквізитів — типовий шлях шахрайства, тож вести контрагентів і змінювати рахунки — різні ролі.
/// </summary>
public static class SaveBankAccount
{
    [RequirePermission(Permissions.PartiesBankManage)]
    public record Command(int PartyId, int? Id, BankAccountInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Iban).Must(PartyRules.IsValidIban)
                .WithMessage("Невірний IBAN (формат або контрольна сума)");
            RuleFor(x => x.Data.Currency).Must(c => c is not null && PartyRules.Currency().IsMatch(c))
                .WithMessage("Валюта — трилітерний код ISO 4217 (UAH, USD, EUR…)");
            RuleFor(x => x.Data.SwiftBic)
                .Must(s => PartyRules.SwiftBic().IsMatch(s!)).When(x => !string.IsNullOrEmpty(x.Data.SwiftBic))
                .WithMessage("Невірний SWIFT/BIC (8 або 11 символів)");
            // Для іноземного рахунку банк не визначити за МФО — потрібна назва або SWIFT
            RuleFor(x => x.Data.BankName)
                .Must((x, name) => !string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(x.Data.SwiftBic))
                .When(x => PartyRules.IsValidIban(x.Data.Iban) && PartyRules.MfoFromIban(x.Data.Iban) is null)
                .WithMessage("Для іноземного рахунку вкажіть назву банку або SWIFT/BIC");
            RuleFor(x => x.Data.BankName).MaximumLength(200);
            RuleFor(x => x.Data.Purpose).MaximumLength(100);
            RuleFor(x => x.Data.AccountHolderName).MaximumLength(300);
            RuleFor(x => x.Data.Note).MaximumLength(200);
        }
    }
}

/// <summary>Слайс: видалення банківського рахунку контрагента.</summary>
public static class DeleteBankAccount
{
    [RequirePermission(Permissions.PartiesBankManage)]
    public record Command(int PartyId, int Id) : IRequest;
}

/// <summary>Слайс: довідник банків.</summary>
public static class GetBanks
{
    [RequirePermission]
    public record Query : IRequest<IReadOnlyList<BankDto>>;
}

/// <summary>Слайс: створення / оновлення банку в довіднику.</summary>
public static class SaveBank
{
    [RequirePermission(Permissions.PartiesBankManage)]
    public record Command(int? Id, BankInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Mfo).Must(m => m is not null && PartyRules.Mfo().IsMatch(m))
                .WithMessage("МФО — 6 цифр");
            RuleFor(x => x.Data.Name).NotEmpty().WithMessage("Вкажіть назву банку").MaximumLength(200);
            RuleFor(x => x.Data.ShortName).MaximumLength(100);
            RuleFor(x => x.Data.Edrpou).Must(PartyRules.IsValidEdrpou).When(x => !string.IsNullOrEmpty(x.Data.Edrpou))
                .WithMessage("Невірний код ЄДРПОУ банку");
            RuleFor(x => x.Data.SwiftBic)
                .Must(s => PartyRules.SwiftBic().IsMatch(s!)).When(x => !string.IsNullOrEmpty(x.Data.SwiftBic))
                .WithMessage("Невірний SWIFT/BIC (8 або 11 символів)");
        }
    }
}

/// <summary>Слайс: видалення банку (заборонено, якщо в ньому є рахунки).</summary>
public static class DeleteBank
{
    [RequirePermission(Permissions.PartiesBankManage)]
    public record Command(int Id) : IRequest;
}

/// <summary>Слайс: журнал змін банківських рахунків контрагента (нові записи — першими).</summary>
public static class GetBankAccountHistory
{
    [RequirePermission(Permissions.PartiesRead)]
    public record Query(int PartyId) : IRequest<IReadOnlyList<BankAccountHistoryItem>>;
}
