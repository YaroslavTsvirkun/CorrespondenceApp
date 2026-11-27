namespace CorrespondenceSystem.Contracts.Parties.Dtos;

/// <summary>Адреса контрагента. Kind: 1 — юридична, 2 — фактична, 3 — поштова.</summary>
public record PartyAddressItem(byte Kind, string? Country, string? PostalCode, string? Region,
    string? City, string? Line);

/// <summary>Контакт. Kind: 1 — телефон, 2 — email, 3 — сайт, 4 — адреса для обміну документами.</summary>
public record PartyContactItem(byte Kind, string Value, bool IsPrimary, string? Note);

/// <summary>Контактна особа або підписант контрагента.</summary>
public record PartyPersonItem(string FullName, string? PositionName, string? Phone, string? Email,
    bool IsSignatory, string? Note);
