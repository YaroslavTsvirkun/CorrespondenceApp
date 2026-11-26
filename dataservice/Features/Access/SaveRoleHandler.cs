using CorrespondenceSystem.Contracts.Access;
using CorrespondenceSystem.Contracts.Access.Dtos;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Access;

/// <summary>
/// Дані слайсу: створення / оновлення ролі разом із набором пермішенів
/// (процедура dbo.usp_Role_Save, табличний параметр dbo.CodeList).
/// </summary>
public sealed class SaveRoleHandler(Db db) : IRequestHandler<SaveRole.Command, int>
{
    public Task<int> Handle(SaveRole.Command c, CancellationToken ct) =>
        db.ScalarProcAsync<int>("dbo.usp_Role_Save", new
        {
            c.Id,
            c.Data.Name,
            c.Data.Description,
            PermissionCodes = SqlTvp.CodeList(c.Data.Permissions ?? []),
        }, ct);
}
