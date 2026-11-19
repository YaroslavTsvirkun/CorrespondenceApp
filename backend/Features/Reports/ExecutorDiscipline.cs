using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Reports.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Reports;

/// <summary>Звіт: виконавська дисципліна працівників (dbo.usp_Report_ExecutorDiscipline).</summary>
public static class ExecutorDiscipline
{
    public record Query : IRequest<IReadOnlyList<ExecutorDisciplineRow>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<ExecutorDisciplineRow>>
    {
        public Task<IReadOnlyList<ExecutorDisciplineRow>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<ExecutorDisciplineRow>("dbo.usp_Report_ExecutorDiscipline", SqlScope.Documents(user), ct);
    }
}
