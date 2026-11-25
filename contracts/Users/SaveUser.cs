using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Users.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Users;

/// <summary>
/// Слайс: створення / оновлення користувача з перевіркою прав адміністратора
/// (запис — процедура dbo.usp_User_Save, ролі — табличний параметр dbo.IdList).
/// </summary>
public static class SaveUser
{
    [RequirePermission(Permissions.UsersManage)]
    public record Command(int? Id, UserInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.UserName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Data.FullName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Data.Password)
                .NotEmpty().MinimumLength(5)
                .When(x => x.Id is null)
                .WithMessage("Вкажіть пароль (мінімум 5 символів)");
        }
    }
}
