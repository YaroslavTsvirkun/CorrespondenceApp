using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Слайс: картка документа (vw_Document + доручення з vw_Task).</summary>
public static class GetDocumentById
{
    public record Query(int Id) : IRequest<DocumentDetail>;
}
