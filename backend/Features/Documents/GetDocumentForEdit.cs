using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Documents.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>Слайс: сирі поля документа для форми редагування.</summary>
public static class GetDocumentForEdit
{
    public record Query(int Id) : IRequest<DocumentInput>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, DocumentInput>
    {
        public async Task<DocumentInput> Handle(Query q, CancellationToken ct)
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
}
