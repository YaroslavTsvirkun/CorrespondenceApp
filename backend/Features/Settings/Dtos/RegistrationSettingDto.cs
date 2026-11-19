namespace CorrespondenceApi.Features.Settings.Dtos;

/// <summary>Патерн реєстраційного номера організації та приклад номера за ним.</summary>
public record RegistrationSettingDto(int OrganizationId, string OrganizationName, string Pattern, string Preview);
