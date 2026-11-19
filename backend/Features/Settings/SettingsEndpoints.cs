using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Settings;
using CorrespondenceSystem.Contracts.Settings.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Settings;

/// <summary>
/// Фіча «Налаштування організації»: патерн автогенерації реєстраційного номера.
/// Кожна організація задає власний патерн (доступ: settings.manage).
/// </summary>
public static class SettingsEndpoints
{
    public static void MapSettings(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/settings").WithTags("Settings")
            .RequireAuthorization(Permissions.SettingsManage);

        g.MapGet("/registration", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetRegistrationSetting.Query(), ct)));

        g.MapPut("/registration", async (ISender m, RegistrationSettingInput i, CancellationToken ct) =>
        {
            await m.Send(new UpdateRegistrationSetting.Command(i), ct);
            return Results.NoContent();
        });
    }
}
