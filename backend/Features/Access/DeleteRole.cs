using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Access;

/// <summary>Слайс: видалення ролі (dbo.usp_Role_Delete забороняє видаляти призначену роль).</summary>
public static class DeleteRole
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct) =>
            db.ExecuteProcAsync("dbo.usp_Role_Delete", new { c.Id }, ct);
    }
}
