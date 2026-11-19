using CorrespondenceApi.Infrastructure.Conversion;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>
/// Слайс: вміст вкладення для перегляду в браузері.
/// PDF і HTML віддаються як є; офісні документи (DOC/DOCX/XLS/XLSX…) конвертуються у PDF
/// (Gotenberg/LibreOffice), результат кешується у сховищі поруч з оригіналом.
/// </summary>
public static class PreviewAttachment
{
    public record Query(int Id) : IRequest<Result>;

    public sealed record Result(Stream Content, long Length, string ContentType);

    /// <summary>Розширення, які конвертуються у PDF.</summary>
    private static readonly HashSet<string> Convertible = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".odt", ".rtf", ".xls", ".xlsx", ".ods", ".ppt", ".pptx", ".odp",
    };

    private static readonly HashSet<string> Html = new(StringComparer.OrdinalIgnoreCase) { ".html", ".htm" };

    /// <summary>Ключ PDF-копії для перегляду (видаляється разом з оригіналом).</summary>
    public static string PreviewKey(string storageKey) => storageKey + ".preview.pdf";

    public class Handler(ISender sender, IFileStorage storage, IDocumentConverter converter, ILogger<Handler> logger)
        : IRequestHandler<Query, Result>
    {
        public async Task<Result> Handle(Query q, CancellationToken ct)
        {
            var file = await sender.Send(new GetAttachmentFile.Query(q.Id), ct);
            var ext = Path.GetExtension(file.FileName);

            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return await Open(file.StorageKey, "application/pdf", q.Id, ct);

            if (Html.Contains(ext))
                return await Open(file.StorageKey, "text/html", q.Id, ct);

            if (!Convertible.Contains(ext))
                throw new ValidationException([new ValidationFailure("File", $"Перегляд файлів «{ext}» не підтримується")]);

            var previewKey = PreviewKey(file.StorageKey);
            try
            {
                var cached = await storage.OpenReadAsync(previewKey, ct);
                return new Result(cached.Content, cached.Length, "application/pdf");
            }
            catch (FileNotFoundException)
            {
                // PDF-копії ще немає — конвертуємо
            }

            MemoryStream pdf;
            var original = await Open(file.StorageKey, file.ContentType, q.Id, ct);
            await using (original.Content)
            {
                try
                {
                    pdf = await converter.ConvertToPdfAsync(original.Content, file.FileName, ct);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
                {
                    throw new DocumentConversionException("Сервіс перегляду документів недоступний", ex);
                }
            }

            try
            {
                await storage.PutAsync(previewKey, pdf, pdf.Length, "application/pdf", ct);
            }
            catch (Exception ex)
            {
                // Кеш — не обов'язковий: показуємо результат навіть якщо не вдалося зберегти
                logger.LogWarning(ex, "Не вдалося зберегти PDF-копію {Key}", previewKey);
            }

            pdf.Position = 0;
            return new Result(pdf, pdf.Length, "application/pdf");
        }

        private async Task<Result> Open(string key, string contentType, int id, CancellationToken ct)
        {
            try
            {
                var obj = await storage.OpenReadAsync(key, ct);
                return new Result(obj.Content, obj.Length, contentType);
            }
            catch (FileNotFoundException)
            {
                throw new NotFoundException($"Файл вкладення #{id} відсутній у сховищі");
            }
        }
    }
}
