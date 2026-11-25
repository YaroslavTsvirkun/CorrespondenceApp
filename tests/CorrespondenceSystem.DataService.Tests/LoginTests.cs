using System.Net;
using System.Net.Http.Json;
using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users;
using CorrespondenceSystem.Contracts.Users.Dtos;
using static CorrespondenceSystem.DataService.Tests.DataServiceFactory;

namespace CorrespondenceSystem.DataService.Tests;

/// <summary>Вхід: блокування облікового запису після серії невдалих спроб.</summary>
[Collection(DemoDatabaseCollection.Name)]
public sealed class LoginTests(DataServiceFactory app)
{
    private async Task<(string UserName, int Id)> CreateUserAsync(string password)
    {
        var admin = (await app.LoginAsync("admin", "admin123")).UserId;
        var userName = $"lock{Guid.NewGuid():N}"[..20];
        var id = await app.RpcOkAsync<int>(new SaveUser.Command(null,
            new UserInput(userName, password, "Тест блокування", null, null, true, [])), admin);
        return (userName, id);
    }

    private Task<HttpResponseMessage> TryLogin(string userName, string password) =>
        app.RpcAsync(new VerifyCredentials.Query(userName, password), null);

    [Fact]
    public async Task Account_is_locked_after_repeated_failures_and_unlocked_by_password_reset()
    {
        var (userName, id) = await CreateUserAsync("Correct-Pass-1");

        for (var i = 0; i < MaxFailedAttempts; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await TryLogin(userName, "wrong")).StatusCode);

        // Навіть правильний пароль не приймається, поки діє блокування
        var locked = await TryLogin(userName, "Correct-Pass-1");
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Contains("заблоковано", await locked.Content.ReadAsStringAsync());

        // Адміністратор задає новий пароль — блокування знімається
        var admin = (await app.LoginAsync("admin", "admin123")).UserId;
        await EnsureStatusAsync(await app.RpcAsync(new SaveUser.Command(id,
            new UserInput(userName, "New-Pass-2", "Тест блокування", null, null, true, [])), admin),
            HttpStatusCode.OK);
        await EnsureStatusAsync(await TryLogin(userName, "New-Pass-2"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Successful_login_resets_failure_counter()
    {
        var (userName, _) = await CreateUserAsync("Correct-Pass-1");

        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < MaxFailedAttempts - 1; i++)
                await TryLogin(userName, "wrong");
            await EnsureStatusAsync(await TryLogin(userName, "Correct-Pass-1"), HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Unknown_user_and_wrong_password_look_the_same()
    {
        var unknown = await TryLogin("no-such-user", "whatever");
        var wrong = await TryLogin("registrar", "wrong-password");
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal((await unknown.Content.ReadFromJsonAsync<Problem>())!.Title,
            (await wrong.Content.ReadFromJsonAsync<Problem>())!.Title);
    }

    private sealed record Problem(string Title);
}

/// <summary>Робоча БД: демо-даних немає, перший супер-адмін створюється з налаштування.</summary>
public sealed class ProductionSeedTests(ProductionLikeDataServiceFactory app)
    : IClassFixture<ProductionLikeDataServiceFactory>
{
    [Fact]
    public async Task Demo_users_are_not_created()
    {
        var response = await app.RpcAsync(new VerifyCredentials.Query("registrar", "registrar123"), null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Initial_admin_is_created_with_super_role()
    {
        var admin = await app.LoginAsync("admin", ProductionLikeDataServiceFactory.AdminPassword);
        Assert.Contains(Permissions.OrganizationsManage, admin.Permissions);
        Assert.Null(admin.OrganizationId);

        // Демо-пароль не підходить
        var demo = await app.RpcAsync(new VerifyCredentials.Query("admin", "admin123"), null);
        Assert.Equal(HttpStatusCode.Unauthorized, demo.StatusCode);
    }
}
