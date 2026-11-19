namespace CorrespondenceSystem.Contracts.Attachments.Dtos;

/// <summary>Попередня версія вмісту вкладення (до редагування чи відновлення).</summary>
public sealed record AttachmentVersionDto(int Id, int VersionNo, string FileName, long SizeBytes,
    DateTimeOffset CreatedAt, string? CreatedBy);
