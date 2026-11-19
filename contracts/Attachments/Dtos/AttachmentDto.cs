namespace CorrespondenceSystem.Contracts.Attachments.Dtos;

/// <summary>Вкладення документа (метадані файлу, остання зміна вмісту, кількість попередніх версій).</summary>
public record AttachmentDto(int Id, string FileName, string ContentType, long SizeBytes,
    DateTimeOffset AddedAt, string? AddedBy, string? Description,
    DateTimeOffset? ModifiedAt, string? ModifiedBy, int VersionCount);
