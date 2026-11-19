using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Users;

/// <summary>Слайс: видалення користувача (не себе; адмін організації — лише своїх).</summary>
public static class DeleteUser
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            if (c.Id == current.UserId)
                throw new ValidationException([new ValidationFailure("user", "Не можна видалити власний обліковий запис")]);

            var user = await db.QuerySingleOrDefaultAsync<(bool Exists, int? OrganizationId)>(
                "SELECT CAST(1 AS bit), OrganizationId FROM dbo.AppUser WHERE Id = @Id", new { c.Id }, ct);
            if (!user.Exists) throw new NotFoundException($"Користувача #{c.Id} не знайдено");

            var isSuper = current.Has(Permissions.OrganizationsManage);
            if (!isSuper && user.OrganizationId != current.OrganizationId)
                throw new ValidationException([new ValidationFailure("user", "Користувач належить іншій організації")]);

            await db.ExecuteAsync("DELETE dbo.AppUser WHERE Id = @Id", new { c.Id }, ct);
        }
    }
}
