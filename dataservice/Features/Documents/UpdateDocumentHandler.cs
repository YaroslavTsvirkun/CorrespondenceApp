using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: редагування документа (процедура dbo.usp_Document_Update).</summary>
public sealed class UpdateDocumentHandler(Db db, ICurrentUser user) : IRequestHandler<UpdateDocument.Command>
{
    public async Task Handle(UpdateDocument.Command c, CancellationToken ct)
    {
        var p = DocumentParams.From(c.Input, SqlScope.Tenant(user));
        p.Add("Id", c.Id);
        // Адресати не передані — лишаються як були
        p.Add("ReplaceAddressees", c.Input.Addressees is not null);
        await db.ExecuteProcAsync("dbo.usp_Document_Update", p, ct);
    }
}
