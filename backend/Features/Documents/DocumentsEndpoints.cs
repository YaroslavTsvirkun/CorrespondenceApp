using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Documents.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>Фіча «Документи»: реєстр, картка, реєстрація, редагування, видалення.</summary>
public static class DocumentsEndpoints
{
    public static void MapDocuments(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/documents").WithTags("Documents");

        g.MapGet("", async (ISender m, [AsParameters] GetDocuments.Query q, CancellationToken ct) =>
            Results.Ok(await m.Send(q, ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDocumentById.Query(id), ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/{id:int}/edit", async (ISender m, int id, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDocumentForEdit.Query(id), ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/next-number", async (ISender m, int directionId, int documentTypeId, int year, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetNextNumber.Query(directionId, documentTypeId, year), ct)))
            .RequireAuthorization(Permissions.DocumentsCreate);

        g.MapPost("", async (ISender m, DocumentInput input, CancellationToken ct) =>
        {
            var id = await m.Send(new CreateDocument.Command(input), ct);
            return Results.Created($"/api/documents/{id}", new { id });
        }).RequireAuthorization(Permissions.DocumentsCreate);

        g.MapPut("/{id:int}", async (ISender m, int id, DocumentInput input, CancellationToken ct) =>
        {
            await m.Send(new UpdateDocument.Command(id, input), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DocumentsUpdate);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteDocument.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DocumentsDelete);
    }
}
