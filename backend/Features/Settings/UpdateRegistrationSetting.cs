using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Settings.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Settings;

/// <summary>Слайс: зміна патерну реєстраційного номера своєї організації.</summary>
public static class UpdateRegistrationSetting
{
    public record Command(RegistrationSettingInput Data) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Pattern).NotEmpty().MaximumLength(100);
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var orgId = SettingsEndpoints.RequireOrg(user);
            await db.ExecuteAsync(
                "UPDATE dbo.Organization SET RegistrationNumberPattern = @pattern WHERE Id = @orgId",
                new { pattern = c.Data.Pattern.Trim(), orgId }, ct);
        }
    }
}
