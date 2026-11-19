using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Correspondents;

/// <summary>Слайс: видалення кореспондента (у документах посилання обнуляться — FK ON DELETE SET NULL).</summary>
public static class DeleteCorrespondent
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            var affected = await db.ExecuteAsync(
                $"DELETE c FROM dbo.Correspondent c WHERE c.Id = @Id AND {SqlScope.TenantFilter("c")}", p, ct);
            if (affected == 0) throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
        }
    }
}
