using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Tasks;

/// <summary>Дані слайсу: редагування доручення та тексту резолюції (dbo.usp_Task_Update).</summary>
public sealed class UpdateTaskHandler(Db db, ICurrentUser user) : IRequestHandler<UpdateTask.Command>
{
    public Task Handle(UpdateTask.Command c, CancellationToken ct)
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
