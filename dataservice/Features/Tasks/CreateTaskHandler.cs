using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Tasks;

/// <summary>Дані слайсу: створення доручення (dbo.usp_Task_Create — резолюція + доручення в одній транзакції).</summary>
public sealed class CreateTaskHandler(Db db, ICurrentUser user) : IRequestHandler<CreateTask.Command, int>
{
    public Task<int> Handle(CreateTask.Command c, CancellationToken ct)
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
