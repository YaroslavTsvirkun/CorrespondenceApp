namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>
/// Банківський рахунок контрагента. BankName/Mfo — з довідника банків (для українських IBAN
/// банк визначається за МФО з IBAN) або введена вручну назва іноземного банку.
/// </summary>
public record BankAccountDto(
    int Id, string Iban, int? BankId, string? Mfo, string? BankName, string? SwiftBic,
    string Currency, string? Purpose, string? AccountHolderName, bool IsPrimary,
    DateOnly? ClosedAt, string? Note);

/// <summary>Поля рахунку для створення / редагування (банк визначається сервером за IBAN).</summary>
public record BankAccountInput(
    string Iban, string? BankName, string? SwiftBic, string Currency, string? Purpose,
    string? AccountHolderName, bool IsPrimary, DateOnly? ClosedAt, string? Note);
