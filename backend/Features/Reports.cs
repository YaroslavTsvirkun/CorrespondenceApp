using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Reports;

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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public async Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct)
        {
            var rows = await DocumentScope.ApplyRead(db.Documents, user)
                .GroupBy(d => d.DocumentType.Name)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            return rows.OrderByDescending(r => r.Count)
                .Select(r => new Row(r.Type, r.Count)).ToList();
        }
    }
}

// 2) Розподіл за напрямом кореспонденції (з часткою у відсотках)
public static class DocumentsByDirection
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(string Direction, int Count, int Percent);

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public async Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct)
        {
            var scoped = DocumentScope.ApplyRead(db.Documents, user);
            var total = await scoped.CountAsync(ct);
            var grouped = await scoped
                .GroupBy(d => d.Direction.Name)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct);
            return grouped
                .Select(x => new Row(x.Key, x.Count, total == 0 ? 0 : x.Count * 100 / total))
                .OrderByDescending(r => r.Count)
                .ToList();
        }
    }
}

// 3) Виконавська дисципліна працівників
public static class ExecutorDiscipline
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(string Executor, int Total, int Done, int Percent);

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public async Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct)
        {
            var scopedIds = DocumentScope.ApplyRead(db.Documents, user).Select(d => d.Id);
            var data = await db.Tasks
                .Where(t => scopedIds.Contains(t.DocumentId))
                .Select(t => new { Name = t.ExecutorEmployee.FullName, Done = t.TaskStatus.Name == "Виконано" })
                .ToListAsync(ct);
            return data
                .GroupBy(x => x.Name)
                .Select(g =>
                {
                    var total = g.Count();
                    var done = g.Count(x => x.Done);
                    return new Row(g.Key, total, done, total == 0 ? 0 : done * 100 / total);
                })
                .OrderByDescending(r => r.Total)
                .ToList();
        }
    }
}

// 4) Прострочені доручення
public static class OverdueTasks
{
    public record Query : IRequest<IReadOnlyList<Row>>;
    public record Row(int TaskId, string Document, string Executor, DateOnly DueDate, int DaysOverdue);

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Row>>
    {
        public async Task<IReadOnlyList<Row>> Handle(Query q, CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var scopedIds = DocumentScope.ApplyRead(db.Documents, user).Select(d => d.Id);
            var data = await db.Tasks
                .Where(t => scopedIds.Contains(t.DocumentId) && !t.TaskStatus.IsFinal && t.DueDate < today)
                .Select(t => new
                {
                    t.Id,
                    Doc = t.Document.Registration != null
                        ? t.Document.Registration.RegistrationNumber + "/" + t.Document.Registration.RegistrationYear
                        : "—",
                    Executor = t.ExecutorEmployee.FullName,
                    t.DueDate,
                })
                .ToListAsync(ct);
            return data
                .Select(x => new Row(x.Id, x.Doc, x.Executor, x.DueDate, today.DayNumber - x.DueDate.DayNumber))
                .OrderByDescending(r => r.DaysOverdue)
                .ToList();
        }
    }
}
