using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Departments;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Departments;

/// <summary>
/// Дані слайсу: видалення підрозділу (dbo.usp_Department_Delete перевіряє наявність
/// працівників і дочірніх підрозділів).
/// </summary>
public sealed class DeleteDepartmentHandler(Db db, ICurrentUser user) : IRequestHandler<DeleteDepartment.Command>
{
    public Task Handle(DeleteDepartment.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        return db.ExecuteProcAsync("dbo.usp_Department_Delete", p, ct);
    }
}
