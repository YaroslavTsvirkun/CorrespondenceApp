using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Reports;

/// <summary>Звіт: розподіл за напрямом кореспонденції з часткою (dbo.usp_Report_DocumentsByDirection).</summary>
public sealed class DocumentsByDirectionHandler(Db db, ICurrentUser user) : IRequestHandler<DocumentsByDirection.Query, IReadOnlyList<DocumentsByDirectionRow>>
{
    public Task<IReadOnlyList<DocumentsByDirectionRow>> Handle(DocumentsByDirection.Query q, CancellationToken ct) =>
        db.QueryProcAsync<DocumentsByDirectionRow>("dbo.usp_Report_DocumentsByDirection", SqlScope.Documents(user), ct);
}
