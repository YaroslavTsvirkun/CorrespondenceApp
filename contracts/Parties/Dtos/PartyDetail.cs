namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>Картка контрагента з усіма дочірніми записами.</summary>
public record PartyDetail(
    int Id, byte Kind, string FullName, string? ShortName, string? Code, int? LegalFormId,
    int? ParentPartyId, bool IsActive, string? Note, int DocumentCount)
{
    public IReadOnlyList<int> CategoryIds { get; init; } = [];
    public IReadOnlyList<PartyAddressItem> Addresses { get; init; } = [];
    public IReadOnlyList<PartyContactItem> Contacts { get; init; } = [];
    public IReadOnlyList<PartyPersonItem> Persons { get; init; } = [];
    public IReadOnlyList<BankAccountDto> BankAccounts { get; init; } = [];
}
