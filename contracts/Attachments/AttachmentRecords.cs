using CorrespondenceSystem.Contracts.Attachments.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Attachments;

// Операції з записами вкладень у БД. Самі файли зберігає backend у MinIO / S3:
// він викликає ці операції до/після роботи зі сховищем.

/// <summary>
/// Перевірка перед завантаженням: документ існує й доступний користувачу.
/// Повертає Id організації документа (префікс ключа об'єкта у сховищі).
/// </summary>
public static class PrepareAttachmentUpload
{
    public record Query(int DocumentId) : IRequest<int>;
}

/// <summary>Реєстрація завантаженого у сховище файлу (dbo.usp_Attachment_Create). Повертає Id вкладення.</summary>
public static class RegisterAttachment
{
    public record Command(
        int DocumentId, string FileName, string ContentType, long SizeBytes,
        byte[] Sha256, string StorageKey, string? Description) : IRequest<int>;
}

/// <summary>Метадані файлу вкладення для завантаження (з перевіркою видимості документа).</summary>
public static class GetAttachmentFile
{
    public record Query(int Id) : IRequest<AttachmentFileDto>;
}

/// <summary>Видалення запису вкладення (dbo.usp_Attachment_Delete). Повертає ключі об'єктів для видалення зі сховища.</summary>
public static class DeleteAttachmentRecord
{
    public record Command(int Id) : IRequest<IReadOnlyList<string>>;
}
