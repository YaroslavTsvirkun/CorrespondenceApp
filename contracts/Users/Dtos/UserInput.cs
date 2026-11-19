namespace CorrespondenceSystem.Contracts.Users.Dtos;

/// <summary>Поля користувача для створення / редагування (Password = null — не змінювати).</summary>
public record UserInput(string UserName, string? Password, string FullName,
    int? OrganizationId, int? EmployeeId, bool IsActive, List<int> RoleIds);
