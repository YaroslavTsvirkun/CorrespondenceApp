using System.Buffers;
using System.Security.Cryptography;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using FluentValidation;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

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
    private const int BufferSize = 81920;

    public static string NewStorageKey(int organizationId)
    {
        var now = DateTime.UtcNow;
        return $"{organizationId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
    }

    /// <param name="content">
    /// Новий вміст. Копіюється в тимчасовий файл (а не в пам'ять) з одночасним підрахунком SHA-256
    /// і довжини; копіювання переривається, щойно розмір перевищить ліміт.
    /// </param>
    /// <exception cref="ValidationException">Файл порожній або завеликий (-> 400).</exception>
    public async Task ReplaceAsync(int attachmentId, int organizationId, string fileName, Stream content,
        int? userId, CancellationToken ct)
    {
        await using var buffer = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite,
            FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        var (length, sha256) = await CopyWithLimitAsync(content, buffer, storageOptions.MaxFileSizeBytes, ct);
        if (length == 0)
            throw Invalid("Новий вміст вкладення порожній");
        buffer.Position = 0;

        var contentType = AttachmentFiles.ContentTypeFor(fileName);
        var key = NewStorageKey(organizationId);
        await storage.PutAsync(key, buffer, length, contentType, ct);

        string oldKey;
        try
        {
            oldKey = await dataApi.SendAsync(new ReplaceAttachmentContent.Command(
                attachmentId, fileName, contentType, length, sha256, key), userId, ct);
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

    private async Task<(long Length, byte[] Sha256)> CopyWithLimitAsync(Stream source, Stream target, long maxBytes,
        CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var chunk = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(chunk.AsMemory(0, BufferSize), ct)) > 0)
            {
                total += read;
                if (total > maxBytes)
                    throw Invalid($"Розмір файлу перевищує {storageOptions.MaxFileSizeMb} МБ");
                hash.AppendData(chunk, 0, read);
                await target.WriteAsync(chunk.AsMemory(0, read), ct);
            }
            await target.FlushAsync(ct);
            return (total, hash.GetHashAndReset());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
    }

    private static ValidationException Invalid(string message) =>
        new([new ValidationFailure("File", message)]);
}
