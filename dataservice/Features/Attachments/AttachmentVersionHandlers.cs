using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Attachments.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Attachments;

// Версії вмісту вкладень (редагування документів). Файли у сховищі обслуговує backend.

/// <summary>Заміна вмісту вкладення (dbo.usp_Attachment_ReplaceContent) — повертає попередній ключ об'єкта.</summary>
public sealed class ReplaceAttachmentContentHandler(Db db, ICurrentUser user)
    : IRequestHandler<ReplaceAttachmentContent.Command, string>
{
    public async Task<string> Handle(ReplaceAttachmentContent.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        p.Add("FileName", c.FileName);
        p.Add("ContentType", c.ContentType);
        p.Add("SizeBytes", c.SizeBytes);
        p.Add("Sha256", c.Sha256);
        p.Add("StorageKey", c.StorageKey);
        p.Add("EditorEmployeeId", user.EmployeeId);
        return await db.ScalarProcAsync<string>("dbo.usp_Attachment_ReplaceContent", p, ct);
    }
}

/// <summary>Попередні версії вкладення, новіші — першими.</summary>
public sealed class GetAttachmentVersionsHandler(Db db, ICurrentUser user)
    : IRequestHandler<GetAttachmentVersions.Query, IReadOnlyList<AttachmentVersionDto>>
{
    public async Task<IReadOnlyList<AttachmentVersionDto>> Handle(GetAttachmentVersions.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("AttachmentId", q.AttachmentId);

        var sql = $"""
            SELECT CAST(1 AS bit)
            FROM dbo.Attachment a
            JOIN dbo.Document d ON d.Id = a.DocumentId
            WHERE a.Id = @AttachmentId AND {SqlScope.DocumentFilter("d")};

            SELECT v.Id, v.VersionNo, v.FileName, v.SizeBytes, TODATETIMEOFFSET(v.CreatedAt, 0) AS CreatedAt,
                   e.FullName AS CreatedBy
            FROM dbo.AttachmentVersion v
            LEFT JOIN dbo.Employee e ON e.Id = v.CreatedByEmployeeId
            WHERE v.AttachmentId = @AttachmentId
            ORDER BY v.VersionNo DESC;
            """;

        await using var conn = await db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
        if (await grid.ReadSingleOrDefaultAsync<bool?>() is null)
            throw new NotFoundException($"Вкладення #{q.AttachmentId} не знайдено");
        return (await grid.ReadAsync<AttachmentVersionDto>()).AsList();
    }
}

/// <summary>Метадані файлу попередньої версії вкладення.</summary>
public sealed class GetAttachmentVersionFileHandler(Db db, ICurrentUser user)
    : IRequestHandler<GetAttachmentVersionFile.Query, AttachmentFileDto>
{
    public async Task<AttachmentFileDto> Handle(GetAttachmentVersionFile.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("AttachmentId", q.AttachmentId);
        p.Add("VersionId", q.VersionId);
        return await db.QuerySingleOrDefaultAsync<AttachmentFileDto>(
            $"""
            SELECT v.FileName, v.ContentType, v.StorageKey, d.OrganizationId
            FROM dbo.AttachmentVersion v
            JOIN dbo.Attachment a ON a.Id = v.AttachmentId
            JOIN dbo.Document d ON d.Id = a.DocumentId
            WHERE v.Id = @VersionId AND v.AttachmentId = @AttachmentId AND {SqlScope.DocumentFilter("d")}
            """, p, ct) ?? throw new NotFoundException($"Версію вкладення #{q.VersionId} не знайдено");
    }
}
