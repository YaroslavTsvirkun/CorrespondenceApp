using System.Net;
using System.Net.Http.Json;
using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using CorrespondenceSystem.Contracts.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace CorrespondenceSystem.DataService.Tests;

/// <summary>
/// Сервіс даних на окремій тимчасовій БД у SQL Server LocalDB: під час старту розгортається весь
/// SQL-код (схема, процедури, довідники, ролі) і — якщо <see cref="SeedDemoData"/> — демо-дані.
/// Після тестів БД видаляється.
/// </summary>
public class DataServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiKey = "test-data-api-key";
    public const int MaxFailedAttempts = 3;

    private readonly string _database = $"CorrespondenceApiDb_Tests_{Guid.NewGuid():N}";

    protected virtual bool SeedDemoData => true;
    protected virtual string? InitialAdminPassword => null;

    public string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={_database};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Database:DeployOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", SeedDemoData ? "true" : "false");
        builder.UseSetting("Database:InitialAdmin:Password", InitialAdminPassword ?? "");
        builder.UseSetting("DataApi:ApiKey", ApiKey);
        builder.UseSetting("Security:Lockout:MaxFailedAttempts", MaxFailedAttempts.ToString());
        builder.UseSetting("Security:Lockout:LockoutMinutes", "15");
    }

    public Task InitializeAsync()
    {
        _ = Server; // старт хоста: розгортання БД
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        SqlConnection.ClearAllPools();
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{_database}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_database}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }

    // ---- Виклики Data API ----

    /// <summary>POST /rpc/{операція} від імені користувача (null — анонімно).</summary>
    public Task<HttpResponseMessage> RpcAsync(object request, int? userId, string? apiKey = ApiKey)
    {
        var client = CreateClient();
        var message = new HttpRequestMessage(HttpMethod.Post,
            $"{DataApi.RoutePrefix}/{DataApi.OperationName(request.GetType())}")
        {
            Content = JsonContent.Create(request, request.GetType()),
        };
        if (apiKey is not null) message.Headers.Add(DataApi.ApiKeyHeader, apiKey);
        if (userId is int id) message.Headers.Add(DataApi.UserIdHeader, id.ToString());
        return client.SendAsync(message);
    }

    /// <summary>Виклик, що має завершитися успішно; повертає результат.</summary>
    public async Task<T> RpcOkAsync<T>(object request, int? userId)
    {
        var response = await RpcAsync(request, userId);
        await EnsureStatusAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task<UserInfo> LoginAsync(string userName, string password) =>
        await RpcOkAsync<UserInfo>(new VerifyCredentials.Query(userName, password), null);

    public static async Task EnsureStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
            Assert.Fail($"Очікувався {(int)expected}, отримано {(int)response.StatusCode}: " +
                        await response.Content.ReadAsStringAsync());
    }
}

/// <summary>Робоча БД: без демо-даних, перший супер-адмін — з налаштування Database:InitialAdmin.</summary>
public sealed class ProductionLikeDataServiceFactory : DataServiceFactory
{
    public const string AdminPassword = "Initial-Admin-Pass-42";

    protected override bool SeedDemoData => false;
    protected override string? InitialAdminPassword => AdminPassword;
}

/// <summary>Одна БД з демо-даними на всі тести колекції (розгортання займає кілька секунд).</summary>
[CollectionDefinition(Name)]
public sealed class DemoDatabaseCollection : ICollectionFixture<DataServiceFactory>
{
    public const string Name = "demo-database";
}
