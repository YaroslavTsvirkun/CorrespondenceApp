using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, Dto>
    {
        public async Task<Dto> Handle(Query q, CancellationToken ct)
        {
            var orgId = SettingsEndpoints.RequireOrg(user);
            var org = await db.Organizations.IgnoreQueryFilters().Where(o => o.Id == orgId)
                .Select(o => new { o.Id, o.Name, o.RegistrationNumberPattern }).FirstAsync(ct);
            var typeCode = await db.DocumentTypes.OrderBy(t => t.Name)
                .Select(t => t.Code).FirstOrDefaultAsync(ct);
            var dirCode = await db.Directions.OrderBy(d => d.Id)
                .Select(d => d.Code).FirstOrDefaultAsync(ct);
            var preview = RegistrationNumberFormatter.Format(
                org.RegistrationNumberPattern, 1, DateTime.Now.Year, typeCode, dirCode);
            return new Dto(org.Id, org.Name, org.RegistrationNumberPattern, preview);
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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var orgId = SettingsEndpoints.RequireOrg(user);
            var org = await db.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Id == orgId, ct);
            org.RegistrationNumberPattern = c.Data.Pattern.Trim();
            await db.SaveChangesAsync(ct);
        }
    }
}
