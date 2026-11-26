using System.Net;
using System.Net.Http.Json;
using CorrespondenceApi.Common;
using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceApi.Infrastructure.DataService;

/// <summary>Налаштування клієнта Data API (секція <c>DataApi</c>).</summary>
public sealed class DataApiOptions
{
    /// <summary>
    /// Адреса сервісу даних. За замовчуванням — ім'я ресурсу Aspire <c>dataservice</c>
    /// (розв'язується service discovery; поза Aspire — із секції <c>Services:dataservice</c>).
    /// </summary>
    public string BaseUrl { get; set; } = "https+http://dataservice";

    /// <summary>Спільний секрет із сервісом даних (у Aspire — параметр <c>data-api-key</c>).</summary>
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Помилка, яку повернув сервіс даних (400 / 401 / 404 …). Тіло відповіді (JSON { title, status, errors? })
/// передається клієнту без змін — див. GlobalExceptionHandler.
/// </summary>
public sealed class DataApiException(HttpStatusCode statusCode, string? body)
    : Exception($"Data API повернув {(int)statusCode}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Body { get; } = body;
}

/// <summary>
/// Клієнт внутрішнього Data API: <c>POST /rpc/{операція}</c> із JSON запиту MediatR.
/// Додає спільний секрет і Id поточного користувача (з JWT) — від його імені сервіс даних
/// застосовує тенант-ізоляцію та зону видимості документів.
/// </summary>
public sealed class DataApiClient(HttpClient http, DataApiOptions options, IHttpContextAccessor accessor)
{
    /// <summary>Запит із результатом; користувач — із поточного HTTP-запиту.</summary>
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct) =>
        SendAsync(request, CurrentUserId(), ct);

    /// <summary>Запит із результатом від імені вказаного користувача (null — анонімно).</summary>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, int? userId, CancellationToken ct)
    {
        using var response = await PostAsync(request, userId, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return default!;
        return (await response.Content.ReadFromJsonAsync<TResponse>(ct))!;
    }

    /// <summary>Команда без результату; користувач — із поточного HTTP-запиту.</summary>
    public async Task SendAsync(IRequest request, CancellationToken ct)
    {
        using var _ = await PostAsync(request, CurrentUserId(), ct);
    }

    private async Task<HttpResponseMessage> PostAsync(object request, int? userId, CancellationToken ct)
    {
        var type = request.GetType();
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{DataApi.RoutePrefix}/{DataApi.OperationName(type)}")
        {
            Content = JsonContent.Create(request, type),
        };
        message.Headers.Add(DataApi.ApiKeyHeader, options.ApiKey);
        if (userId is int id) message.Headers.Add(DataApi.UserIdHeader, id.ToString());

        var response = await http.SendAsync(message, ct);
        if (response.IsSuccessStatusCode) return response;

        var body = await response.Content.ReadAsStringAsync(ct);
        response.Dispose();
        throw new DataApiException(response.StatusCode, body);
    }

    private int? CurrentUserId() => accessor.HttpContext?.User.FindUserId();
}
