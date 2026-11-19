namespace CorrespondenceSystem.Contracts.Organizations.Dtos;

/// <summary>Поля організації для створення / редагування (порожній патерн — не змінювати).</summary>
public record OrganizationInput(string Name, string? Code, int? ParentOrganizationId, bool IsActive,
    string? RegistrationNumberPattern);
