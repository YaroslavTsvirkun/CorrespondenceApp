namespace CorrespondenceApi.Features.Access.Dtos;

/// <summary>Роль з кількістю користувачів і кодами пермішенів.</summary>
public record RoleDto(int Id, string Name, string? Description, int UserCount, IReadOnlyList<string> Permissions);
