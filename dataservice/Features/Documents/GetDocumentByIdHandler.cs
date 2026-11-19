using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.DataService.Common;
using CorrespondenceSystem.DataService.Infrastructure.Database;
using Dapper;
using MediatR;

namespace CorrespondenceSystem.DataService.Features.Documents;

/// <summary>Дані слайсу: картка документа (vw_Document + доручення з vw_Task).</summary>
public sealed class GetDocumentByIdHandler(Db db, ICurrentUser user) : IRequestHandler<GetDocumentById.Query, DocumentDetail>
{
    // Рядок картки без доручень — Dapper мапить його за конструктором
    private sealed record DetailRow(
        int Id, string RegistrationNumber, int RegistrationYear, DateTime RegisteredAt,
        string Registrar, string Direction, string DocumentType, string Status, bool IsControlled,
        string? Channel, string? Correspondent, string? SenderDepartment, string? ReceiverDepartment,
        string? Author, string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
        string? Note);

    public async Task<DocumentDetail> Handle(GetDocumentById.Query q, CancellationToken ct)
    {
        var p = SqlScope.Documents(user);
        p.Add("Id", q.Id);

        var sql = $"""
            SELECT d.Id,
                   ISNULL(d.RegistrationNumber, N'') AS RegistrationNumber,
                   ISNULL(d.RegistrationYear, 0) AS RegistrationYear,
                   ISNULL(d.RegisteredAt, CAST('0001-01-01' AS datetime2)) AS RegisteredAt,
                   ISNULL(d.Registrar, N'') AS Registrar,
                   d.Direction, d.DocumentType, d.Status, d.IsControlled,
                   d.Channel, d.Correspondent, d.SenderDepartment, d.ReceiverDepartment, d.Author,
                   d.Title, d.Summary, d.DocumentDate, d.IncomingNumber, d.Note
            FROM dbo.vw_Document d
            WHERE d.Id = @Id AND {SqlScope.DocumentFilter("d")};

            SELECT t.Id, t.ExecutorName AS Executor, t.TaskText, t.DueDate, t.StatusName AS Status, t.CompletedAt
            FROM dbo.vw_Task t
            WHERE t.DocumentId = @Id
            ORDER BY t.Id;
            """;

        await using var conn = await db.OpenAsync(ct);
        using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
        var d = await grid.ReadSingleOrDefaultAsync<DetailRow>()
            ?? throw new NotFoundException($"Документ #{q.Id} не знайдено");
        var tasks = (await grid.ReadAsync<DocumentTaskItem>()).AsList();

        return new DocumentDetail(d.Id, d.RegistrationNumber, d.RegistrationYear, d.RegisteredAt, d.Registrar,
            d.Direction, d.DocumentType, d.Status, d.IsControlled, d.Channel, d.Correspondent,
            d.SenderDepartment, d.ReceiverDepartment, d.Author, d.Title, d.Summary, d.DocumentDate,
            d.IncomingNumber, d.Note, tasks);
    }
}
