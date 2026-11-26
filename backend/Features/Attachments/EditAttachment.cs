using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceApi.Infrastructure.Editor;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Common;
using FluentValidation;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>
/// Редагування документів Word у браузері через ONLYOFFICE Docs.
/// <list type="number">
/// <item>Браузер отримує підписану конфігурацію редактора (<see cref="GetConfigAsync"/>).</item>
/// <item>Document Server завантажує файл за одноразовим підписаним посиланням (<see cref="OpenFileAsync"/>).</item>
/// <item>Після закриття редактора Document Server надсилає callback із посиланням на збережений файл
///   (<see cref="HandleCallbackAsync"/>) — вміст вкладення замінюється, попередній стає версією.</item>
/// </list>
/// .doc редактор конвертує в DOCX, тож збережений файл отримує розширення .docx.
/// </summary>
public sealed class DocumentEditorService(
    DocumentEditorOptions options, OnlyOfficeJwt onlyOfficeJwt, EditorLinkTokens links,
    DataApiClient dataApi, IFileStorage storage, AttachmentContentService content,
    IHttpClientFactory httpFactory, ILogger<DocumentEditorService> logger)
{
    /// <summary>Розширення, які можна редагувати.</summary>
    public static readonly HashSet<string> Editable = new(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx" };

    public sealed record EditorConfig(string DocumentServerUrl, object Config);

    // ---- 1. Конфігурація редактора для браузера ----

    public async Task<EditorConfig> GetConfigAsync(int attachmentId, int userId, string userName, bool canEdit,
        CancellationToken ct)
    {
        if (!options.IsConfigured)
            throw Errors.Validation("Editor", "Редактор документів не налаштовано");

        // Перевірка доступу до документа — від імені поточного користувача
        var file = await dataApi.SendAsync(new GetAttachmentFile.Query(attachmentId), userId, ct);
        var ext = Path.GetExtension(file.FileName);
        if (!Editable.Contains(ext))
            throw Errors.Validation("File", $"Редагування файлів «{ext}» не підтримується");

        var key = DocumentKey(attachmentId, file.StorageKey);
        var baseUrl = options.CallbackBaseUrl.TrimEnd('/');
        var fileUrl = $"{baseUrl}/api/editor/attachments/{attachmentId}/file?t={links.Create(attachmentId, userId, key, TimeSpan.FromHours(24))}";

        var editorConfig = new Dictionary<string, object?>
        {
            ["mode"] = canEdit ? "edit" : "view",
            ["lang"] = "uk",
            ["region"] = "uk-UA",
            ["user"] = new { id = userId.ToString(), name = userName },
            // plugins = false: вбудовані плагіни (AI-асистент, маркетплейс…) не вантажаться —
            // це сотні зайвих запитів і помітно довше відкриття редактора
            ["customization"] = new { forcesave = false, compactHeader = true, uiTheme = "theme-light", plugins = false },
        };
        if (canEdit)
            // Callback може прийти через години після відкриття (коли всі закриють документ)
            editorConfig["callbackUrl"] =
                $"{baseUrl}/api/editor/attachments/{attachmentId}/callback?t={links.Create(attachmentId, userId, key, TimeSpan.FromDays(30))}";

        var payload = new Dictionary<string, object?>
        {
            ["document"] = new
            {
                fileType = ext.TrimStart('.').ToLowerInvariant(),
                key,
                title = file.FileName,
                url = fileUrl,
                permissions = new { edit = canEdit, review = canEdit, comment = canEdit, download = true, print = true },
            },
            ["documentType"] = "word",
            ["editorConfig"] = editorConfig,
        };
        payload["token"] = onlyOfficeJwt.Sign(payload);

        return new EditorConfig(options.PublicUrl.TrimEnd('/'), payload);
    }

    // ---- 2. Файл для Document Server ----

    /// <summary>Вміст файлу для Document Server (потік треба закрити після використання).</summary>
    public sealed record EditorFile(Stream Content, long Length, string ContentType);

    public async Task<EditorFile> OpenFileAsync(int attachmentId, string? token, CancellationToken ct)
    {
        var link = await links.ValidateAsync(token, attachmentId)
                   ?? throw new UnauthorizedException("Недійсне посилання на файл");
        var file = await dataApi.SendAsync(new GetAttachmentFile.Query(attachmentId), link.UserId, ct);
        if (DocumentKey(attachmentId, file.StorageKey) != link.Key)
            throw new NotFoundException("Вміст вкладення змінився — відкрийте документ знову");
        try
        {
            var obj = await storage.OpenReadAsync(file.StorageKey, ct);
            return new EditorFile(obj.Content, obj.Length, file.ContentType);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException($"Файл вкладення #{attachmentId} відсутній у сховищі");
        }
    }

    // ---- 3. Callback збереження ----

    /// <summary>Дані callback, потрібні для збереження (після перевірки підпису й ключа).</summary>
    private sealed record SaveRequest(Uri DownloadUrl, string? FileType, int? LastEditorId);

    /// <summary>Обробляє callback Document Server; повертає код для відповіді { error }.</summary>
    public async Task<int> HandleCallbackAsync(int attachmentId, string? token, HttpRequest request,
        CancellationToken ct)
    {
        var link = await links.ValidateAsync(token, attachmentId);
        if (link is null)
        {
            logger.LogWarning("Callback редактора для вкладення #{Id}: недійсне посилання", attachmentId);
            return 1;
        }

        JsonElement body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body, cancellationToken: ct);
        }
        catch (JsonException)
        {
            logger.LogWarning("Callback редактора для вкладення #{Id}: тіло не є JSON", attachmentId);
            return 1;
        }

        var data = body.ValueKind == JsonValueKind.Object
            ? await VerifiedCallbackAsync(body, request.Headers.Authorization.ToString())
            : null;
        if (data is not { ValueKind: JsonValueKind.Object } d)
        {
            logger.LogWarning("Callback редактора для вкладення #{Id}: недійсний підпис Document Server", attachmentId);
            return 1;
        }

        var status = d.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.Number
                     && s.TryGetInt32(out var st) ? st : 0;
        switch (status)
        {
            case 2 or 6: // документ готовий до збереження (2 — усі закрили, 6 — примусове збереження)
                break;
            case 3 or 7:
                logger.LogError("Document Server не зміг зберегти вкладення #{Id} (статус {Status})", attachmentId, status);
                return 0;
            default: // 1 — редагується, 4 — закрито без змін
                return 0;
        }

        if (StringProperty(d, "key") != link.Key)
        {
            logger.LogWarning("Callback редактора для вкладення #{Id}: ключ документа не збігається", attachmentId);
            return 1;
        }

        if (ParseSaveRequest(d) is not { } save)
        {
            logger.LogWarning("Callback редактора для вкладення #{Id}: некоректне або недозволене посилання на файл", attachmentId);
            return 1;
        }

        // Хто зберіг: останній користувач, що змінював документ (за замовчуванням — той, хто відкрив)
        var userId = save.LastEditorId ?? link.UserId;

        try
        {
            // Права (documents.update) і видимість документа сервіс даних перевіряє на момент збереження
            var file = await dataApi.SendAsync(new GetAttachmentFile.Query(attachmentId), userId, ct);
            if (DocumentKey(attachmentId, file.StorageKey) != link.Key)
                logger.LogWarning("Вкладення #{Id} змінилося під час редагування — зберігаємо поверх (попередній вміст стане версією)", attachmentId);

            var fileName = string.IsNullOrEmpty(save.FileType)
                ? file.FileName
                : Path.ChangeExtension(file.FileName, "." + save.FileType.ToLowerInvariant());

            using var http = httpFactory.CreateClient("onlyoffice-download");
            await using var stream = await http.GetStreamAsync(save.DownloadUrl, ct);
            await content.ReplaceAsync(attachmentId, file.OrganizationId, fileName, stream, userId, ct);
        }
        catch (DataApiException e) when ((int)e.StatusCode is >= 400 and < 500)
        {
            logger.LogWarning("Збереження вкладення #{Id} з редактора відхилено для користувача {User}: {Status} {Body}",
                attachmentId, userId, (int)e.StatusCode, e.Body);
            return 1;
        }
        catch (ValidationException e)
        {
            logger.LogWarning("Збереження вкладення #{Id} з редактора відхилено: {Errors}", attachmentId,
                string.Join("; ", e.Errors.Select(x => x.ErrorMessage)));
            return 1;
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Не вдалося завантажити збережений файл вкладення #{Id} з Document Server", attachmentId);
            return 1;
        }

        logger.LogInformation("Вкладення #{Id} збережено з редактора (користувач {User})", attachmentId, userId);
        return 0;
    }

    private static string? StringProperty(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    /// <summary>
    /// Посилання на збережений файл, тип файлу й останній редактор. Файл завантажується лише з Document Server:
    /// якщо задано <see cref="DocumentEditorOptions.InternalUrl"/>, origin посилання замінюється на нього,
    /// інакше origin має збігатися з <see cref="DocumentEditorOptions.PublicUrl"/> (захист від SSRF).
    /// </summary>
    private SaveRequest? ParseSaveRequest(JsonElement d)
    {
        if (!Uri.TryCreate(StringProperty(d, "url"), UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
            return null;

        Uri downloadUrl;
        if (!string.IsNullOrWhiteSpace(options.InternalUrl))
        {
            var target = new Uri(options.InternalUrl);
            downloadUrl = new UriBuilder(url) { Scheme = target.Scheme, Host = target.Host, Port = target.Port }.Uri;
        }
        else if (Uri.TryCreate(options.PublicUrl, UriKind.Absolute, out var server)
                 && Uri.Compare(url, server, UriComponents.SchemeAndServer, UriFormat.Unescaped,
                     StringComparison.OrdinalIgnoreCase) == 0)
            downloadUrl = url;
        else
            return null;

        // Редактор зберігає .doc як .docx; інших типів не приймаємо
        var fileType = StringProperty(d, "filetype");
        if (fileType is not null && !Editable.Contains("." + fileType)) return null;

        int? lastEditor = d.TryGetProperty("users", out var users) && users.ValueKind == JsonValueKind.Array
                          && users.GetArrayLength() > 0 && users[0].ValueKind == JsonValueKind.String
                          && int.TryParse(users[0].GetString(), out var id)
            ? id
            : null;

        return new SaveRequest(downloadUrl, fileType, lastEditor);
    }

    /// <summary>
    /// Дані callback, перевірені підписом Document Server: токен у тілі (<c>token</c>, payload — саме тіло)
    /// або в заголовку Authorization (payload загорнуто в <c>payload</c>).
    /// </summary>
    private async Task<JsonElement?> VerifiedCallbackAsync(JsonElement body, string authorization)
    {
        if (StringProperty(body, "token") is { Length: > 0 } t)
            return await onlyOfficeJwt.ValidateAsync(t);

        const string bearer = "Bearer ";
        if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase)
            && await onlyOfficeJwt.ValidateAsync(authorization[bearer.Length..]) is { } header)
            return header.TryGetProperty("payload", out var p) ? p : header;

        return null;
    }

    /// <summary>
    /// Ключ документа для Document Server: змінюється з кожним новим вмістом (новий ключ об'єкта у сховищі),
    /// тож після збереження редактор відкриває свіжу версію, а не кеш.
    /// </summary>
    private static string DocumentKey(int attachmentId, string storageKey) =>
        $"{attachmentId}-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(storageKey)))[..24]}";
}
