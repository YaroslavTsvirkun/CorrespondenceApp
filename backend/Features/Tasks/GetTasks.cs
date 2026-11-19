using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Tasks.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Tasks;

/// <summary>Слайс: доручення за документами, видимими користувачу (vw_Task + fn_VisibleDocuments).</summary>
public static class GetTasks
{
    public record Query : IRequest<IReadOnlyList<TaskDto>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<TaskDto>>
    {
        public Task<IReadOnlyList<TaskDto>> Handle(Query q, CancellationToken ct) =>
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
}
