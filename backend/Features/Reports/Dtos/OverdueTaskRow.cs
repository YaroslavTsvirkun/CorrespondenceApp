namespace CorrespondenceApi.Features.Reports.Dtos;

/// <summary>Звіт «Прострочені доручення»: доручення, документ, виконавець, строк, днів прострочення.</summary>
public record OverdueTaskRow(int TaskId, string Document, string Executor, DateOnly DueDate, int DaysOverdue);
