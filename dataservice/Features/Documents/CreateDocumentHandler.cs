using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using FluentValidation;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: реєстрація документа (процедура dbo.usp_Document_Create).</summary>
public sealed class CreateDocumentHandler(Db db, ICurrentUser user) : IRequestHandler<CreateDocument.Command, int>
{
    public async Task<int> Handle(CreateDocument.Command c, CancellationToken ct)
    {
        // Організація документа = організація поточного користувача
        // (для супер-адміна процедура візьме її від реєстратора).
        var p = new DynamicParameters(c.Input);
        p.Add("OrganizationId", user.OrganizationId);
        return await db.ScalarProcAsync<int>("dbo.usp_Document_Create", p, ct);
    }
}
