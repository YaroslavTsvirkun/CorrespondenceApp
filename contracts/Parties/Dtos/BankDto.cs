namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>Банк із довідника (МФО — 6 цифр).</summary>
public record BankDto(int Id, string Mfo, string Name, string? ShortName, string? Edrpou,
    string? SwiftBic, bool IsActive, int AccountCount);

/// <summary>Поля банку для створення / редагування.</summary>
public record BankInput(string Mfo, string Name, string? ShortName, string? Edrpou,
    string? SwiftBic, bool IsActive);
