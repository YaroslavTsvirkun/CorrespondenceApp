using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Correspondents.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Correspondents;

/// <summary>Слайс: створення / оновлення кореспондента.</summary>
public static class SaveCorrespondent
{
    [RequirePermission(Permissions.CorrespondentsManage)]
    public record Command(int? Id, CorrespondentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Data.CorrespondentTypeId).GreaterThan(0).WithMessage("Оберіть тип кореспондента");
        }
    }
}
