namespace CorrespondenceApi.Features.Attachments.Dtos;

/// <summary>Вкладення документа (метадані файлу).</summary>
public record AttachmentDto(int Id, string FileName, string ContentType, long SizeBytes,
    DateTimeOffset AddedAt, string? AddedBy, string? Description);
