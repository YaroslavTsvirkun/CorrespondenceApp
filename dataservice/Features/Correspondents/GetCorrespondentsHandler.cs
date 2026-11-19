using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Correspondents;
using CorrespondenceSystem.Contracts.Correspondents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Correspondents;

/// <summary>Дані слайсу: кореспонденти організації користувача.</summary>
public sealed class GetCorrespondentsHandler(Db db, ICurrentUser user) : IRequestHandler<GetCorrespondents.Query, IReadOnlyList<CorrespondentDto>>
{
    public Task<IReadOnlyList<CorrespondentDto>> Handle(GetCorrespondents.Query q, CancellationToken ct) =>
        db.QueryAsync<CorrespondentDto>($"""
            SELECT c.Id, c.Name, c.CorrespondentTypeId, t.Name AS TypeName,
                   c.Address, c.Phone, c.Email, c.ContactPerson, c.Note
            FROM dbo.Correspondent c
            JOIN dbo.CorrespondentType t ON t.Id = c.CorrespondentTypeId
            WHERE {SqlScope.TenantFilter("c")}
            ORDER BY c.Name
            """, SqlScope.Tenant(user), ct);
}
