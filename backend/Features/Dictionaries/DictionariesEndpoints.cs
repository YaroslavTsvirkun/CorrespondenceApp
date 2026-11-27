using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Dictionaries;
using CorrespondenceSystem.Contracts.Dictionaries.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Dictionaries;

/// <summary>
/// Фіча «Довідники»: узагальнений CRUD для простих довідників {id, name, description}.
/// kind = document-types | channels (право dictionaries.manage) | legal-forms | party-categories (parties.manage).
/// </summary>
public static class DictionariesEndpoints
{
    public static void MapDictionaries(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/dictionaries").WithTags("Dictionaries");

        g.MapGet("/{kind}", async (ISender m, string kind, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDictionary.Query(kind), ct))).RequireAuthorization();

        g.MapPost("/{kind}", async (ISender m, string kind, DictionaryItemInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveDictionaryItem.Command(kind, null, i), ct);
            return Results.Created($"/api/dictionaries/{kind}/{id}", new { id });
        }).RequireAuthorization(Permissions.DictionariesWrite);

        g.MapPut("/{kind}/{id:int}", async (ISender m, string kind, int id, DictionaryItemInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveDictionaryItem.Command(kind, id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DictionariesWrite);

        g.MapDelete("/{kind}/{id:int}", async (ISender m, string kind, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteDictionaryItem.Command(kind, id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DictionariesWrite);
    }
}
