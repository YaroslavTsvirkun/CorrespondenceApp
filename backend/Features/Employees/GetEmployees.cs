using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Employees.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Employees;

/// <summary>Слайс: працівники організації користувача.</summary>
public static class GetEmployees
{
    public record Query : IRequest<IReadOnlyList<EmployeeDto>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<EmployeeDto>>
    {
        public Task<IReadOnlyList<EmployeeDto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<EmployeeDto>($"""
                SELECT e.Id, e.FullName, e.PositionName, e.DepartmentId, d.Name AS DepartmentName,
                       e.Phone, e.Email, e.IsActive
                FROM dbo.Employee e
                JOIN dbo.Department d ON d.Id = e.DepartmentId
                WHERE {SqlScope.TenantFilter("e")}
                ORDER BY e.FullName
                """, SqlScope.Tenant(user), ct);
    }
}
