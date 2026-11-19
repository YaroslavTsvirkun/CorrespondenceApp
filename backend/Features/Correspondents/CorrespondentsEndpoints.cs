using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Correspondents.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Correspondents;

/// <summary>Фіча «Кореспонденти» (у межах організації користувача).</summary>
public static class CorrespondentsEndpoints
{
    public static void MapCorrespondents(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/correspondents").WithTags("Correspondents");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetCorrespondents.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, CorrespondentInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveCorrespondent.Command(null, i), ct);
            return Results.Created($"/api/correspondents/{id}", new { id });
        }).RequireAuthorization(Permissions.CorrespondentsManage);

        g.MapPut("/{id:int}", async (ISender m, int id, CorrespondentInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveCorrespondent.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.CorrespondentsManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteCorrespondent.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.CorrespondentsManage);
    }
}
