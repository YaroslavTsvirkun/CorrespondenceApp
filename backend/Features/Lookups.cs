using CorrespondenceApi.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Lookups;

// Слайс: усі довідники для випадних списків інтерфейсу.
public static class LookupsEndpoints
{
    public static void MapLookups(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lookups", async (ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetLookups.Query(), ct)))
            .WithTags("Lookups").WithName("GetLookups")
            .RequireAuthorization();
    }
}

public static class GetLookups
{
    public record Query : IRequest<Response>;

    public record Option(int Id, string Name);
    public record DirectionOption(int Id, string Code, string Name);
    public record StatusOption(int Id, string Name, bool IsFinal);

    public record Response(
        IReadOnlyList<DirectionOption> Directions,
        IReadOnlyList<Option> DocumentTypes,
        IReadOnlyList<StatusOption> DocumentStatuses,
        IReadOnlyList<Option> Channels,
        IReadOnlyList<Option> CorrespondentTypes,
        IReadOnlyList<StatusOption> TaskStatuses,
        IReadOnlyList<Option> Departments,
        IReadOnlyList<Option> Employees,
        IReadOnlyList<Option> Correspondents);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Response>
    {
        public async Task<Response> Handle(Query q, CancellationToken ct) => new(
            await db.Directions.OrderBy(x => x.Id)
                .Select(x => new DirectionOption(x.Id, x.Code, x.Name)).ToListAsync(ct),
            await db.DocumentTypes.OrderBy(x => x.Name)
                .Select(x => new Option(x.Id, x.Name)).ToListAsync(ct),
            await db.DocumentStatuses.OrderBy(x => x.Id)
                .Select(x => new StatusOption(x.Id, x.Name, x.IsFinal)).ToListAsync(ct),
            await db.Channels.OrderBy(x => x.Name)
                .Select(x => new Option(x.Id, x.Name)).ToListAsync(ct),
            await db.CorrespondentTypes.OrderBy(x => x.Name)
                .Select(x => new Option(x.Id, x.Name)).ToListAsync(ct),
            await db.TaskStatuses.OrderBy(x => x.Id)
                .Select(x => new StatusOption(x.Id, x.Name, x.IsFinal)).ToListAsync(ct),
            await db.Departments.OrderBy(x => x.Name)
                .Select(x => new Option(x.Id, x.Name)).ToListAsync(ct),
            await db.Employees.OrderBy(x => x.FullName)
                .Select(x => new Option(x.Id, x.FullName)).ToListAsync(ct),
            await db.Correspondents.OrderBy(x => x.Name)
                .Select(x => new Option(x.Id, x.Name)).ToListAsync(ct));
    }
}
