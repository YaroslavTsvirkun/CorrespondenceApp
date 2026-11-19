using CorrespondenceApi.Features.Access.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceApi.Features.Access;

/// <summary>Слайс: ролі з кількістю користувачів і пермішенами.</summary>
public static class GetRoles
{
    public record Query : IRequest<IReadOnlyList<RoleDto>>;

    // Проміжні рядки двох наборів результатів
    private sealed record RoleRow(int Id, string Name, string? Description, int UserCount);
    private sealed record RolePermissionRow(int RoleId, string Code);

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<RoleDto>>
    {
        public async Task<IReadOnlyList<RoleDto>> Handle(Query q, CancellationToken ct)
        {
            await using var c = await db.OpenAsync(ct);
            using var grid = await c.QueryMultipleAsync(new CommandDefinition("""
                SELECT r.Id, r.Name, r.Description,
                       (SELECT COUNT(*) FROM dbo.UserRole ur WHERE ur.RoleId = r.Id) AS UserCount
                FROM dbo.Role r
                ORDER BY r.Name;

                SELECT rp.RoleId, p.Code
                FROM dbo.RolePermission rp
                JOIN dbo.Permission p ON p.Id = rp.PermissionId;
                """, cancellationToken: ct));
            var roles = await grid.ReadAsync<RoleRow>();
            var perms = (await grid.ReadAsync<RolePermissionRow>()).ToLookup(x => x.RoleId, x => x.Code);

            return roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.UserCount, perms[r.Id].ToList()))
                .ToList();
        }
    }
}
