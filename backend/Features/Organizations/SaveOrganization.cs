using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Organizations.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Organizations;

/// <summary>Слайс: створення / оновлення організації.</summary>
public static class SaveOrganization
{
    public record Command(int? Id, OrganizationInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
    }

    public class Handler(Db db) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var d = c.Data;
            var param = new
            {
                c.Id,
                d.Name,
                d.Code,
                d.IsActive,
                // Організація не може бути батьківською сама для себе
                ParentOrganizationId = c.Id is not null && d.ParentOrganizationId == c.Id ? null : d.ParentOrganizationId,
                Pattern = string.IsNullOrWhiteSpace(d.RegistrationNumberPattern) ? null : d.RegistrationNumberPattern.Trim(),
            };

            if (c.Id is null)
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Organization (Name, Code, IsActive, ParentOrganizationId, RegistrationNumberPattern)
                    VALUES (@Name, @Code, @IsActive, @ParentOrganizationId, ISNULL(@Pattern, N'{type}-{seq:000}/{year}'));
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, param, ct);

            var affected = await db.ExecuteAsync("""
                UPDATE dbo.Organization
                SET Name = @Name, Code = @Code, IsActive = @IsActive,
                    ParentOrganizationId = @ParentOrganizationId,
                    RegistrationNumberPattern = ISNULL(@Pattern, RegistrationNumberPattern)
                WHERE Id = @Id
                """, param, ct);
            if (affected == 0) throw new NotFoundException($"Організацію #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}
