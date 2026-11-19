using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Organizations;
using CorrespondenceSystem.Contracts.Organizations.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Organizations;

/// <summary>Дані слайсу: створення / оновлення організації.</summary>
public sealed class SaveOrganizationHandler(Db db) : IRequestHandler<SaveOrganization.Command, int>
{
    public async Task<int> Handle(SaveOrganization.Command c, CancellationToken ct)
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
