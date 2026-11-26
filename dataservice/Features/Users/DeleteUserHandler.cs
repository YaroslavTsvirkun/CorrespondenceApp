using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Users;

/// <summary>Дані слайсу: видалення користувача (не себе; адмін організації — лише своїх).</summary>
public sealed class DeleteUserHandler(Db db, ICurrentUser current) : IRequestHandler<DeleteUser.Command>
{
    public async Task Handle(DeleteUser.Command c, CancellationToken ct)
    {
        if (c.Id == current.UserId)
            throw Errors.Validation("user", "Не можна видалити власний обліковий запис");

        var user = await db.QuerySingleOrDefaultAsync<(bool Exists, int? OrganizationId)>(
            "SELECT CAST(1 AS bit), OrganizationId FROM dbo.AppUser WHERE Id = @Id", new { c.Id }, ct);
        if (!user.Exists) throw new NotFoundException($"Користувача #{c.Id} не знайдено");

        var isSuper = current.Has(Permissions.OrganizationsManage);
        if (!isSuper && user.OrganizationId != current.OrganizationId)
            throw Errors.Validation("user", "Користувач належить іншій організації");

        await db.ExecuteAsync("DELETE dbo.AppUser WHERE Id = @Id", new { c.Id }, ct);
    }
}
