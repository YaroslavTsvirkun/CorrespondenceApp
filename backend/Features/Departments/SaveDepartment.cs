using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Departments.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceApi.Features.Departments;

/// <summary>Слайс: створення / оновлення підрозділу.</summary>
public static class SaveDepartment
{
    public record Command(int? Id, DepartmentInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(150);
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var d = c.Data;
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(new
            {
                c.Id,
                d.Name,
                d.ShortName,
                d.Description,
                d.IsActive,
                // Підрозділ не може бути батьківським сам для себе
                ParentDepartmentId = c.Id is not null && d.ParentDepartmentId == c.Id ? null : d.ParentDepartmentId,
            });

            if (c.Id is null)
            {
                var orgId = user.OrganizationId ?? d.OrganizationId
                    ?? throw new ValidationException([new ValidationFailure("organization", "Оберіть організацію")]);
                p.Add("OrganizationId", orgId);
                return await db.ScalarAsync<int>("""
                    INSERT dbo.Department (OrganizationId, Name, ShortName, Description, IsActive, ParentDepartmentId)
                    VALUES (@OrganizationId, @Name, @ShortName, @Description, @IsActive, @ParentDepartmentId);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """, p, ct);
            }

            var affected = await db.ExecuteAsync($"""
                UPDATE d
                SET Name = @Name, ShortName = @ShortName, Description = @Description,
                    IsActive = @IsActive, ParentDepartmentId = @ParentDepartmentId
                FROM dbo.Department d
                WHERE d.Id = @Id AND {SqlScope.TenantFilter("d")}
                """, p, ct);
            if (affected == 0) throw new NotFoundException($"Підрозділ #{c.Id} не знайдено");
            return c.Id.Value;
        }
    }
}
