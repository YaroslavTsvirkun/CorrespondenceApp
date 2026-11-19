using System.Data;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: попередній перегляд наступного реєстраційного номера (dbo.usp_RegistrationNumber_Next).</summary>
public sealed class GetNextNumberHandler(Db db, ICurrentUser user) : IRequestHandler<GetNextNumber.Query, NextNumberResult>
{
    public async Task<NextNumberResult> Handle(GetNextNumber.Query q, CancellationToken ct)
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
