using CorrespondenceSystem.Contracts.Organizations;
using CorrespondenceSystem.Contracts.Organizations.Dtos;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Organizations;

/// <summary>Дані слайсу: усі організації з лічильниками підрозділів і користувачів.</summary>
public sealed class GetOrganizationsHandler(Db db) : IRequestHandler<GetOrganizations.Query, IReadOnlyList<OrganizationDto>>
{
    public Task<IReadOnlyList<OrganizationDto>> Handle(GetOrganizations.Query q, CancellationToken ct) =>
        db.QueryAsync<OrganizationDto>("""
            SELECT o.Id, o.Name, o.Code, o.ParentOrganizationId, o.IsActive, o.RegistrationNumberPattern,
                   (SELECT COUNT(*) FROM dbo.Department d WHERE d.OrganizationId = o.Id) AS DepartmentCount,
                   (SELECT COUNT(*) FROM dbo.AppUser u WHERE u.OrganizationId = o.Id) AS UserCount
            FROM dbo.Organization o
            ORDER BY o.Name
            """, null, ct);
}
