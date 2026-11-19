using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Reports.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Reports;

/// <summary>Звіт: прострочені доручення (dbo.usp_Report_OverdueTasks).</summary>
public static class OverdueTasks
{
    public record Query : IRequest<IReadOnlyList<OverdueTaskRow>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<OverdueTaskRow>>
    {
        public Task<IReadOnlyList<OverdueTaskRow>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<OverdueTaskRow>("dbo.usp_Report_OverdueTasks", SqlScope.Documents(user), ct);
    }
}
