namespace CorrespondenceSystem.Contracts.Access.Dtos;

/// <summary>Поля ролі для створення / редагування разом із кодами пермішенів.</summary>
public record RoleInput(string Name, string? Description, List<string> Permissions);
