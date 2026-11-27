using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Parties;

/// <summary>Слайс: створення / оновлення контрагента разом з адресами, контактами, особами й категоріями.</summary>
public static class SaveParty
{
    [RequirePermission(Permissions.PartiesManage)]
    public record Command(int? Id, PartyInput Data) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Kind).Must(PartyKind.IsValid).WithMessage("Оберіть вид контрагента");
            RuleFor(x => x.Data.FullName).NotEmpty().WithMessage("Вкажіть повну назву або ПІБ").MaximumLength(300);
            RuleFor(x => x.Data.ShortName).MaximumLength(150);
            RuleFor(x => x.Data.Note).MaximumLength(4000);
            RuleFor(x => x.Data.Code)
                .Must((x, code) => PartyRules.IsValidCode(x.Data.Kind, code))
                .WithName("Code")
                .WithMessage(x => PartyKind.HasEdrpou(x.Data.Kind)
                    ? "Невірний код ЄДРПОУ (8 цифр, контрольна сума)"
                    : "Невірний РНОКПП (10 цифр, контрольна сума)");
            RuleFor(x => x.Data.ParentPartyId)
                .Must((x, parent) => parent is null || parent != x.Id)
                .WithMessage("Контрагент не може бути філією самого себе");

            RuleForEach(x => x.Data.Addresses).ChildRules(a =>
            {
                a.RuleFor(x => x.Kind).InclusiveBetween((byte)1, (byte)3).WithMessage("Невідомий вид адреси");
                a.RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Line) || !string.IsNullOrWhiteSpace(x.City))
                    .WithName("Address").WithMessage("Адреса має містити населений пункт або адресний рядок");
                a.RuleFor(x => x.Country).MaximumLength(100);
                a.RuleFor(x => x.PostalCode).MaximumLength(10);
                a.RuleFor(x => x.Region).MaximumLength(100);
                a.RuleFor(x => x.City).MaximumLength(100);
                a.RuleFor(x => x.Line).MaximumLength(300);
            });
            RuleFor(x => x.Data.Addresses)
                .Must(list => list is null || list.GroupBy(a => a.Kind).All(g => g.Count() == 1))
                .WithMessage("Кожен вид адреси (юридична, фактична, поштова) — не більше одного разу");

            RuleForEach(x => x.Data.Contacts).ChildRules(c =>
            {
                c.RuleFor(x => x.Kind).InclusiveBetween((byte)1, (byte)4).WithMessage("Невідомий вид контакту");
                c.RuleFor(x => x.Value).NotEmpty().WithMessage("Вкажіть значення контакту").MaximumLength(200);
                c.RuleFor(x => x.Value).EmailAddress().When(x => x.Kind == 2).WithMessage("Невірна адреса електронної пошти");
                c.RuleFor(x => x.Note).MaximumLength(200);
            });
            RuleFor(x => x.Data.Contacts)
                .Must(list => list is null || list.Where(c => c.IsPrimary).GroupBy(c => c.Kind).All(g => g.Count() == 1))
                .WithMessage("Основним може бути лише один контакт кожного виду");

            RuleForEach(x => x.Data.Persons).ChildRules(p =>
            {
                p.RuleFor(x => x.FullName).NotEmpty().WithMessage("Вкажіть ПІБ контактної особи").MaximumLength(150);
                p.RuleFor(x => x.PositionName).MaximumLength(150);
                p.RuleFor(x => x.Phone).MaximumLength(50);
                p.RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                    .WithMessage("Невірна адреса електронної пошти");
                p.RuleFor(x => x.Note).MaximumLength(200);
            });
        }
    }
}
