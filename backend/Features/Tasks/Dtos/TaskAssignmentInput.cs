namespace CorrespondenceApi.Features.Tasks.Dtos;

/// <summary>Одне доручення в пакеті (рядок табличного типу dbo.TaskAssignmentList).</summary>
public record TaskAssignmentInput(int ExecutorEmployeeId, int StatusId, string TaskText, DateOnly DueDate);
