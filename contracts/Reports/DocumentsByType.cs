using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Reports.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Reports;

/// <summary>Звіт: кількість документів за типами (dbo.usp_Report_DocumentsByType).</summary>
public static class DocumentsByType
{
    public record Query : IRequest<IReadOnlyList<DocumentsByTypeRow>>;
}
