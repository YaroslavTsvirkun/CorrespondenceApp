namespace CorrespondenceApi.Features.Tasks.Dtos;

/// <summary>Одна резолюція з кількома дорученнями різним виконавцям.</summary>
public record TaskBatchInput(int DocumentId, string ResolutionText, DateOnly? ResolutionDueDate,
    List<TaskAssignmentInput> Tasks);
