using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>
/// Параметри процедур dbo.usp_Document_Create / _Update з полів документа. Скалярні поля передаються
/// явно, а адресати — табличним параметром dbo.DocumentPartyList (список як шаблон Dapper розгорнув би в IN-параметри).
/// </summary>
internal static class DocumentParams
{
    public static DynamicParameters From(DocumentInput i, DynamicParameters? p = null)
    {
        p ??= new DynamicParameters();
        p.AddDynamicParams(new
        {
            i.DirectionId, i.DocumentTypeId, i.CurrentStatusId, i.DeliveryChannelId, i.CorrespondentId,
            i.SenderDepartmentId, i.ReceiverDepartmentId, i.AuthorEmployeeId, i.Title, i.Summary, i.DocumentDate,
            i.IncomingNumber, i.IsControlled, i.Note, i.RegistrationNumber, i.RegistrationYear, i.RegistrarEmployeeId,
            Addressees = SqlTvp.DocumentParties(i.Addressees ?? []),
        });
        return p;
    }
}
