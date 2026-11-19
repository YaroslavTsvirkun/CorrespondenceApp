using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Tasks.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Tasks;

/// <summary>Слайс: редагування доручення та тексту резолюції (dbo.usp_Task_Update).</summary>
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
