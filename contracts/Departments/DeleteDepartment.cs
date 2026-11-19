using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceSystem.Contracts.Departments;

/// <summary>
/// Слайс: видалення підрозділу (dbo.usp_Department_Delete перевіряє наявність
/// працівників і дочірніх підрозділів).
/// </summary>
public static class DeleteDepartment
{
    public record Command(int Id) : IRequest;
}
