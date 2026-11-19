using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Tasks.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Tasks;

/// <summary>Слайс: створення доручення (dbo.usp_Task_Create — резолюція + доручення в одній транзакції).</summary>
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
