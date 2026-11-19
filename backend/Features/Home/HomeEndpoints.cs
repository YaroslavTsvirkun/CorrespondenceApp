namespace CorrespondenceApi.Features.Home;

/// <summary>Фіча «Корінь API»: перенаправлення з «/» на Swagger UI.</summary>
public static class HomeEndpoints
{
    public static void MapHome(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Redirect("/swagger"))
            .WithName("Home")
            .ExcludeFromDescription()
            .AllowAnonymous();
    }
}
