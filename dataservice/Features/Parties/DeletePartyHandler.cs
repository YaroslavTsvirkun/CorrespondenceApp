using CorrespondenceSystem.Contracts.Parties;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Parties;

/// <summary>
/// Дані слайсу: видалення контрагента (dbo.usp_Party_Delete забороняє видаляти контрагента
/// з документів або з філіями).
/// </summary>
public sealed class DeletePartyHandler(Db db, ICurrentUser user) : IRequestHandler<DeleteParty.Command>
{
    public Task Handle(DeleteParty.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        return db.ExecuteProcAsync("dbo.usp_Party_Delete", p, ct);
    }
}
