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
    }
}
