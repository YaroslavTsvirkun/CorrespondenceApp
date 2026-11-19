using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Слайс: вміст вкладення (метадані — із сервісу даних, потік — зі сховища).</summary>
public static class DownloadAttachment
{
    public record Query(int Id) : IRequest<Result>;

    public sealed record Result(Stream Content, long Length, string FileName, string ContentType);

    public class Handler(ISender sender, IFileStorage storage) : IRequestHandler<Query, Result>
    {
        public async Task<Result> Handle(Query q, CancellationToken ct)
        {
            var file = await sender.Send(new GetAttachmentFile.Query(q.Id), ct);
            try
            {
                var obj = await storage.OpenReadAsync(file.StorageKey, ct);
                return new Result(obj.Content, obj.Length, file.FileName, file.ContentType);
            }
            catch (FileNotFoundException)
            {
                throw new NotFoundException($"Файл вкладення #{q.Id} відсутній у сховищі");
            }
        }
    }
}
