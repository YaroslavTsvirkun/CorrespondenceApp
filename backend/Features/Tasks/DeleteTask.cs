using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Tasks;

/// <summary>Слайс: видалення доручення (dbo.usp_Task_Delete).</summary>
public static class DeleteTask
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            return db.ExecuteProcAsync("dbo.usp_Task_Delete", p, ct);
        }
    }
}
