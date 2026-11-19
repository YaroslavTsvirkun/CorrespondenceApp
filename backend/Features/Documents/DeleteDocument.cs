using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Attachments;
using CorrespondenceApi.Infrastructure.Database;
using CorrespondenceApi.Infrastructure.Storage;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>
/// Слайс: видалення документа (каскадно з реєстрацією, резолюціями, дорученнями, вкладеннями).
/// Файли вкладень після цього видаляються зі сховища.
/// </summary>
public static class DeleteDocument
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user, IFileStorage storage, ILogger<Handler> logger)
        : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            var keys = await db.QueryProcAsync<string>("dbo.usp_Document_Delete", p, ct);
            await AttachmentCleanup.DeleteObjectsAsync(storage, keys, logger);
        }
    }
}
