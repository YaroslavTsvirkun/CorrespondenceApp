using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Слайс: вміст попередньої версії вкладення (для завантаження).</summary>
public static class DownloadAttachmentVersion
{
    public record Query(int AttachmentId, int VersionId) : IRequest<DownloadAttachment.Result>;

    public class Handler(ISender sender, IFileStorage storage) : IRequestHandler<Query, DownloadAttachment.Result>
    {
        public async Task<DownloadAttachment.Result> Handle(Query q, CancellationToken ct)
        {
            var file = await sender.Send(new GetAttachmentVersionFile.Query(q.AttachmentId, q.VersionId), ct);
            try
            {
                var obj = await storage.OpenReadAsync(file.StorageKey, ct);
                return new DownloadAttachment.Result(obj.Content, obj.Length, file.FileName, file.ContentType);
            }
            catch (FileNotFoundException)
            {
                throw new NotFoundException($"Файл версії #{q.VersionId} відсутній у сховищі");
            }
        }
    }
}

/// <summary>
/// Слайс: відновлення попередньої версії — її вміст копіюється як новий поточний,
/// а поточний вміст сам стає версією (історія не губиться).
/// </summary>
public static class RestoreAttachmentVersion
{
    public record Command(int AttachmentId, int VersionId, int? UserId) : IRequest;

    public class Handler(ISender sender, IFileStorage storage, AttachmentContentService content)
        : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var version = await sender.Send(new GetAttachmentVersionFile.Query(c.AttachmentId, c.VersionId), ct);
            StoredObject obj;
            try { obj = await storage.OpenReadAsync(version.StorageKey, ct); }
            catch (FileNotFoundException) { throw new NotFoundException($"Файл версії #{c.VersionId} відсутній у сховищі"); }

            await using (obj.Content)
                await content.ReplaceAsync(c.AttachmentId, version.OrganizationId, version.FileName, obj.Content, c.UserId, ct);
        }
    }
}
