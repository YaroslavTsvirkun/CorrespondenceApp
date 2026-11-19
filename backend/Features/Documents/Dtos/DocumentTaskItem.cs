namespace CorrespondenceApi.Features.Documents.Dtos;

/// <summary>Доручення в картці документа.</summary>
public record DocumentTaskItem(int Id, string Executor, string TaskText, DateOnly DueDate,
    string Status, DateTime? CompletedAt);
