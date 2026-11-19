namespace CorrespondenceSystem.Contracts.Departments.Dtos;

/// <summary>Підрозділ з організацією та кількістю працівників.</summary>
public record DepartmentDto(int Id, string Name, string? ShortName, string? Description, bool IsActive,
    int OrganizationId, string OrganizationName, int? ParentDepartmentId, int EmployeeCount);
