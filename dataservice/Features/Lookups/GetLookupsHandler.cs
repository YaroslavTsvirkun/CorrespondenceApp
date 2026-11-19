using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Lookups;
using CorrespondenceSystem.Contracts.Lookups.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Lookups;

/// <summary>Дані слайсу: усі довідники (один запит — кілька наборів результатів).</summary>
public sealed class GetLookupsHandler(Db db, ICurrentUser user) : IRequestHandler<GetLookups.Query, LookupsDto>
{
    public async Task<LookupsDto> Handle(GetLookups.Query q, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        using var grid = await c.QueryMultipleAsync(new CommandDefinition($"""
            SELECT Id, Code, Name FROM dbo.DocumentDirection ORDER BY Id;
            SELECT Id, Name FROM dbo.DocumentType ORDER BY Name;
            SELECT Id, Name, IsFinal FROM dbo.DocumentStatus ORDER BY Id;
            SELECT Id, Name FROM dbo.DeliveryChannel ORDER BY Name;
            SELECT Id, Name FROM dbo.CorrespondentType ORDER BY Name;
            SELECT Id, Name, IsFinal FROM dbo.TaskStatus ORDER BY Id;
            SELECT d.Id, d.Name FROM dbo.Department d WHERE {SqlScope.TenantFilter("d")} ORDER BY d.Name;
            SELECT e.Id, e.FullName AS Name FROM dbo.Employee e WHERE {SqlScope.TenantFilter("e")} ORDER BY e.FullName;
            SELECT x.Id, x.Name FROM dbo.Correspondent x WHERE {SqlScope.TenantFilter("x")} ORDER BY x.Name;
            """, SqlScope.Tenant(user), cancellationToken: ct));

        return new LookupsDto(
            (await grid.ReadAsync<DirectionOption>()).AsList(),
            (await grid.ReadAsync<LookupOption>()).AsList(),
            (await grid.ReadAsync<StatusOption>()).AsList(),
            (await grid.ReadAsync<LookupOption>()).AsList(),
            (await grid.ReadAsync<LookupOption>()).AsList(),
            (await grid.ReadAsync<StatusOption>()).AsList(),
            (await grid.ReadAsync<LookupOption>()).AsList(),
            (await grid.ReadAsync<LookupOption>()).AsList(),
            (await grid.ReadAsync<LookupOption>()).AsList());
    }
}
