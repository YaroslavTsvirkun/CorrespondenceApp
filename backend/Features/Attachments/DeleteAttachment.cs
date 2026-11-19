using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Слайс: видалення вкладення — запис у БД (через сервіс даних), потім об'єкт у сховищі.</summary>
public static class DeleteAttachment
{
    public record Command(int Id) : IRequest;

    public class Handler(ISender sender, IFileStorage storage, ILogger<Handler> logger)
        : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var keys = await sender.Send(new DeleteAttachmentRecord.Command(c.Id), ct);
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
