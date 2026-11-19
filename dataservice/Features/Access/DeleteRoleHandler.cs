using CorrespondenceSystem.Contracts.Access;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Access;

/// <summary>Дані слайсу: видалення ролі (dbo.usp_Role_Delete забороняє видаляти призначену роль).</summary>
public sealed class DeleteRoleHandler(Db db) : IRequestHandler<DeleteRole.Command>
{
    public Task Handle(DeleteRole.Command c, CancellationToken ct) =>
        db.ExecuteProcAsync("dbo.usp_Role_Delete", new { c.Id }, ct);
}
