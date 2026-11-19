using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Attachments.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Attachments;

/// <summary>Дані слайсу: вкладення документа (з урахуванням тенанта й зони видимості документа).</summary>
public sealed class GetAttachmentsHandler(Db db, ICurrentUser user)
    : IRequestHandler<GetAttachments.Query, IReadOnlyList<AttachmentDto>>
{
    public async Task<IReadOnlyList<AttachmentDto>> Handle(GetAttachments.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("DocumentId", q.DocumentId);

        var sql = $"""
            SELECT CAST(1 AS bit) FROM dbo.Document d
            WHERE d.Id = @DocumentId AND {SqlScope.DocumentFilter("d")};

            SELECT a.Id, a.FileName, a.ContentType, a.SizeBytes, TODATETIMEOFFSET(a.AddedAt, 0) AS AddedAt,
                   e.FullName AS AddedBy, a.Description,
                   TODATETIMEOFFSET(a.ModifiedAt, 0) AS ModifiedAt, m.FullName AS ModifiedBy,
                   (SELECT COUNT(*) FROM dbo.AttachmentVersion v WHERE v.AttachmentId = a.Id) AS VersionCount
            FROM dbo.Attachment a
            LEFT JOIN dbo.Employee e ON e.Id = a.AddedByEmployeeId
            LEFT JOIN dbo.Employee m ON m.Id = a.ModifiedByEmployeeId
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
