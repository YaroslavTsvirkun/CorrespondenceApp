using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CorrespondenceApi.Tests;

public sealed class AuthCookieTests(BackendFactory app) : IClassFixture<BackendFactory>
{
    private static readonly object Task = new
    {
        documentId = 1, resolutionText = "Р", executorEmployeeId = 1, statusId = 1,
        taskText = "Т", dueDate = "2030-01-01", completedAt = (string?)null, resultText = (string?)null,
    };

    private async Task<string> LoginCookieAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "user", password = "good" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("cs_auth="));
        return setCookie.Split(';')[0];
    }

    [Fact]
    public async Task Login_sets_httponly_strict_cookie_and_does_not_return_token()
    {
        using var client = app.CreateRawClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "user", password = "good" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("cs_auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api", cookie, StringComparison.OrdinalIgnoreCase);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("token", out _));
        Assert.Equal("user", json.RootElement.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Cookie_authenticates_reads()
    {
        using var client = app.CreateRawClient();
        var cookie = await LoginCookieAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Unsafe_request_with_cookie_requires_csrf_header()
    {
        using var client = app.CreateRawClient();
        var cookie = await LoginCookieAsync(client);

        var withoutHeader = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(Task) };
        withoutHeader.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(withoutHeader)).StatusCode);

        var withHeader = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(Task) };
        withHeader.Headers.Add("Cookie", cookie);
        withHeader.Headers.Add("X-Requested-With", "fetch");
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(withHeader)).StatusCode);
    }

    [Fact]
    public async Task Logout_expires_cookie()
    {
        using var client = app.CreateRawClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-Requested-With", "fetch");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("cs_auth="));
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Окремий хост — власний лічильник ліміту, не залежить від інших тестів.</summary>
public sealed class LoginRateLimitTests : IDisposable
{
    private readonly RateLimitedBackendFactory _app = new();

    [Fact]
    public async Task Too_many_login_attempts_get_429()
    {
        using var client = _app.CreateRawClient();
        for (var i = 0; i < _app.LoginPermitLimit; i++)
        {
            var attempt = await client.PostAsJsonAsync("/api/auth/login", new { userName = "user", password = "bad" });
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/auth/login", new { userName = "user", password = "good" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.Contains("Retry-After"));
        Assert.Contains("Забагато спроб входу", await limited.Content.ReadAsStringAsync());
    }

    public void Dispose() => _app.Dispose();
}

public sealed class SwaggerTests
{
    [Fact]
    public async Task Swagger_is_hidden_outside_development()
    {
        using var app = new BackendFactory();
        using var client = app.CreateRawClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Swagger_is_available_in_development()
    {
        using var app = new DevelopmentBackendFactory();
        using var client = app.CreateRawClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
}
