using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Departments.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Departments;

/// <summary>Слайс: підрозділи організації користувача з кількістю працівників.</summary>
public static class GetDepartments
{
    public record Query : IRequest<IReadOnlyList<DepartmentDto>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<DepartmentDto>>
    {
        public Task<IReadOnlyList<DepartmentDto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<DepartmentDto>($"""
                SELECT d.Id, d.Name, d.ShortName, d.Description, d.IsActive,
                       d.OrganizationId, o.Name AS OrganizationName, d.ParentDepartmentId,
                       (SELECT COUNT(*) FROM dbo.Employee e WHERE e.DepartmentId = d.Id) AS EmployeeCount
                FROM dbo.Department d
                JOIN dbo.Organization o ON o.Id = d.OrganizationId
                WHERE {SqlScope.TenantFilter("d")}
                ORDER BY d.Name
                """, SqlScope.Tenant(user), ct);
    }
}
