using System.Net.Http.Headers;

namespace CorrespondenceApi.Infrastructure.Conversion;

/// <summary>Налаштування конвертера документів (секція <c>DocumentConversion</c>).</summary>
public sealed class DocumentConversionOptions
{
    /// <summary>Адреса Gotenberg; під Aspire — через service discovery (<c>http://gotenberg</c>).</summary>
    public string BaseUrl { get; set; } = "http://gotenberg";

    /// <summary>Максимальний час конвертації одного файлу, с.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>Конвертація офісних документів у PDF для перегляду в браузері.</summary>
public interface IDocumentConverter
{
    /// <summary>Повертає PDF (потік у пам'яті, позиція 0).</summary>
    Task<MemoryStream> ConvertToPdfAsync(Stream source, string fileName, CancellationToken ct);
}

/// <summary>
/// Конвертер на базі Gotenberg (LibreOffice у контейнері): <c>POST /forms/libreoffice/convert</c>.
/// Формат визначається за розширенням імені файлу.
/// </summary>
public sealed class GotenbergDocumentConverter(HttpClient http) : IDocumentConverter
{
    public async Task<MemoryStream> ConvertToPdfAsync(Stream source, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(source);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        // Gotenberg визначає формат за розширенням; ім'я — латиницею, щоб уникнути проблем з кодуванням
        form.Add(file, "files", "document" + Path.GetExtension(fileName).ToLowerInvariant());

        using var response = await http.PostAsync("/forms/libreoffice/convert", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new DocumentConversionException(
                $"Конвертер повернув {(int)response.StatusCode}: {body}");
        }

        var pdf = new MemoryStream();
        await response.Content.CopyToAsync(pdf, ct);
        pdf.Position = 0;
        return pdf;
    }
}

/// <summary>Не вдалося перетворити документ для перегляду -> 422.</summary>
public sealed class DocumentConversionException(string message, Exception? inner = null) : Exception(message, inner);
