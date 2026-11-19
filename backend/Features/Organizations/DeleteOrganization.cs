using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Organizations;

/// <summary>Слайс: видалення організації (dbo.usp_Organization_Delete перевіряє залежності).</summary>
public static class DeleteOrganization
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct) =>
            db.ExecuteProcAsync("dbo.usp_Organization_Delete", new { c.Id }, ct);
    }
}
