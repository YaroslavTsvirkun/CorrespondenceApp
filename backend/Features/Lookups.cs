using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceApi.Features.Lookups;

// Слайс: усі довідники для випадних списків інтерфейсу (один запит — кілька наборів).
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

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, Response>
    {
        public async Task<Response> Handle(Query q, CancellationToken ct)
        {
            await using var c = await db.OpenAsync(ct);
            using var grid = await c.QueryMultipleAsync(new CommandDefinition($"""
                SELECT Id, Code, Name FROM dbo.DocumentDirection ORDER BY Id;
                SELECT Id, Name FROM dbo.DocumentType ORDER BY Name;
                SELECT Id, Name, IsFinal FROM dbo.DocumentStatus ORDER BY Id;
                SELECT Id, Name FROM dbo.DeliveryChannel ORDER BY Name;
                SELECT Id, Name FROM dbo.CorrespondentType ORDER BY Name;
                SELECT Id, Name, IsFinal FROM dbo.TaskStatus ORDER BY Id;
                SELECT d.Id, d.Name FROM dbo.Department d WHERE {SqlScope.TenantFilter("d")} ORDER BY d.Name;
                SELECT e.Id, e.FullName AS Name FROM dbo.Employee e WHERE {SqlScope.TenantFilter("e")} ORDER BY e.FullName;
                SELECT x.Id, x.Name FROM dbo.Correspondent x WHERE {SqlScope.TenantFilter("x")} ORDER BY x.Name;
                """, SqlScope.Tenant(user), cancellationToken: ct));

            return new Response(
                (await grid.ReadAsync<DirectionOption>()).AsList(),
                (await grid.ReadAsync<Option>()).AsList(),
                (await grid.ReadAsync<StatusOption>()).AsList(),
                (await grid.ReadAsync<Option>()).AsList(),
                (await grid.ReadAsync<Option>()).AsList(),
                (await grid.ReadAsync<StatusOption>()).AsList(),
                (await grid.ReadAsync<Option>()).AsList(),
                (await grid.ReadAsync<Option>()).AsList(),
                (await grid.ReadAsync<Option>()).AsList());
        }
    }
}
