using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Settings.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Settings;

/// <summary>
/// Слайс: патерн номера організації з попереднім переглядом
/// (формує скалярна функція БД dbo.fn_FormatRegistrationNumber).
/// null — користувач (супер-адмін) не прив'язаний до організації: патерн задається в картці організації.
/// </summary>
public static class GetRegistrationSetting
{
    [RequirePermission(Permissions.SettingsManage)]
    public record Query : IRequest<RegistrationSettingDto?>;
}
