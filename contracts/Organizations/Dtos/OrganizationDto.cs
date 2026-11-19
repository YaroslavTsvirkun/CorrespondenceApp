namespace CorrespondenceSystem.Contracts.Organizations.Dtos;

/// <summary>Організація з кількістю підрозділів і користувачів.</summary>
public record OrganizationDto(int Id, string Name, string? Code, int? ParentOrganizationId, bool IsActive,
    string RegistrationNumberPattern, int DepartmentCount, int UserCount);
