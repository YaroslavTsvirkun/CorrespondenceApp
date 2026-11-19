using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Correspondents.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Correspondents;

/// <summary>Слайс: створення / оновлення кореспондента.</summary>
public static class SaveCorrespondent
{
    public record Command(int? Id, CorrespondentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Data.CorrespondentTypeId).GreaterThan(0).WithMessage("Оберіть тип кореспондента");
        }
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(c.Data);
            p.Add("Id", c.Id);

            if (c.Id is null)
            {
                var orgId = user.OrganizationId
                    ?? throw new ValidationException([new ValidationFailure("organization", "Створення доступне лише в межах організації")]);
                p.Add("OrganizationId", orgId);
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Correspondent (OrganizationId, CorrespondentTypeId, Name, Address, Phone, Email, ContactPerson, Note)
                    VALUES (@OrganizationId, @CorrespondentTypeId, @Name, @Address, @Phone, @Email, @ContactPerson, @Note);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, p, ct);
            }

            var affected = await db.ExecuteAsync($"""
                UPDATE c
                SET CorrespondentTypeId = @CorrespondentTypeId, Name = @Name, Address = @Address,
                    Phone = @Phone, Email = @Email, ContactPerson = @ContactPerson, Note = @Note
                FROM dbo.Correspondent c
                WHERE c.Id = @Id AND {SqlScope.TenantFilter("c")}
                """, p, ct);
            if (affected == 0) throw new NotFoundException($"Кореспондента #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}
