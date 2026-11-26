using Microsoft.AspNetCore.StaticFiles;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>Допоміжні функції для імен і MIME-типів файлів вкладень.</summary>
public static class AttachmentFiles
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = CreateProvider();

    /// <summary>
    /// Новий ключ об'єкта у сховищі: {організація}/{рік}/{місяць}/{guid}. Ключ не залежить від імені файлу,
    /// тож ім'я від клієнта ніколи не потрапляє в шлях у сховищі; новий вміст — завжди новий ключ.
    /// </summary>
    public static string NewStorageKey(int organizationId)
    {
        var now = DateTime.UtcNow;
        return $"{organizationId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Ім'я файлу без шляху та керувальних символів (браузери/ОС можуть передати повний шлях).
    /// Використовується лише для відображення й Content-Disposition, не як ключ у сховищі.
    /// </summary>
    public static string SafeFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? "").Replace('\\', '/'));
        return new string(name.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
    }

    /// <summary>MIME-тип за розширенням (значенню від клієнта не довіряємо).</summary>
    public static string ContentTypeFor(string fileName) =>
        ContentTypes.TryGetContentType(fileName, out var type) ? type : "application/octet-stream";

    private static FileExtensionContentTypeProvider CreateProvider()
    {
        var p = new FileExtensionContentTypeProvider();
        p.Mappings[".p7s"] = "application/pkcs7-signature";
        p.Mappings[".asice"] = "application/vnd.etsi.asic-e+zip";
        p.Mappings[".asics"] = "application/vnd.etsi.asic-s+zip";
        p.Mappings[".msg"] = "application/vnd.ms-outlook";
        p.Mappings[".7z"] = "application/x-7z-compressed";
        p.Mappings[".odt"] = "application/vnd.oasis.opendocument.text";
        p.Mappings[".ods"] = "application/vnd.oasis.opendocument.spreadsheet";
        p.Mappings[".odp"] = "application/vnd.oasis.opendocument.presentation";
        return p;
    }
}
