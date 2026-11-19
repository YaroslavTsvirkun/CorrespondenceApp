using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Reports.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Reports;

/// <summary>Звіт: кількість документів за типами (dbo.usp_Report_DocumentsByType).</summary>
public static class DocumentsByType
{
    public record Query : IRequest<IReadOnlyList<DocumentsByTypeRow>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<DocumentsByTypeRow>>
    {
        public Task<IReadOnlyList<DocumentsByTypeRow>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<DocumentsByTypeRow>("dbo.usp_Report_DocumentsByType", SqlScope.Documents(user), ct);
    }
}
