using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Employees;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Data.SqlClient;

namespace CorrespondenceSystem.DataService.Features.Employees;

/// <summary>Дані слайсу: видалення працівника (заборонено, якщо він фігурує в документах / дорученнях).</summary>
public sealed class DeleteEmployeeHandler(Db db, ICurrentUser user) : IRequestHandler<DeleteEmployee.Command>
{
    public async Task Handle(DeleteEmployee.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        try
        {
            var affected = await db.ExecuteAsync(
                $"DELETE e FROM dbo.Employee e WHERE e.Id = @Id AND {SqlScope.TenantFilter("e")}", p, ct);
            if (affected == 0) throw new NotFoundException($"Працівника #{c.Id} не знайдено");
        }
        catch (SqlException ex) when (SqlErrors.IsReferenceViolation(ex))
        {
            throw new ValidationException(
                [new ValidationFailure("employee", "Працівник використовується в документах/дорученнях — видалення заборонено")]);
        }
    }
}
