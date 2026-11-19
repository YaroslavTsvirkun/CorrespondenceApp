using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Reports;

/// <summary>Звіт: прострочені доручення (dbo.usp_Report_OverdueTasks).</summary>
public sealed class OverdueTasksHandler(Db db, ICurrentUser user) : IRequestHandler<OverdueTasks.Query, IReadOnlyList<OverdueTaskRow>>
{
    public Task<IReadOnlyList<OverdueTaskRow>> Handle(OverdueTasks.Query q, CancellationToken ct) =>
        db.QueryProcAsync<OverdueTaskRow>("dbo.usp_Report_OverdueTasks", SqlScope.Documents(user), ct);
}
