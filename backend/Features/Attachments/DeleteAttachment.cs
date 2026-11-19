using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using CorrespondenceApi.Infrastructure.Storage;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Слайс: видалення вкладення — запис у БД (dbo.usp_Attachment_Delete), потім об'єкт у сховищі.</summary>
public static class DeleteAttachment
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user, IFileStorage storage, ILogger<Handler> logger)
        : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            var keys = await db.QueryProcAsync<string>("dbo.usp_Attachment_Delete", p, ct);
            await AttachmentCleanup.DeleteObjectsAsync(storage, keys, logger);
        }
    }
}

/// <summary>Видалення об'єктів зі сховища після видалення записів у БД (best effort).</summary>
public static class AttachmentCleanup
{
    public static async Task DeleteObjectsAsync(IFileStorage storage, IEnumerable<string> keys, ILogger logger)
    {
        foreach (var key in keys)
        {
            // Запис у БД уже видалено: збій сховища не повинен ламати операцію користувача
            try { await storage.DeleteAsync(key, CancellationToken.None); }
            catch (Exception ex) { logger.LogWarning(ex, "Не вдалося видалити об'єкт {Key} зі сховища", key); }
        }
    }
}
