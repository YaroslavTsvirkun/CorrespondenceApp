using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: реєстр документів (пошук, фільтри, сортування, сторінки).</summary>
public sealed class GetDocumentsHandler(Db db, ICurrentUser user) : IRequestHandler<GetDocuments.Query, PagedResult<DocumentListItem>>
{
    public async Task<PagedResult<DocumentListItem>> Handle(GetDocuments.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        var where = new List<string> { "1 = 1" };

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            p.Add("Search", "%" + SqlText.EscapeLike(q.Search.Trim()) + "%");
            where.Add($"(d.Title LIKE @Search ESCAPE '{SqlText.LikeEscape}' " +
                      $"OR d.RegistrationNumber LIKE @Search ESCAPE '{SqlText.LikeEscape}')");
        }
        if (q.DirectionId is int dir) { p.Add("DirectionId", dir); where.Add("d.DirectionId = @DirectionId"); }
        if (q.DocumentTypeId is int dt) { p.Add("DocumentTypeId", dt); where.Add("d.DocumentTypeId = @DocumentTypeId"); }
        if (q.StatusId is int st) { p.Add("StatusId", st); where.Add("d.CurrentStatusId = @StatusId"); }
        if (q.IsControlled is bool c) { p.Add("IsControlled", c); where.Add("d.IsControlled = @IsControlled"); }
        if (q.DateFrom is DateOnly dateFrom) { p.Add("DateFrom", dateFrom); where.Add("d.DocumentDate >= @DateFrom"); }
        if (q.DateTo is DateOnly dateTo) { p.Add("DateTo", dateTo); where.Add("d.DocumentDate <= @DateTo"); }

        var direction = q.Desc ?? true ? "DESC" : "ASC";
        var orderBy = q.SortBy switch
        {
            "number" => $"d.RegistrationNumber {direction}",
            "title" => $"d.Title {direction}",
            _ => $"d.DocumentDate {direction}",
        };

        var page = q.Page is null or < 1 ? 1 : q.Page.Value;
        var size = q.PageSize is null or < 1 or > 200 ? 10 : q.PageSize.Value;
        p.Add("Skip", (page - 1) * size);
        p.Add("Take", size);

        // Представлення dbo.vw_Document + таблична функція видимості dbo.fn_VisibleDocuments
        var from = $"""
            FROM dbo.vw_Document d
            JOIN {SqlScope.VisibleDocuments} v ON v.Id = d.Id
            WHERE {string.Join(" AND ", where)}
            """;
        var sql = $"""
            SELECT COUNT(*) {from};

            SELECT d.Id,
                   ISNULL(d.RegistrationNumber, N'') AS RegistrationNumber,
                   ISNULL(d.RegistrationYear, 0)     AS RegistrationYear,
                   d.Direction, d.DocumentType, d.Title,
                   d.Correspondent, d.DocumentDate, d.Status, d.IsControlled
            {from}
            ORDER BY {orderBy}, d.Id {direction}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        await using var conn = await db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
        var total = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<DocumentListItem>()).AsList();
        return new PagedResult<DocumentListItem>(items, total, page, size);
    }
}
