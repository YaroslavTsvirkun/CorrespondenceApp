using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Tasks;

public record TaskInput(int DocumentId, string ResolutionText, int ExecutorEmployeeId,
    int StatusId, string TaskText, DateOnly DueDate, DateOnly? CompletedAt, string? ResultText);

public static class TasksEndpoints
{
    public static void MapTasks(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/tasks").WithTags("Tasks");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetTasks.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, TaskInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new CreateTask.Command(i), ct);
            return Results.Created($"/api/tasks/{id}", new { id });
        }).RequireAuthorization(Permissions.TasksAssign);

        g.MapPut("/{id:int}", async (ISender m, int id, TaskInput i, CancellationToken ct) =>
        {
            await m.Send(new UpdateTask.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TasksWrite);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteTask.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TasksAssign);
    }
}

public static class GetTasks
{
    public record Query : IRequest<IReadOnlyList<Dto>>;
    public record Dto(int Id, int DocumentId, string DocumentNumber, string DocumentTitle,
        int ExecutorEmployeeId, string ExecutorName, int StatusId, string StatusName,
        string TaskText, string ResolutionText, DateOnly DueDate, DateTime? CompletedAt, string? ResultText);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<Dto>($"""
                SELECT t.Id, t.DocumentId,
                       dbo.fn_DocumentDisplayNumber(t.DocumentId) AS DocumentNumber,
                       d.Title AS DocumentTitle,
                       t.ExecutorEmployeeId, e.FullName AS ExecutorName,
                       t.TaskStatusId AS StatusId, s.Name AS StatusName,
                       t.TaskText, r.ResolutionText, t.DueDate, t.CompletedAt, t.ResultText
                FROM dbo.ExecutionTask t
                JOIN dbo.Document d   ON d.Id = t.DocumentId
                JOIN dbo.Employee e   ON e.Id = t.ExecutorEmployeeId
                JOIN dbo.TaskStatus s ON s.Id = t.TaskStatusId
                JOIN dbo.Resolution r ON r.Id = t.ResolutionId
                WHERE {SqlScope.DocumentFilter("d")}
                ORDER BY t.DueDate DESC, t.Id DESC
                """, SqlScope.Documents(user), ct);
    }
}

public class TaskInputValidator : AbstractValidator<TaskInput>
{
    public TaskInputValidator()
    {
        RuleFor(x => x.DocumentId).GreaterThan(0).WithMessage("Оберіть документ");
        RuleFor(x => x.ExecutorEmployeeId).GreaterThan(0).WithMessage("Оберіть виконавця");
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("Оберіть статус");
        RuleFor(x => x.TaskText).NotEmpty().WithMessage("Вкажіть завдання");
        RuleFor(x => x.ResolutionText).NotEmpty().WithMessage("Вкажіть текст резолюції");
    }
}

// Створення доручення: процедура dbo.usp_Task_Create (резолюція + доручення в одній транзакції)
public static class CreateTask
{
    public record Command(TaskInput Input) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new TaskInputValidator());
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public Task<int> Handle(Command c, CancellationToken ct)
        {
            var i = c.Input;
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(new
            {
                i.DocumentId,
                i.ResolutionText,
                AuthorEmployeeId = user.EmployeeId,
                i.ExecutorEmployeeId,
                TaskStatusId = i.StatusId,
                i.TaskText,
                i.DueDate,
                CompletedAt = i.CompletedAt?.ToDateTime(TimeOnly.MinValue),
                i.ResultText,
            });
            return db.ScalarProcAsync<int>("dbo.usp_Task_Create", p, ct);
        }
    }
}

public static class UpdateTask
{
    public record Command(int Id, TaskInput Input) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new TaskInputValidator());
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct)
        {
            var i = c.Input;
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(new
            {
                c.Id,
                i.ResolutionText,
                i.ExecutorEmployeeId,
                TaskStatusId = i.StatusId,
                i.TaskText,
                i.DueDate,
                CompletedAt = i.CompletedAt?.ToDateTime(TimeOnly.MinValue),
                i.ResultText,
            });
            return db.ExecuteProcAsync("dbo.usp_Task_Update", p, ct);
        }
    }
}

public static class DeleteTask
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            return db.ExecuteProcAsync("dbo.usp_Task_Delete", p, ct);
        }
    }
}
