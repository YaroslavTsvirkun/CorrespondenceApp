namespace CorrespondenceSystem.Contracts.Attachments.Dtos;

/// <summary>
/// Метадані файлу вкладення: ім'я, MIME-тип, ключ об'єкта у сховищі
/// та організація документа (префікс ключа для нового вмісту).
/// </summary>
public sealed record AttachmentFileDto(string FileName, string ContentType, string StorageKey, int OrganizationId);
