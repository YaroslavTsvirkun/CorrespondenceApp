using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Settings;
using CorrespondenceSystem.Contracts.Settings.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Settings;

/// <summary>
/// Дані слайсу: патерн номера організації з попереднім переглядом
/// (формує скалярна функція БД dbo.fn_FormatRegistrationNumber).
/// </summary>
public sealed class GetRegistrationSettingHandler(Db db, ICurrentUser user) : IRequestHandler<GetRegistrationSetting.Query, RegistrationSettingDto?>
{
    public async Task<RegistrationSettingDto?> Handle(GetRegistrationSetting.Query q, CancellationToken ct)
    {
        // Супер-адмін без організації: налаштування немає (патерн задається в картці організації)
        if (user.OrganizationId is not int orgId) return null;
        return await db.QuerySingleOrDefaultAsync<RegistrationSettingDto>("""
            SELECT o.Id AS OrganizationId, o.Name AS OrganizationName,
                   o.RegistrationNumberPattern AS Pattern,
                   dbo.fn_FormatRegistrationNumber(o.RegistrationNumberPattern, 1, YEAR(GETDATE()),
                       (SELECT TOP (1) Code FROM dbo.DocumentType ORDER BY Name),
                       (SELECT TOP (1) Code FROM dbo.DocumentDirection ORDER BY Id)) AS Preview
            FROM dbo.Organization o
            WHERE o.Id = @orgId
            """, new { orgId }, ct)
            ?? throw new NotFoundException($"Організацію #{orgId} не знайдено");
    }
}
