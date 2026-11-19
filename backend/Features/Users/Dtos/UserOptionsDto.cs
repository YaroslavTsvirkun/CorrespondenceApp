namespace CorrespondenceApi.Features.Users.Dtos;

/// <summary>Довідники для форми користувача з урахуванням зони видимості адміністратора.</summary>
public record UserOptionsDto(bool IsSuperAdmin, IReadOnlyList<OrganizationOption> Organizations,
    IReadOnlyList<RoleOption> Roles, IReadOnlyList<EmployeeOption> Employees);
