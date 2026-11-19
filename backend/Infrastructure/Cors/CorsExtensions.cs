namespace CorrespondenceApi.Infrastructure.Cors;

public static class CorsExtensions
{
    /// <summary>Реєструє політику CORS за налаштуваннями із секції <c>Cors</c>.</summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(CorsPolicyOptions.SectionName).Get<CorsPolicyOptions>()
                      ?? new CorsPolicyOptions();
        services.AddSingleton(options);

        services.AddCors(o => o.AddPolicy(options.PolicyName, p =>
        {
            if (options.AllowedOrigins.Contains("*")) p.AllowAnyOrigin();
            else p.WithOrigins(options.AllowedOrigins);

            if (options.AllowedMethods.Length == 0) p.AllowAnyMethod();
            else p.WithMethods(options.AllowedMethods);

            if (options.AllowedHeaders.Length == 0) p.AllowAnyHeader();
            else p.WithHeaders(options.AllowedHeaders);

            if (options.ExposedHeaders.Length > 0) p.WithExposedHeaders(options.ExposedHeaders);
            if (options.AllowCredentials) p.AllowCredentials();
            if (options.PreflightMaxAgeSeconds is { } maxAge) p.SetPreflightMaxAge(TimeSpan.FromSeconds(maxAge));
        }));

        return services;
    }

    /// <summary>Підключає політику CORS, зареєстровану через <see cref="AddFrontendCors"/>.</summary>
    public static IApplicationBuilder UseFrontendCors(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<CorsPolicyOptions>();
        return app.UseCors(options.PolicyName);
    }
}
