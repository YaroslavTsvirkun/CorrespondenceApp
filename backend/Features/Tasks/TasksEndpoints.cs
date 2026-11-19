using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Tasks;

/// <summary>Фіча «Доручення»: список, створення (поодиноке та пакетне), редагування, видалення.</summary>
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

        // Одна резолюція — кілька виконавців (табличний параметр dbo.TaskAssignmentList)
        g.MapPost("/batch", async (ISender m, TaskBatchInput i, CancellationToken ct) =>
            Results.Ok(await m.Send(new CreateTaskBatch.Command(i), ct)))
            .RequireAuthorization(Permissions.TasksAssign);

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
