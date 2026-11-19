using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Tasks;

/// <summary>Дані слайсу: доручення за документами, видимими користувачу (vw_Task + fn_VisibleDocuments).</summary>
public sealed class GetTasksHandler(Db db, ICurrentUser user) : IRequestHandler<GetTasks.Query, IReadOnlyList<TaskDto>>
{
    public Task<IReadOnlyList<TaskDto>> Handle(GetTasks.Query q, CancellationToken ct) =>
        db.QueryAsync<TaskDto>($"""
            SELECT t.Id, t.DocumentId, t.DocumentNumber, t.DocumentTitle,
                   t.ExecutorEmployeeId, t.ExecutorName,
                   t.TaskStatusId AS StatusId, t.StatusName,
                   t.TaskText, t.ResolutionText, t.DueDate, t.CompletedAt, t.ResultText
            FROM dbo.vw_Task t
            JOIN {SqlScope.VisibleDocuments} v ON v.Id = t.DocumentId
            ORDER BY t.DueDate DESC, t.Id DESC
            """, SqlScope.Documents(user), ct);
}
