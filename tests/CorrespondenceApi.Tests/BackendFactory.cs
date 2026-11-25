using System.Net;
using System.Net.Http.Json;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using CorrespondenceSystem.Contracts.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CorrespondenceApi.Tests;

/// <summary>
/// Backend із підміненим сервісом даних (<see cref="FakeDataApi"/>): перевіряються механізми самого backend —
/// cookie з JWT, захист від CSRF, ліміт спроб входу, Swagger лише в Development.
/// </summary>
public class BackendFactory : WebApplicationFactory<Program>
{
    /// <summary>Ліміт спроб входу з однієї IP за хвилину (у тестах усі запити — з однієї адреси).</summary>
    public virtual int LoginPermitLimit => 100;

    protected virtual string Environment => "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.UseSetting("Jwt:Key", "test-jwt-signing-key-0123456789-abcdefghijklmnop");
        builder.UseSetting("DataApi:ApiKey", "test-key");
        builder.UseSetting("DataApi:BaseUrl", "http://dataservice.test");
        builder.UseSetting("RateLimiting:Login:PermitLimit", LoginPermitLimit.ToString());
        builder.UseSetting("RateLimiting:Login:WindowSeconds", "60");
        builder.ConfigureServices(services =>
            services.AddHttpClient<DataApiClient>().ConfigurePrimaryHttpMessageHandler(() => new FakeDataApi()));
    }

    /// <summary>Клієнт без автоматичної обробки cookie — заголовки Set-Cookie/Cookie видно в тестах.</summary>
    public HttpClient CreateRawClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
}

/// <summary>Низький ліміт спроб входу — для перевірки 429.</summary>
public sealed class RateLimitedBackendFactory : BackendFactory
{
    public override int LoginPermitLimit => 3;
}

public sealed class DevelopmentBackendFactory : BackendFactory
{
    protected override string Environment => "Development";
}

/// <summary>
/// Сервіс даних-заглушка: користувач «user» з паролем «good» (Id 7, право tasks.assign);
/// решта операцій повертає порожній результат.
/// </summary>
internal sealed class FakeDataApi : HttpMessageHandler
{
    private static readonly UserInfo User = new(7, "user", "Тестовий користувач", 1, 1, 1, "Org",
        ["Роль"], [Permissions.TasksAssign]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var operation = request.RequestUri!.AbsolutePath[(DataApi.RoutePrefix.Length + 1)..];
        switch (operation)
        {
            case "Auth.VerifyCredentials.Query":
                var body = await request.Content!.ReadFromJsonAsync<Dictionary<string, string>>(ct);
                return body!["password"] == "good"
                    ? Json(HttpStatusCode.OK, User)
                    : Json(HttpStatusCode.Unauthorized, new { title = "Невірний логін або пароль", status = 401 });
            case "Auth.Me.Query":
                return request.Headers.TryGetValues(DataApi.UserIdHeader, out var ids) && ids.Single() == "7"
                    ? Json(HttpStatusCode.OK, User)
                    : Json(HttpStatusCode.Unauthorized, new { title = "Користувача не знайдено", status = 401 });
            case "Tasks.CreateTask.Command":
                return Json(HttpStatusCode.OK, 42);
            default:
                return Json(HttpStatusCode.OK, Array.Empty<object>());
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, object value) =>
        new(code) { Content = JsonContent.Create(value) };
}
