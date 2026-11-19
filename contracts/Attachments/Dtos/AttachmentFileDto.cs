namespace CorrespondenceSystem.Contracts.Attachments.Dtos;

/// <summary>Метадані файлу вкладення: ім'я, MIME-тип і ключ об'єкта у сховищі.</summary>
public sealed record AttachmentFileDto(string FileName, string ContentType, string StorageKey);
