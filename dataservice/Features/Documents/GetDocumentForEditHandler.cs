using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: сирі поля документа для форми редагування.</summary>
public sealed class GetDocumentForEditHandler(Db db, ICurrentUser user) : IRequestHandler<GetDocumentForEdit.Query, DocumentInput>
{
    public async Task<DocumentInput> Handle(GetDocumentForEdit.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("Id", q.Id);
        p.Add("CurrentYear", DateTime.Now.Year);

        var d = await db.QuerySingleOrDefaultAsync<DocumentInput>($"""
            SELECT d.DirectionId, d.DocumentTypeId, d.CurrentStatusId,
                   d.DeliveryChannelId, d.CorrespondentId, d.SenderDepartmentId,
                   d.ReceiverDepartmentId, d.AuthorEmployeeId,
                   d.Title, d.Summary, d.DocumentDate, d.IncomingNumber, d.IsControlled, d.Note,
                   ISNULL(d.RegistrationNumber, N'')        AS RegistrationNumber,
                   ISNULL(d.RegistrationYear, @CurrentYear) AS RegistrationYear,
                   ISNULL(d.RegistrarEmployeeId, 0)         AS RegistrarEmployeeId
            FROM dbo.vw_Document d
            WHERE d.Id = @Id AND {SqlScope.DocumentFilter("d")}
            """, p, ct);
        return d ?? throw new NotFoundException($"Документ #{q.Id} не знайдено");
    }
}
