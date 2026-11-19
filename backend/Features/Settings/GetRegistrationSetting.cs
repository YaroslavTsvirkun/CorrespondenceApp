using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Settings.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Settings;

/// <summary>
/// Слайс: патерн номера організації з попереднім переглядом
/// (формує скалярна функція БД dbo.fn_FormatRegistrationNumber).
/// </summary>
public static class GetRegistrationSetting
{
    public record Query : IRequest<RegistrationSettingDto>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, RegistrationSettingDto>
    {
        public async Task<RegistrationSettingDto> Handle(Query q, CancellationToken ct)
        {
            var orgId = SettingsEndpoints.RequireOrg(user);
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
}
