using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Employees.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Employees;

/// <summary>Слайс: створення / оновлення працівника.</summary>
public static class SaveEmployee
{
    public record Command(int? Id, EmployeeInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.FullName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Data.PositionName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Data.DepartmentId).GreaterThan(0).WithMessage("Оберіть підрозділ");
        }
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
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
}
