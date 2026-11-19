using System.Data;
using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Documents.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>Слайс: попередній перегляд наступного реєстраційного номера (dbo.usp_RegistrationNumber_Next).</summary>
public static class GetNextNumber
{
    public record Query(int DirectionId, int DocumentTypeId, int Year) : IRequest<NextNumberResult>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, NextNumberResult>
    {
        public async Task<NextNumberResult> Handle(Query q, CancellationToken ct)
        {
            // Для супер-адміна (без організації) попередній номер не обчислюємо —
            // він буде згенерований на сервері від організації реєстратора.
            if (user.OrganizationId is not int orgId) return new NextNumberResult("");
            var year = q.Year is < 1990 or > 2100 ? DateTime.Now.Year : q.Year;

            var p = new DynamicParameters();
            p.Add("OrganizationId", orgId);
            p.Add("DirectionId", q.DirectionId);
            p.Add("DocumentTypeId", q.DocumentTypeId);
            p.Add("Year", year);
            p.Add("Number", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
            p.Add("Sequence", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await db.ExecuteProcAsync("dbo.usp_RegistrationNumber_Next", p, ct);
            return new NextNumberResult(p.Get<string?>("Number") ?? "");
        }
    }
}
