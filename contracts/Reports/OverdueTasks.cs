using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Reports;

/// <summary>Звіт: прострочені доручення (dbo.usp_Report_OverdueTasks).</summary>
public static class OverdueTasks
{
    [RequirePermission(Permissions.ReportsView)]
    public record Query : IRequest<IReadOnlyList<OverdueTaskRow>>;
}
