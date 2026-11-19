namespace CorrespondenceApi.Features.Employees.Dtos;

/// <summary>Поля працівника для створення / редагування (організація — від підрозділу).</summary>
public record EmployeeInput(int DepartmentId, string FullName, string PositionName,
    string? Phone, string? Email, bool IsActive);
