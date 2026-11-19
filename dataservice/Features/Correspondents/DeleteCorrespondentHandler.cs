using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Correspondents;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Correspondents;

/// <summary>Дані слайсу: видалення кореспондента (у документах посилання обнуляться — FK ON DELETE SET NULL).</summary>
public sealed class DeleteCorrespondentHandler(Db db, ICurrentUser user) : IRequestHandler<DeleteCorrespondent.Command>
{
    public async Task Handle(DeleteCorrespondent.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        var affected = await db.ExecuteAsync(
            $"DELETE c FROM dbo.Correspondent c WHERE c.Id = @Id AND {SqlScope.TenantFilter("c")}", p, ct);
        if (affected == 0) throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
    }
}
