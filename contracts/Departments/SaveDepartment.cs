using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Departments.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Departments;

/// <summary>Слайс: створення / оновлення підрозділу.</summary>
public static class SaveDepartment
{
    public record Command(int? Id, DepartmentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(150);
    }
}
