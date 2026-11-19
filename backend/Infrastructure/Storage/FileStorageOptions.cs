using System.Data.Common;

namespace CorrespondenceApi.Infrastructure.Storage;

/// <summary>
/// Налаштування файлового сховища (секція <c>FileStorage</c> + рядок підключення <c>minio</c>).
/// Під Aspire рядок підключення приходить з AppHost (<c>ConnectionStrings__minio</c>) у форматі
/// <c>Endpoint=http://host:port;AccessKey=…;SecretKey=…</c>.
/// </summary>
public sealed class FileStorageOptions
{
    public const string ConnectionStringName = "minio";

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";

    /// <summary>Бакет для вкладень документів (створюється автоматично).</summary>
    public string Bucket { get; set; } = "correspondence";

    /// <summary>Максимальний розмір одного файлу, МБ.</summary>
    public int MaxFileSizeMb { get; set; } = 50;

    /// <summary>Дозволені розширення файлів (з крапкою, без урахування регістру).</summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf", ".doc", ".docx", ".odt", ".rtf", ".txt",
        ".xls", ".xlsx", ".ods", ".csv", ".ppt", ".pptx", ".odp",
        ".jpg", ".jpeg", ".png", ".tif", ".tiff",
        ".zip", ".7z", ".xml", ".eml", ".msg",
        // Файли з кваліфікованим електронним підписом (КЕП)
        ".p7s", ".asice", ".asics",
    ];

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;

    /// <summary>Застосовує значення з рядка підключення (Endpoint, AccessKey, SecretKey).</summary>
    public void ApplyConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var b = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (b.TryGetValue("Endpoint", out var endpoint)) Endpoint = endpoint.ToString()!;
        if (b.TryGetValue("AccessKey", out var accessKey)) AccessKey = accessKey.ToString()!;
        if (b.TryGetValue("SecretKey", out var secretKey)) SecretKey = secretKey.ToString()!;
    }
}
