using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Settings;

// Налаштування організації: патерн автогенерації реєстраційного номера.
// Кожна організація задає власний патерн (доступ: settings.manage).
public static class SettingsEndpoints
{
    public static void MapSettings(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/settings").WithTags("Settings")
            .RequireAuthorization(Permissions.SettingsManage);

        g.MapGet("/registration", async (ISender m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetRegistrationSetting.Query(), ct)));

        g.MapPut("/registration", async (ISender m, UpdateRegistrationSetting.Input i, CancellationToken ct) =>
        {
            await m.Send(new UpdateRegistrationSetting.Command(i), ct);
            return Results.NoContent();
        });
    }

    internal static int RequireOrg(ICurrentUser user) =>
        user.OrganizationId ?? throw new ValidationException([new ValidationFailure("organization",
            "Супер-адміністратор не прив'язаний до організації — задайте патерн у картці організації")]);
}

public static class GetRegistrationSetting
{
    public record Query : IRequest<Dto>;
    public record Dto(int OrganizationId, string OrganizationName, string Pattern, string Preview);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, Dto>
    {
        // Попередній перегляд формує скалярна функція БД dbo.fn_FormatRegistrationNumber
        public async Task<Dto> Handle(Query q, CancellationToken ct)
        {
            var orgId = SettingsEndpoints.RequireOrg(user);
            return await db.QuerySingleOrDefaultAsync<Dto>("""
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

public static class UpdateRegistrationSetting
{
    public record Input(string Pattern);
    public record Command(Input Data) : IRequest;

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
