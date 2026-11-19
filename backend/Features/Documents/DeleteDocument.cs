using CorrespondenceApi.Features.Attachments;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Documents;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>
/// Слайс: видалення документа (каскадно з реєстрацією, резолюціями, дорученнями, вкладеннями)
/// у сервісі даних; файли вкладень після цього видаляються зі сховища.
/// </summary>
public static class DeleteDocument
{
    public record Command(int Id) : IRequest;

    public class Handler(ISender sender, IFileStorage storage, ILogger<Handler> logger)
        : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var keys = await sender.Send(new DeleteDocumentRecord.Command(c.Id), ct);
            await AttachmentCleanup.DeleteObjectsAsync(storage, keys, logger);
        }
    }
}
