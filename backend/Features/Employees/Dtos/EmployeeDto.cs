namespace CorrespondenceApi.Features.Employees.Dtos;

/// <summary>Працівник із назвою підрозділу.</summary>
public record EmployeeDto(int Id, string FullName, string PositionName, int DepartmentId,
    string DepartmentName, string? Phone, string? Email, bool IsActive);
