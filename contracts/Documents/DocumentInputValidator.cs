using CorrespondenceSystem.Contracts.Documents.Dtos;
using FluentValidation;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Спільна валідація полів документа (створення та редагування).</summary>
public class DocumentInputValidator : AbstractValidator<DocumentInput>
{
    public DocumentInputValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Вкажіть тему документа").MaximumLength(300);
        RuleFor(x => x.DirectionId).GreaterThan(0).WithMessage("Оберіть напрям");
        RuleFor(x => x.DocumentTypeId).GreaterThan(0).WithMessage("Оберіть тип документа");
        RuleFor(x => x.CurrentStatusId).GreaterThan(0).WithMessage("Оберіть статус");
        // Номер можна не вказувати — буде згенеровано автоматично за патерном організації
        RuleFor(x => x.RegistrationNumber).MaximumLength(30);
        RuleFor(x => x.RegistrationYear).InclusiveBetween(1990, 2100);
        RuleFor(x => x.RegistrarEmployeeId).GreaterThan(0).WithMessage("Оберіть реєстратора");
        RuleFor(x => x.DocumentDate).NotEqual(default(DateOnly)).WithMessage("Вкажіть дату документа");
        RuleForEach(x => x.Addressees).ChildRules(a =>
            a.RuleFor(x => x.Role).InclusiveBetween((byte)1, (byte)2).WithMessage("Роль адресата: 1 — адресат, 2 — копія"));
        RuleFor(x => x.Addressees)
            .Must(list => list is null || list.Select(a => a.PartyId).Distinct().Count() == list.Count)
            .WithMessage("Контрагента додано до адресатів документа двічі");
        RuleFor(x => x.Addressees)
            .Must((x, list) => list is null || x.CorrespondentId is null || list.All(a => a.PartyId != x.CorrespondentId))
            .WithMessage("Головний кореспондент уже є в документі — не додавайте його ще й до адресатів");
    }
}
