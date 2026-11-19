using CorrespondenceSystem.Contracts.Common;
using MediatR;
using Microsoft.Extensions.Http.Resilience;

namespace CorrespondenceApi.Infrastructure.DataService;

/// <summary>Пересилання запиту MediatR у сервіс даних (хендлер із SQL-кодом виконується там).</summary>
public sealed class DataApiRequestHandler<TRequest, TResponse>(DataApiClient client)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken ct) => client.SendAsync(request, ct);
}

/// <summary>Пересилання команди без результату в сервіс даних.</summary>
public sealed class DataApiRequestHandler<TRequest>(DataApiClient client) : IRequestHandler<TRequest>
    where TRequest : IRequest
{
    public Task Handle(TRequest request, CancellationToken ct) => client.SendAsync(request, ct);
}

public static class DataApiExtensions
{
    /// <summary>
    /// Клієнт Data API + хендлери-пересилачі для кожного запиту з Contracts:
    /// <c>ISender.Send(new GetDocuments.Query(...))</c> у backend виконується в сервісі даних.
    /// </summary>
    public static IServiceCollection AddDataApi(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("DataApi").Get<DataApiOptions>() ?? new DataApiOptions();
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Не задано DataApi:ApiKey — спільний секрет для викликів сервісу даних.");
        services.AddSingleton(options);

        // Усі виклики — POST: повтор після таймауту міг би, напр., зареєструвати документ двічі,
        // тому стандартний resilience-обробник (із ServiceDefaults) замінюється на варіант без повторів POST.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers — експериментальний API
        services.AddHttpClient<DataApiClient>(http => http.BaseAddress = new Uri(options.BaseUrl))
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(r => r.Retry.DisableForUnsafeHttpMethods());
#pragma warning restore EXTEXP0001

        foreach (var request in DataApi.RequestTypes)
        {
            var response = DataApi.ResponseType(request);
            if (response is null)
                services.AddTransient(typeof(IRequestHandler<>).MakeGenericType(request),
                    typeof(DataApiRequestHandler<>).MakeGenericType(request));
            else
                services.AddTransient(typeof(IRequestHandler<,>).MakeGenericType(request, response),
                    typeof(DataApiRequestHandler<,>).MakeGenericType(request, response));
        }
        return services;
    }
}
