using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Слайс: реєстр документів (пошук, фільтри, сортування, сторінки).</summary>
public static class GetDocuments
{
    [RequirePermission(Permissions.DocumentsReadAny)]
    public class Query : IRequest<PagedResult<DocumentListItem>>
    {
        public string? Search { get; set; }
        public int? DirectionId { get; set; }
        public int? DocumentTypeId { get; set; }
        public int? StatusId { get; set; }
        public bool? IsControlled { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public string? SortBy { get; set; }   // date | number | title
        public bool? Desc { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
