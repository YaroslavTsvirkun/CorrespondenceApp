using System.Security.Cryptography;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>
/// Заміна вмісту вкладення (збереження з редактора, відновлення версії): новий файл кладеться
/// у сховище під новим ключем, у БД поточний вміст стає попередньою версією.
/// Операція з БД виконується від імені вказаного користувача (callback редактора не має JWT користувача).
/// </summary>
public sealed class AttachmentContentService(
    DataApiClient dataApi, IFileStorage storage, FileStorageOptions storageOptions,
    ILogger<AttachmentContentService> logger)
{
    public static string NewStorageKey(int organizationId)
    {
        var now = DateTime.UtcNow;
        return $"{organizationId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
    }

    /// <param name="content">Новий вміст; буде повністю прочитано (у пам'ять, для SHA-256 і довжини).</param>
    public async Task ReplaceAsync(int attachmentId, int organizationId, string fileName, Stream content,
        int? userId, CancellationToken ct)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0) throw new InvalidOperationException("Новий вміст вкладення порожній");
        if (buffer.Length > storageOptions.MaxFileSizeBytes)
            throw new InvalidOperationException($"Розмір файлу перевищує {storageOptions.MaxFileSizeMb} МБ");

        buffer.Position = 0;
        var sha256 = await SHA256.HashDataAsync(buffer, ct);
        buffer.Position = 0;

        var contentType = AttachmentFiles.ContentTypeFor(fileName);
        var key = NewStorageKey(organizationId);
        await storage.PutAsync(key, buffer, buffer.Length, contentType, ct);

        string oldKey;
        try
        {
            oldKey = await dataApi.SendAsync(new ReplaceAttachmentContent.Command(
                attachmentId, fileName, contentType, buffer.Length, sha256, key), userId, ct);
        }
        catch
        {
            // Компенсація: не залишаємо у сховищі файл без запису в БД
            try { await storage.DeleteAsync(key, CancellationToken.None); }
            catch (Exception ex) { logger.LogWarning(ex, "Не вдалося видалити осиротілий об'єкт {Key}", key); }
            throw;
        }

        // Старий файл лишається як версія; його PDF-копія для перегляду більше не потрібна
        try { await storage.DeleteAsync(PreviewAttachment.PreviewKey(oldKey), CancellationToken.None); }
        catch (Exception ex) { logger.LogWarning(ex, "Не вдалося видалити PDF-копію {Key}", oldKey); }
    }
}
