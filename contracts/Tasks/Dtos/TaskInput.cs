namespace CorrespondenceSystem.Contracts.Tasks.Dtos;

/// <summary>Поля доручення для створення / редагування (разом із текстом резолюції).</summary>
public record TaskInput(int DocumentId, string ResolutionText, int ExecutorEmployeeId,
    int StatusId, string TaskText, DateOnly DueDate, DateOnly? CompletedAt, string? ResultText);
