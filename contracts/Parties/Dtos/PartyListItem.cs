namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>Рядок реєстру контрагентів: основні реквізити, головна адреса й контакти.</summary>
public record PartyListItem(
    int Id, byte Kind, string FullName, string? ShortName, string? Code, string? LegalForm,
    int? ParentPartyId, string? ParentName, bool IsActive,
    string? Address, string? Phone, string? Email, string? Categories,
    int DocumentCount, int BankAccountCount);
