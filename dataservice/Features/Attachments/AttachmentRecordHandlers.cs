using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Attachments.Dtos;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Attachments;

// Записи вкладень у БД. Файли у сховищі MinIO / S3 обслуговує backend.

/// <summary>Документ має існувати й бути доступним — перевіряємо до завантаження байтів у сховище.</summary>
public sealed class PrepareAttachmentUploadHandler(Db db, ICurrentUser user)
    : IRequestHandler<PrepareAttachmentUpload.Query, int>
{
    public async Task<int> Handle(PrepareAttachmentUpload.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("DocumentId", q.DocumentId);
        return await db.QuerySingleOrDefaultAsync<int?>(
            $"SELECT d.OrganizationId FROM dbo.Document d WHERE d.Id = @DocumentId AND {SqlScope.DocumentFilter("d")}",
            p, ct) ?? throw new NotFoundException($"Документ #{q.DocumentId} не знайдено");
    }
}

/// <summary>Реєстрація файлу, уже покладеного у сховище (dbo.usp_Attachment_Create).</summary>
public sealed class RegisterAttachmentHandler(Db db, ICurrentUser user)
    : IRequestHandler<RegisterAttachment.Command, int>
{
    public async Task<int> Handle(RegisterAttachment.Command c, CancellationToken ct)
    {
        await AttachmentAccess.EnsureDocumentVisibleAsync(db, user, c.DocumentId, ct);
        var p = SqlScope.Tenant(user);
        p.Add("DocumentId", c.DocumentId);
        p.Add("FileName", c.FileName);
        p.Add("ContentType", c.ContentType);
        p.Add("SizeBytes", c.SizeBytes);
        p.Add("Sha256", c.Sha256);
        p.Add("StorageKey", c.StorageKey);
        p.Add("Description", c.Description);
        p.Add("AddedByEmployeeId", user.EmployeeId);
        return await db.ScalarProcAsync<int>("dbo.usp_Attachment_Create", p, ct);
    }
}

/// <summary>Метадані файлу вкладення (з перевіркою тенанта й видимості документа).</summary>
public sealed class GetAttachmentFileHandler(Db db, ICurrentUser user)
    : IRequestHandler<GetAttachmentFile.Query, AttachmentFileDto>
{
    public async Task<AttachmentFileDto> Handle(GetAttachmentFile.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("Id", q.Id);
        return await db.QuerySingleOrDefaultAsync<AttachmentFileDto>(
            $"""
            SELECT a.FileName, a.ContentType, a.StorageKey, d.OrganizationId
            FROM dbo.Attachment a
            JOIN dbo.Document d ON d.Id = a.DocumentId
            WHERE a.Id = @Id AND {SqlScope.DocumentFilter("d")}
            """, p, ct) ?? throw new NotFoundException($"Вкладення #{q.Id} не знайдено");
    }
}

/// <summary>Видалення запису вкладення (dbo.usp_Attachment_Delete) — повертає ключі об'єктів сховища.</summary>
public sealed class DeleteAttachmentRecordHandler(Db db, ICurrentUser user)
    : IRequestHandler<DeleteAttachmentRecord.Command, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(DeleteAttachmentRecord.Command c, CancellationToken ct)
    {
        await AttachmentAccess.EnsureAttachmentVisibleAsync(db, user, c.Id, ct);
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        return await db.QueryProcAsync<string>("dbo.usp_Attachment_Delete", p, ct);
    }
}

/// <summary>
/// Зміна вкладень — лише для документів у зоні видимості користувача (тенант + dbo.fn_CanReadDocument),
/// а не просто в межах організації: інакше право documents.update дозволяло б змінювати вкладення
/// документів, яких користувач не бачить.
/// </summary>
internal static class AttachmentAccess
{
    public static async Task EnsureDocumentVisibleAsync(Db db, ICurrentUser user, int documentId, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("DocumentId", documentId);
        if (await db.QuerySingleOrDefaultAsync<bool?>(
                $"SELECT CAST(1 AS bit) FROM dbo.Document d WHERE d.Id = @DocumentId AND {SqlScope.DocumentFilter("d")}",
                p, ct) is null)
            throw new NotFoundException($"Документ #{documentId} не знайдено");
    }

    public static async Task EnsureAttachmentVisibleAsync(Db db, ICurrentUser user, int attachmentId, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("AttachmentId", attachmentId);
        if (await db.QuerySingleOrDefaultAsync<bool?>(
                $"""
                SELECT CAST(1 AS bit)
                FROM dbo.Attachment a
                JOIN dbo.Document d ON d.Id = a.DocumentId
                WHERE a.Id = @AttachmentId AND {SqlScope.DocumentFilter("d")}
                """, p, ct) is null)
            throw new NotFoundException($"Вкладення #{attachmentId} не знайдено");
    }
}
