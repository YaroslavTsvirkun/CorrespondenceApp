using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Employees;

/// <summary>Слайс: видалення працівника (заборонено, якщо він фігурує в документах / дорученнях).</summary>
public static class DeleteEmployee
{
    [RequirePermission(Permissions.EmployeesManage)]
    public record Command(int Id) : IRequest;
}
