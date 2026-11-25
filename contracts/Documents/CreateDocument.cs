using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Слайс: реєстрація документа (процедура dbo.usp_Document_Create).</summary>
public static class CreateDocument
{
    [RequirePermission(Permissions.DocumentsCreate)]
    public record Command(DocumentInput Input) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new DocumentInputValidator());
    }
}
