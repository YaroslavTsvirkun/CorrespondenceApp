using CorrespondenceSystem.Contracts.Lookups;
using MediatR;

namespace CorrespondenceApi.Features.Lookups;

/// <summary>Фіча «Довідники для інтерфейсу»: усі випадні списки одним запитом.</summary>
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
