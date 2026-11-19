namespace CorrespondenceApi.Infrastructure.Storage;

/// <summary>Об'єкт, прочитаний зі сховища (потік треба закрити після використання).</summary>
public sealed record StoredObject(Stream Content, long Length);

/// <summary>
/// Сховище вмісту файлів. Метадані (ім'я, тип, розмір, документ) живуть у БД (dbo.Attachment),
/// тут — лише байти за ключем.
/// </summary>
public interface IFileStorage
{
    Task PutAsync(string key, Stream content, long length, string contentType, CancellationToken ct);

    /// <exception cref="FileNotFoundException">Об'єкта з таким ключем немає.</exception>
    Task<StoredObject> OpenReadAsync(string key, CancellationToken ct);

    /// <summary>Видаляє об'єкт; відсутність об'єкта не є помилкою.</summary>
    Task DeleteAsync(string key, CancellationToken ct);

    /// <summary>Перевірка доступності сховища (для health check).</summary>
    Task PingAsync(CancellationToken ct);
}
