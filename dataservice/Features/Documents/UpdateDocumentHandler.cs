using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: редагування документа (процедура dbo.usp_Document_Update).</summary>
public sealed class UpdateDocumentHandler(Db db, ICurrentUser user) : IRequestHandler<UpdateDocument.Command>
{
    public async Task Handle(UpdateDocument.Command c, CancellationToken ct)
    {
        var p = SqlScope.Tenant(user);
        p.AddDynamicParams(c.Input);
        p.Add("Id", c.Id);
        await db.ExecuteProcAsync("dbo.usp_Document_Update", p, ct);
    }
}
