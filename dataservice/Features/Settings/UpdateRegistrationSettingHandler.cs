using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Settings;
using CorrespondenceSystem.Contracts.Settings.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Settings;

/// <summary>Дані слайсу: зміна патерну реєстраційного номера своєї організації.</summary>
public sealed class UpdateRegistrationSettingHandler(Db db, ICurrentUser user) : IRequestHandler<UpdateRegistrationSetting.Command>
{
    public async Task Handle(UpdateRegistrationSetting.Command c, CancellationToken ct)
    {
        var orgId = OrganizationScope.RequireOrg(user);
        await db.ExecuteAsync(
            "UPDATE dbo.Organization SET RegistrationNumberPattern = @pattern WHERE Id = @orgId",
            new { pattern = c.Data.Pattern.Trim(), orgId }, ct);
    }
}
