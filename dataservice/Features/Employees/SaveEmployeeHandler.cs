using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Employees;
using CorrespondenceSystem.Contracts.Employees.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Employees;

/// <summary>Дані слайсу: створення / оновлення працівника.</summary>
public sealed class SaveEmployeeHandler(Db db, ICurrentUser user) : IRequestHandler<SaveEmployee.Command, int>
{
    public async Task<int> Handle(SaveEmployee.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.AddDynamicParams(c.Data);
        p.Add("Id", c.Id);

        // Організація працівника = організація підрозділу (підрозділ має бути видимий користувачу)
        var departmentOrg = await db.QuerySingleOrDefaultAsync<int?>($"""
            SELECT d.OrganizationId FROM dbo.Department d
            WHERE d.Id = @DepartmentId AND {SqlScope.TenantFilter("d")}
            """, p, ct)
            ?? throw new ValidationException([new ValidationFailure("department", "Підрозділ не знайдено")]);
        p.Add("OrganizationId", departmentOrg);

        if (c.Id is null)
            return await db.ScalarAsync<int>("""
                INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Phone, Email, IsActive)
                VALUES (@OrganizationId, @DepartmentId, @FullName, @PositionName, @Phone, @Email, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """, p, ct);

        var affected = await db.ExecuteAsync($"""
            UPDATE e
            SET DepartmentId = @DepartmentId, FullName = @FullName, PositionName = @PositionName,
                Phone = @Phone, Email = @Email, IsActive = @IsActive
            FROM dbo.Employee e
            WHERE e.Id = @Id AND {SqlScope.TenantFilter("e")}
            """, p, ct);
        if (affected == 0) throw new NotFoundException($"Працівника #{c.Id} не знайдено");
        return c.Id.Value;
    }
}
