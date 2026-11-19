using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Tasks;

/// <summary>Слайс: редагування доручення та тексту резолюції (dbo.usp_Task_Update).</summary>
public static class UpdateTask
{
    public record Command(int Id, TaskInput Input) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new TaskInputValidator());
    }
}
