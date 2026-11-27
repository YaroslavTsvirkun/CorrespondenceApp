using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Dictionaries;

/// <summary>Слайс: створення / оновлення запису довідника.</summary>
public static class SaveDictionaryItem
{
    [RequirePermission(Permissions.DictionariesWrite)]
    public record Command(string Kind, int? Id, DictionaryItemInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(100);
    }
}
