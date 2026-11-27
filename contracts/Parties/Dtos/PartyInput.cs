namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>
/// Поля контрагента для створення / редагування. Адреси, контакти, контактні особи й категорії
/// зберігаються разом із картою (повністю замінюються); банківські рахунки — окремо (інше право).
/// </summary>
public record PartyInput(
    byte Kind, string FullName, string? ShortName, string? Code, int? LegalFormId,
    int? ParentPartyId, bool IsActive, string? Note)
{
    public IReadOnlyList<int>? CategoryIds { get; init; }
    public IReadOnlyList<PartyAddressItem>? Addresses { get; init; }
    public IReadOnlyList<PartyContactItem>? Contacts { get; init; }
    public IReadOnlyList<PartyPersonItem>? Persons { get; init; }
}
