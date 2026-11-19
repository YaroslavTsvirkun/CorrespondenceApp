using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports;
using MediatR;

namespace CorrespondenceApi.Features.Reports;

/// <summary>
/// Фіча «Звіти»: будуються збереженими процедурами dbo.usp_Report_* з урахуванням
/// тенант-ізоляції та зони видимості документів поточного користувача.
/// </summary>
public static class ReportsEndpoints
{
    public static void MapReports(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reports").WithTags("Reports")
            .RequireAuthorization(Permissions.ReportsView);
        g.MapGet("/by-type", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new DocumentsByType.Query(), ct)));
        g.MapGet("/by-direction", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new DocumentsByDirection.Query(), ct)));
        g.MapGet("/executor-discipline", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new ExecutorDiscipline.Query(), ct)));
        g.MapGet("/overdue-tasks", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new OverdueTasks.Query(), ct)));
    }
}
