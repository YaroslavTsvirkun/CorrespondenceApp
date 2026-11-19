using CorrespondenceSystem.Contracts.Access.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Access;

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
}
