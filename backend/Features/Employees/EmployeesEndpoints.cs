using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Employees.Dtos;
using MediatR;

namespace CorrespondenceApi.Features.Employees;

/// <summary>Фіча «Працівники» (у межах організації користувача).</summary>
public static class EmployeesEndpoints
{
    public static void MapEmployees(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employees").WithTags("Employees");

        g.MapGet("", async (ISender m, CancellationToken ct) => Results.Ok(await m.Send(new GetEmployees.Query(), ct)))
            .RequireAuthorization();

        g.MapPost("", async (ISender m, EmployeeInput i, CancellationToken ct) =>
        {
            var id = await m.Send(new SaveEmployee.Command(null, i), ct);
            return Results.Created($"/api/employees/{id}", new { id });
        }).RequireAuthorization(Permissions.EmployeesManage);

        g.MapPut("/{id:int}", async (ISender m, int id, EmployeeInput i, CancellationToken ct) =>
        {
            await m.Send(new SaveEmployee.Command(id, i), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.EmployeesManage);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteEmployee.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.EmployeesManage);
    }
}
