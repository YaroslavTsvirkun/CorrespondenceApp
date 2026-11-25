using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Reports;

/// <summary>Звіт: розподіл за напрямом кореспонденції з часткою (dbo.usp_Report_DocumentsByDirection).</summary>
public static class DocumentsByDirection
{
    [RequirePermission(Permissions.ReportsView)]
    public record Query : IRequest<IReadOnlyList<DocumentsByDirectionRow>>;
}
