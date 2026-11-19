using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Tasks;

/// <summary>Дані слайсу: видалення доручення (dbo.usp_Task_Delete).</summary>
public sealed class DeleteTaskHandler(Db db, ICurrentUser user) : IRequestHandler<DeleteTask.Command>
{
    public Task Handle(DeleteTask.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        return db.ExecuteProcAsync("dbo.usp_Task_Delete", p, ct);
    }
}
