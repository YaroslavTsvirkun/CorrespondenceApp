using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Слайс: редагування документа (процедура dbo.usp_Document_Update).</summary>
public static class UpdateDocument
{
    [RequirePermission(Permissions.DocumentsUpdate)]
    public record Command(int Id, DocumentInput Input) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new DocumentInputValidator());
    }
}
