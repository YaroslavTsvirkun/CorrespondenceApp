using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Organizations;
using CorrespondenceSystem.Contracts.Organizations.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Organizations;

/// <summary>Фіча «Організації» (лише супер-адмін: organizations.manage).</summary>
public static class OrganizationsEndpoints
{
    public static void MapOrganizations(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/organizations").WithTags("Organizations")
            .RequireAuthorization(Permissions.OrganizationsManage);

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetOrganizations.Query(), ct)));

        g.MapPost("", async (ISender m, OrganizationInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveOrganization.Command(null, i), ct);
            return Results.Created($"/api/organizations/{id}", new { id });
        });

        g.MapPut("/{id:int}", async (ISender m, int id, OrganizationInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveOrganization.Command(id, i), ct);
            return Results.NoContent();
        });

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteOrganization.Command(id), ct);
            return Results.NoContent();
        });
    }
}
