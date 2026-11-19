using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Reports.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Reports;

/// <summary>Звіт: розподіл за напрямом кореспонденції з часткою (dbo.usp_Report_DocumentsByDirection).</summary>
public static class DocumentsByDirection
{
    public record Query : IRequest<IReadOnlyList<DocumentsByDirectionRow>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<DocumentsByDirectionRow>>
    {
        public Task<IReadOnlyList<DocumentsByDirectionRow>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<DocumentsByDirectionRow>("dbo.usp_Report_DocumentsByDirection", SqlScope.Documents(user), ct);
    }
}
