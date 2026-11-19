using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Reports;

/// <summary>Звіт: кількість документів за типами (dbo.usp_Report_DocumentsByType).</summary>
public sealed class DocumentsByTypeHandler(Db db, ICurrentUser user) : IRequestHandler<DocumentsByType.Query, IReadOnlyList<DocumentsByTypeRow>>
{
    public Task<IReadOnlyList<DocumentsByTypeRow>> Handle(DocumentsByType.Query q, CancellationToken ct) =>
        db.QueryProcAsync<DocumentsByTypeRow>("dbo.usp_Report_DocumentsByType", SqlScope.Documents(user), ct);
}
