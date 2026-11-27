namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>
/// Запис журналу змін банківського рахунку. Action: 1 — створено, 2 — змінено, 3 — видалено.
/// OldValues / NewValues — JSON рахунку до й після зміни (IBAN, банк, валюта, призначення…).
/// ChangedBy — ПІБ користувача або null, якщо зміна зроблена поза застосунком.
/// </summary>
public record BankAccountHistoryItem(int Id, int AccountId, byte Action, DateTime ChangedAt, string? ChangedBy,
    string? OldValues, string? NewValues);
