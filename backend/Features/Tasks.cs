using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<Dto>>
    {
        public async Task<IReadOnlyList<Dto>> Handle(Query q, CancellationToken ct)
        {
            var scopedIds = DocumentScope.ApplyRead(db.Documents, user).Select(d => d.Id);
            return await db.Tasks.AsNoTracking()
                .Where(t => scopedIds.Contains(t.DocumentId))
                .OrderByDescending(t => t.DueDate)
                .Select(t => new Dto(
                    t.Id, t.DocumentId,
                    t.Document.Registration != null
                        ? t.Document.Registration.RegistrationNumber + "/" + t.Document.Registration.RegistrationYear
                        : "—",
                    t.Document.Title,
                    t.ExecutorEmployeeId, t.ExecutorEmployee.FullName,
                    t.TaskStatusId, t.TaskStatus.Name,
                    t.TaskText, t.Resolution.ResolutionText, t.DueDate, t.CompletedAt, t.ResultText))
                .ToListAsync(ct);
        }
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

public static class CreateTask
{
    public record Command(TaskInput Input) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new TaskInputValidator());
    }

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var i = c.Input;
            // Доручення завжди належить резолюції — створюємо її автоматично.
            var resolution = new Resolution
            {
                DocumentId = i.DocumentId,
                AuthorEmployeeId = user.EmployeeId ?? i.ExecutorEmployeeId,
                ResolutionText = i.ResolutionText,
                ResolutionDate = DateOnly.FromDateTime(DateTime.Today),
                DueDate = i.DueDate,
                IsControlled = true,
            };
            var task = new ExecutionTask
            {
                Resolution = resolution,
                DocumentId = i.DocumentId,
                ExecutorEmployeeId = i.ExecutorEmployeeId,
                TaskStatusId = i.StatusId,
                TaskText = i.TaskText,
                AssignedAt = DateTime.UtcNow,
                DueDate = i.DueDate,
                CompletedAt = i.CompletedAt?.ToDateTime(TimeOnly.MinValue),
                ResultText = i.ResultText,
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync(ct);
            return task.Id;
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

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var task = await db.Tasks.Include(t => t.Resolution)
                .FirstOrDefaultAsync(t => t.Id == c.Id, ct)
                ?? throw new NotFoundException($"Доручення #{c.Id} не знайдено");
            var i = c.Input;
            task.ExecutorEmployeeId = i.ExecutorEmployeeId;
            task.TaskStatusId = i.StatusId;
            task.TaskText = i.TaskText;
            task.DueDate = i.DueDate;
            task.CompletedAt = i.CompletedAt?.ToDateTime(TimeOnly.MinValue);
            task.ResultText = i.ResultText;
            task.Resolution.ResolutionText = i.ResolutionText;
            await db.SaveChangesAsync(ct);
        }
    }
}

public static class DeleteTask
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var task = await db.Tasks.FindAsync([c.Id], ct)
                ?? throw new NotFoundException($"Доручення #{c.Id} не знайдено");
            db.Tasks.Remove(task);
            await db.SaveChangesAsync(ct);
        }
    }
}
