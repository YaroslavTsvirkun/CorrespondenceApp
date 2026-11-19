using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Tasks;

/// <summary>
/// Слайс: пакетне створення доручень — процедура dbo.usp_Task_CreateBatch
/// з табличним параметром dbo.TaskAssignmentList.
/// </summary>
public static class CreateTaskBatch
{
    public record Command(TaskBatchInput Input) : IRequest<TaskBatchResult>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Input.DocumentId).GreaterThan(0).WithMessage("Оберіть документ");
            RuleFor(x => x.Input.ResolutionText).NotEmpty().WithMessage("Вкажіть текст резолюції").MaximumLength(1000);
            RuleFor(x => x.Input.Tasks).NotEmpty().WithMessage("Додайте хоча б одне доручення");
            RuleForEach(x => x.Input.Tasks).ChildRules(t =>
            {
                t.RuleFor(i => i.ExecutorEmployeeId).GreaterThan(0).WithMessage("Оберіть виконавця");
                t.RuleFor(i => i.StatusId).GreaterThan(0).WithMessage("Оберіть статус");
                t.RuleFor(i => i.TaskText).NotEmpty().WithMessage("Вкажіть завдання").MaximumLength(1000);
                t.RuleFor(i => i.DueDate).NotEqual(default(DateOnly)).WithMessage("Вкажіть строк");
            });
        }
    }
}
