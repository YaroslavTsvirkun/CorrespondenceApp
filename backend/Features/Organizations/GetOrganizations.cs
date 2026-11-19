using CorrespondenceApi.Features.Organizations.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Organizations;

/// <summary>Слайс: усі організації з лічильниками підрозділів і користувачів.</summary>
public static class GetOrganizations
{
    public record Query : IRequest<IReadOnlyList<OrganizationDto>>;

    public class Handler(Db db) : IRequestHandler<Query, IReadOnlyList<OrganizationDto>>
    {
        public Task<IReadOnlyList<OrganizationDto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<OrganizationDto>("""
                SELECT o.Id, o.Name, o.Code, o.ParentOrganizationId, o.IsActive, o.RegistrationNumberPattern,
                       (SELECT COUNT(*) FROM dbo.Department d WHERE d.OrganizationId = o.Id) AS DepartmentCount,
                       (SELECT COUNT(*) FROM dbo.AppUser u WHERE u.OrganizationId = o.Id) AS UserCount
                FROM dbo.Organization o
                ORDER BY o.Name
                """, null, ct);
    }
}
