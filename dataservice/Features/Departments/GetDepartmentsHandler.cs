using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Departments;
using CorrespondenceSystem.Contracts.Departments.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Departments;

/// <summary>Дані слайсу: підрозділи організації користувача з кількістю працівників.</summary>
public sealed class GetDepartmentsHandler(Db db, ICurrentUser user) : IRequestHandler<GetDepartments.Query, IReadOnlyList<DepartmentDto>>
{
    public Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartments.Query q, CancellationToken ct) =>
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
