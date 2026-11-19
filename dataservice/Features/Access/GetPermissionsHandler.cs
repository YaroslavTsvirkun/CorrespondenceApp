using CorrespondenceSystem.Contracts.Access;
using CorrespondenceSystem.Contracts.Access.Dtos;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Access;

/// <summary>Дані слайсу: каталог пермішенів.</summary>
public sealed class GetPermissionsHandler(Db db) : IRequestHandler<GetPermissions.Query, IReadOnlyList<PermissionDto>>
{
    public Task<IReadOnlyList<PermissionDto>> Handle(GetPermissions.Query q, CancellationToken ct) =>
        db.QueryAsync<PermissionDto>(
            "SELECT Code, Description, Category FROM dbo.Permission ORDER BY Category, Id", null, ct);
}
