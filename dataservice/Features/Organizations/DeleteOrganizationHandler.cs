using CorrespondenceSystem.Contracts.Organizations;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Organizations;

/// <summary>Дані слайсу: видалення організації (dbo.usp_Organization_Delete перевіряє залежності).</summary>
public sealed class DeleteOrganizationHandler(Db db) : IRequestHandler<DeleteOrganization.Command>
{
    public Task Handle(DeleteOrganization.Command c, CancellationToken ct) =>
        db.ExecuteProcAsync("dbo.usp_Organization_Delete", new { c.Id }, ct);
}
