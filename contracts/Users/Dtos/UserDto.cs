namespace CorrespondenceSystem.Contracts.Users.Dtos;

/// <summary>Користувач у списку: організація, працівник, ролі, ознака супер-адміна.</summary>
public record UserDto(int Id, string UserName, string FullName, int? OrganizationId,
    string? OrganizationName, int? EmployeeId, string? EmployeeName, bool IsActive,
    bool IsSuperAdmin, IReadOnlyList<string> Roles);
