using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Employees.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Employees;

/// <summary>Слайс: працівники організації користувача.</summary>
public static class GetEmployees
{
    public record Query : IRequest<IReadOnlyList<EmployeeDto>>;
}
