using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>
/// Дані слайсу: видалення документа (каскадно з реєстрацією, резолюціями, дорученнями, вкладеннями).
/// Повертає ключі файлів вкладень — їх видаляє зі сховища backend.
/// </summary>
public sealed class DeleteDocumentRecordHandler(Db db, ICurrentUser user)
    : IRequestHandler<DeleteDocumentRecord.Command, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(DeleteDocumentRecord.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.Add("Id", c.Id);
        return await db.QueryProcAsync<string>("dbo.usp_Document_Delete", p, ct);
    }
}
