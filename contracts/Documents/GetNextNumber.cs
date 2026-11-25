using System.Data;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>Слайс: попередній перегляд наступного реєстраційного номера (dbo.usp_RegistrationNumber_Next).</summary>
public static class GetNextNumber
{
    [RequirePermission(Permissions.DocumentsCreate)]
    public record Query(int DirectionId, int DocumentTypeId, int Year) : IRequest<NextNumberResult>;
}
