using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Correspondents.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Correspondents;

/// <summary>Слайс: кореспонденти організації користувача.</summary>
public static class GetCorrespondents
{
    public record Query : IRequest<IReadOnlyList<CorrespondentDto>>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, IReadOnlyList<CorrespondentDto>>
    {
        public Task<IReadOnlyList<CorrespondentDto>> Handle(Query q, CancellationToken ct) =>
            db.QueryAsync<CorrespondentDto>($"""
                SELECT c.Id, c.Name, c.CorrespondentTypeId, t.Name AS TypeName,
                       c.Address, c.Phone, c.Email, c.ContactPerson, c.Note
                FROM dbo.Correspondent c
                JOIN dbo.CorrespondentType t ON t.Id = c.CorrespondentTypeId
                WHERE {SqlScope.TenantFilter("c")}
                ORDER BY c.Name
                """, SqlScope.Tenant(user), ct);
    }
}
