using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Storage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Фіча «Вкладення»: файли документів у сховищі MinIO / S3.</summary>
public static class AttachmentsEndpoints
{
    public static void MapAttachments(IEndpointRouteBuilder app)
    {
        var maxRequestBytes = app.ServiceProvider.GetRequiredService<FileStorageOptions>().MaxFileSizeBytes
                              + 1024 * 1024; // запас на multipart-заголовки та опис

        var byDocument = app.MapGroup("/api/documents/{documentId:int}/attachments").WithTags("Attachments");

        byDocument.MapGet("", async (ISender m, int documentId, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetAttachments.Query(documentId), ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        // multipart/form-data: file + description. Автентифікація — JWT у заголовку (не cookie),
        // тож antiforgery-токен не потрібен.
        byDocument.MapPost("", async (ISender m, int documentId, IFormFile file,
                [FromForm] string? description, CancellationToken ct) =>
            {
                var id = await m.Send(new UploadAttachment.Command(documentId, file, description), ct);
                return Results.Created($"/api/attachments/{id}", new { id });
            })
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(maxRequestBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = maxRequestBytes })
            .RequireAuthorization(Permissions.DocumentsUpdate);

        var g = app.MapGroup("/api/attachments").WithTags("Attachments");

        g.MapGet("/{id:int}/content", async (ISender m, HttpContext http, int id, CancellationToken ct) =>
            {
                var file = await m.Send(new DownloadAttachment.Query(id), ct);
                // Завжди як завантаження (attachment), без вгадування типу браузером
                http.Response.Headers.XContentTypeOptions = "nosniff";
                http.Response.ContentLength = file.Length;
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
            {
                await m.Send(new DeleteAttachment.Command(id), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Permissions.DocumentsUpdate);
    }
}
