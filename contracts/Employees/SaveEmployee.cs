using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Employees.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Employees;

/// <summary>Слайс: створення / оновлення працівника.</summary>
public static class SaveEmployee
{
    [RequirePermission(Permissions.EmployeesManage)]
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
}
