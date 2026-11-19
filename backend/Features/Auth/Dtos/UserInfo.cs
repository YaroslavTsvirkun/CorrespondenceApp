namespace CorrespondenceApi.Features.Auth.Dtos;

/// <summary>Поточний користувач: профіль, організація, ролі та пермішени.</summary>
public record UserInfo(
    int UserId, string UserName, string FullName, int? EmployeeId, int? DepartmentId,
    int? OrganizationId, string? OrganizationName, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
