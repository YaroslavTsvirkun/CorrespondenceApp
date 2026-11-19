using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Reports;

/// <summary>Звіт: виконавська дисципліна працівників (dbo.usp_Report_ExecutorDiscipline).</summary>
public static class ExecutorDiscipline
{
    public record Query : IRequest<IReadOnlyList<ExecutorDisciplineRow>>;
}
