using CorrespondenceApi.Features.Tasks.Dtos;
using FluentValidation;

namespace CorrespondenceApi.Features.Tasks;

/// <summary>Спільна валідація доручення (створення та редагування).</summary>
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
