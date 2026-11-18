using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Reports;

// Звіти будуються збереженими процедурами dbo.usp_Report_* з урахуванням
// тенант-ізоляції та зони видимості документів поточного користувача.
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

// 1) Кількість документів за типами
public static class DocumentsByType
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(string Type, int Count);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<Row>("dbo.usp_Report_DocumentsByType", SqlScope.Documents(user), ct);
    }
}

// 2) Розподіл за напрямом кореспонденції (з часткою у відсотках)
public static class DocumentsByDirection
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(string Direction, int Count, int Percent);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<Row>("dbo.usp_Report_DocumentsByDirection", SqlScope.Documents(user), ct);
    }
}

// 3) Виконавська дисципліна працівників
public static class ExecutorDiscipline
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(string Executor, int Total, int Done, int Percent);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<Row>("dbo.usp_Report_ExecutorDiscipline", SqlScope.Documents(user), ct);
    }
}

// 4) Прострочені доручення
public static class OverdueTasks
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(int TaskId, string Document, string Executor, DateOnly DueDate, int DaysOverdue);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct) =>
            db.QueryProcAsync<Row>("dbo.usp_Report_OverdueTasks", SqlScope.Documents(user), ct);
    }
}
