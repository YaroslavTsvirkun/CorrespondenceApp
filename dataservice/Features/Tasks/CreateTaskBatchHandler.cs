using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Tasks;

/// <summary>
/// Дані слайсу: пакетне створення доручень — процедура dbo.usp_Task_CreateBatch
/// з табличним параметром dbo.TaskAssignmentList.
/// </summary>
public sealed class CreateTaskBatchHandler(Db db, ICurrentUser user) : IRequestHandler<CreateTaskBatch.Command, TaskBatchResult>
{
    public async Task<TaskBatchResult> Handle(CreateTaskBatch.Command c, CancellationToken ct)
    {
        var i = c.Input;
        var p = SqlScope.Tenant(user);
        p.Add("DocumentId", i.DocumentId);
        p.Add("ResolutionText", i.ResolutionText);
        p.Add("ResolutionDueDate", i.ResolutionDueDate);
        p.Add("AuthorEmployeeId", user.EmployeeId);
        p.Add("Tasks", SqlTvp.TaskAssignments(
            i.Tasks.Select(t => (t.ExecutorEmployeeId, t.StatusId, t.TaskText, t.DueDate))));
        return new TaskBatchResult(await db.QueryProcAsync<int>("dbo.usp_Task_CreateBatch", p, ct));
    }
}
