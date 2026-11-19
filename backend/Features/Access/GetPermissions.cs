using CorrespondenceApi.Features.Access.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Access;

/// <summary>Слайс: каталог пермішенів.</summary>
public static class GetPermissions
{
    public record Query : IRequest<IReadOnlyList<PermissionDto>>;

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<PermissionDto>>
    {
        public Task<IReadOnlyList<PermissionDto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<PermissionDto>(
                "SELECT Code, Description, Category FROM dbo.Permission ORDER BY Category, Id", null, ct);
    }
}
