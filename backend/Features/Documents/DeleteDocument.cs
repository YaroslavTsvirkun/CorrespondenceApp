using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>Слайс: видалення документа (каскадно з реєстрацією, резолюціями, дорученнями).</summary>
public static class DeleteDocument
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            await db.ExecuteProcAsync("dbo.usp_Document_Delete", p, ct);
        }
    }
}
