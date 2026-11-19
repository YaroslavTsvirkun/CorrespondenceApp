using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Departments;

/// <summary>
/// Слайс: видалення підрозділу (dbo.usp_Department_Delete перевіряє наявність
/// працівників і дочірніх підрозділів).
/// </summary>
public static class DeleteDepartment
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            return db.ExecuteProcAsync("dbo.usp_Department_Delete", p, ct);
        }
    }
}
