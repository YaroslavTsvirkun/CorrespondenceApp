namespace CorrespondenceApi.Features.Access.Dtos;

/// <summary>Пермішен з каталогу прав (dbo.Permission).</summary>
public record PermissionDto(string Code, string? Description, string? Category);
