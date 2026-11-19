using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Attachments.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Слайс: вкладення документа (з урахуванням тенанта й зони видимості документа).</summary>
public static class GetAttachments
{
    public record Query(int DocumentId) : IRequest<IReadOnlyList<AttachmentDto>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<AttachmentDto>>
    {
        public async Task<IReadOnlyList<AttachmentDto>> Handle(Query q, CancellationToken ct)
        {
            var p = SqlScope.Documents(user);
            p.Add("DocumentId", q.DocumentId);

            var sql = $"""
                SELECT CAST(1 AS bit) FROM dbo.Document d
                WHERE d.Id = @DocumentId AND {SqlScope.DocumentFilter("d")};

                SELECT a.Id, a.FileName, a.ContentType, a.SizeBytes, TODATETIMEOFFSET(a.AddedAt, 0) AS AddedAt,
                       e.FullName AS AddedBy, a.Description
                FROM dbo.Attachment a
                LEFT JOIN dbo.Employee e ON e.Id = a.AddedByEmployeeId
                WHERE a.DocumentId = @DocumentId
                ORDER BY a.AddedAt, a.Id;
                """;

            await using var conn = await db.OpenAsync(ct);
            using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
            if (await grid.ReadSingleOrDefaultAsync<bool?>() is null)
                throw new NotFoundException($"Документ #{q.DocumentId} не знайдено");
            return (await grid.ReadAsync<AttachmentDto>()).AsList();
        }
    }
}
