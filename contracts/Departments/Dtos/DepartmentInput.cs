namespace CorrespondenceSystem.Contracts.Departments.Dtos;

/// <summary>
/// Поля підрозділу для створення / редагування.
/// OrganizationId враховується лише для супер-адміна під час створення.
/// </summary>
public record DepartmentInput(string Name, string? ShortName, string? Description, bool IsActive,
    int? ParentDepartmentId, int? OrganizationId);
