using CorrespondenceApi.Features.Access.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Access;

/// <summary>
/// Слайс: створення / оновлення ролі разом із набором пермішенів
/// (процедура dbo.usp_Role_Save, табличний параметр dbo.CodeList).
/// </summary>
public static class SaveRole
{
    public record Command(int? Id, RoleInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(50);
    }

    public class Handler(Db db) : IRequestHandler<Command, int>
    {
        public Task<int> Handle(Command c, CancellationToken ct) =>
            db.ScalarProcAsync<int>("dbo.usp_Role_Save", new
            {
                c.Id,
                c.Data.Name,
                c.Data.Description,
                PermissionCodes = SqlTvp.CodeList(c.Data.Permissions ?? []),
            }, ct);
    }
}
