namespace CorrespondenceApi.Features.Reports.Dtos;

/// <summary>Звіт «Виконавська дисципліна»: доручень усього, виконано, відсоток.</summary>
public record ExecutorDisciplineRow(string Executor, int Total, int Done, int Percent);
