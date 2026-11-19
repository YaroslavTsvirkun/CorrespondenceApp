using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Settings.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Settings;

/// <summary>Слайс: зміна патерну реєстраційного номера своєї організації.</summary>
public static class UpdateRegistrationSetting
{
    public record Command(RegistrationSettingInput Data) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Pattern).NotEmpty().MaximumLength(100);
    }
}
