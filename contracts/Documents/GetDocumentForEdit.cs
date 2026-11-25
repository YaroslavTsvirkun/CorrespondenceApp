using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Слайс: сирі поля документа для форми редагування.</summary>
public static class GetDocumentForEdit
{
    [RequirePermission(Permissions.DocumentsReadAny)]
    public record Query(int Id) : IRequest<DocumentInput>;
}
