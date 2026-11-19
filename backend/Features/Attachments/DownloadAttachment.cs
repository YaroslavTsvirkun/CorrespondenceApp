using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using CorrespondenceApi.Infrastructure.Storage;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Слайс: вміст вкладення (потік зі сховища + метадані для Content-Disposition).</summary>
public static class DownloadAttachment
{
    public record Query(int Id) : IRequest<Result>;

    public sealed record Result(Stream Content, long Length, string FileName, string ContentType);

    private sealed record Row(string FileName, string ContentType, string StorageKey);

    public class Handler(Db db, ICurrentUser user, IFileStorage storage) : IRequestHandler<Query, Result>
    {
        public async Task<Result> Handle(Query q, CancellationToken ct)
        {
            var p = SqlScope.Documents(user);
            p.Add("Id", q.Id);
            var row = await db.QuerySingleOrDefaultAsync<Row>(
                $"""
                SELECT a.FileName, a.ContentType, a.StorageKey
                FROM dbo.Attachment a
                JOIN dbo.Document d ON d.Id = a.DocumentId
                WHERE a.Id = @Id AND {SqlScope.DocumentFilter("d")}
                """, p, ct) ?? throw new NotFoundException($"Вкладення #{q.Id} не знайдено");

            try
            {
                var obj = await storage.OpenReadAsync(row.StorageKey, ct);
                return new Result(obj.Content, obj.Length, row.FileName, row.ContentType);
            }
            catch (FileNotFoundException)
            {
                throw new NotFoundException($"Файл вкладення #{q.Id} відсутній у сховищі");
            }
        }
    }
}
