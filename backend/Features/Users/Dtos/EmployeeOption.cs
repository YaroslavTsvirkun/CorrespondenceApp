namespace CorrespondenceApi.Features.Users.Dtos;

/// <summary>Працівник у формі користувача (для прив'язки облікового запису).</summary>
public record EmployeeOption(int Id, string FullName, string DepartmentName, int OrganizationId);
