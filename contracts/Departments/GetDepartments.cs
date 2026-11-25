using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Departments.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Departments;

/// <summary>Слайс: підрозділи організації користувача з кількістю працівників.</summary>
public static class GetDepartments
{
    [RequirePermission]
    public record Query : IRequest<IReadOnlyList<DepartmentDto>>;
}
