using System.Security.Claims;
using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceApi.Features.Attachments;
using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Common;
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

        // Перегляд у браузері: PDF / HTML як є, офісні документи — сконвертовані у PDF.
        // Без Content-Disposition (inline); HTML додатково ізолюється CSP sandbox.
        g.MapGet("/{id:int}/preview", async (ISender m, HttpContext http, int id, CancellationToken ct) =>
            {
                var file = await m.Send(new PreviewAttachment.Query(id), ct);
                http.Response.Headers.XContentTypeOptions = "nosniff";
                http.Response.Headers.ContentSecurityPolicy =
                    "sandbox; default-src 'none'; style-src 'unsafe-inline'; img-src data:";
                http.Response.Headers.CacheControl = "private, no-store";
                http.Response.ContentLength = file.Length;
                return Results.Stream(file.Content, file.ContentType);
            })
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
            {
                await m.Send(new DeleteAttachment.Command(id), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Permissions.DocumentsUpdate);

        // ---- Редагування (ONLYOFFICE Docs): конфігурація редактора для браузера ----
        // Режим редагування — лише з правом documents.update, інакше документ відкривається для читання.
        g.MapGet("/{id:int}/editor", async (DocumentEditorService editor, ClaimsPrincipal user, int id,
                CancellationToken ct) =>
            {
                var canEdit = user.HasPermission(Permissions.DocumentsUpdate);
                return Results.Ok(await editor.GetConfigAsync(id, user.GetUserId(), user.DisplayName(), canEdit, ct));
            })
            .RequireAuthorization(Permissions.DocumentsReadAny);

        // ---- Версії вмісту (після редагування) ----
        g.MapGet("/{id:int}/versions", async (ISender m, int id, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetAttachmentVersions.Query(id), ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/{id:int}/versions/{versionId:int}/content", async (ISender m, HttpContext http, int id,
                int versionId, CancellationToken ct) =>
            {
                var file = await m.Send(new DownloadAttachmentVersion.Query(id, versionId), ct);
                http.Response.Headers.XContentTypeOptions = "nosniff";
                http.Response.ContentLength = file.Length;
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapPost("/{id:int}/versions/{versionId:int}/restore", async (ISender m, ClaimsPrincipal user, int id,
                int versionId, CancellationToken ct) =>
            {
                await m.Send(new RestoreAttachmentVersion.Command(id, versionId, user.GetUserId()), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Permissions.DocumentsUpdate);

        // ---- Виклики від Document Server (без JWT користувача; доступ — за підписаним посиланням) ----
        var editorApi = app.MapGroup("/api/editor/attachments").WithTags("Document editor").AllowAnonymous();

        editorApi.MapGet("/{id:int}/file", async (DocumentEditorService editor, HttpContext http, int id,
                [FromQuery(Name = "t")] string? token, CancellationToken ct) =>
            {
                var file = await editor.OpenFileAsync(id, token, ct);
                http.Response.ContentLength = file.Length;
                return Results.Stream(file.Content, file.ContentType);
            });

        // Відповідь завжди { error: 0 | 1 } — так вимагає протокол ONLYOFFICE
        editorApi.MapPost("/{id:int}/callback", async (DocumentEditorService editor, HttpRequest request, int id,
                [FromQuery(Name = "t")] string? token, CancellationToken ct) =>
                Results.Json(new { error = await editor.HandleCallbackAsync(id, token, request, ct) }))
            .DisableAntiforgery();
    }
}
