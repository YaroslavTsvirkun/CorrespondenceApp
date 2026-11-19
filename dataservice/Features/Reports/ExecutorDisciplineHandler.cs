using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Reports;

/// <summary>Звіт: виконавська дисципліна працівників (dbo.usp_Report_ExecutorDiscipline).</summary>
public sealed class ExecutorDisciplineHandler(Db db, ICurrentUser user) : IRequestHandler<ExecutorDiscipline.Query, IReadOnlyList<ExecutorDisciplineRow>>
{
    public Task<IReadOnlyList<ExecutorDisciplineRow>> Handle(ExecutorDiscipline.Query q, CancellationToken ct) =>
        db.QueryProcAsync<ExecutorDisciplineRow>("dbo.usp_Report_ExecutorDiscipline", SqlScope.Documents(user), ct);
}
