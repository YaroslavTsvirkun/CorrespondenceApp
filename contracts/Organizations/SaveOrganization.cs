using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Organizations.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Organizations;

/// <summary>Слайс: створення / оновлення організації.</summary>
public static class SaveOrganization
{
    public record Command(int? Id, OrganizationInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
    }
}
