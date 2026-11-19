using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Departments.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Departments;

/// <summary>Фіча «Підрозділи» (дерево підрозділів у межах організації).</summary>
public static class DepartmentsEndpoints
{
    public static void MapDepartments(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/departments").WithTags("Departments");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetDepartments.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, DepartmentInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveDepartment.Command(null, i), ct);
            return Results.Created($"/api/departments/{id}", new { id });
        }).RequireAuthorization(Permissions.DepartmentsManage);

        g.MapPut("/{id:int}", async (ISender m, int id, DepartmentInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveDepartment.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DepartmentsManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteDepartment.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DepartmentsManage);
    }
}
