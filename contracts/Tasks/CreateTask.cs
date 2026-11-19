using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Tasks;

/// <summary>Слайс: створення доручення (dbo.usp_Task_Create — резолюція + доручення в одній транзакції).</summary>
public static class CreateTask
{
    public record Command(TaskInput Input) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new TaskInputValidator());
    }
}
