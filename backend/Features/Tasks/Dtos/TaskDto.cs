namespace CorrespondenceApi.Features.Tasks.Dtos;

/// <summary>Доручення у списку доручень (з документом, резолюцією, виконавцем і статусом).</summary>
public record TaskDto(int Id, int DocumentId, string DocumentNumber, string DocumentTitle,
    int ExecutorEmployeeId, string ExecutorName, int StatusId, string StatusName,
    string TaskText, string ResolutionText, DateOnly DueDate, DateTime? CompletedAt, string? ResultText);
