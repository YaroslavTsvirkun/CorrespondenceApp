using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Employees;
using CorrespondenceSystem.Contracts.Employees.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Employees;

/// <summary>Дані слайсу: працівники організації користувача.</summary>
public sealed class GetEmployeesHandler(Db db, ICurrentUser user) : IRequestHandler<GetEmployees.Query, IReadOnlyList<EmployeeDto>>
{
    public Task<IReadOnlyList<EmployeeDto>> Handle(GetEmployees.Query q, CancellationToken ct) =>
        db.QueryAsync<EmployeeDto>($"""
            SELECT e.Id, e.FullName, e.PositionName, e.DepartmentId, d.Name AS DepartmentName,
                   e.Phone, e.Email, e.IsActive
            FROM dbo.Employee e
            JOIN dbo.Department d ON d.Id = e.DepartmentId
            WHERE {SqlScope.TenantFilter("e")}
            ORDER BY e.FullName
            """, SqlScope.Tenant(user), ct);
}
