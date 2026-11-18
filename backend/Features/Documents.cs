using System.Data;
using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

// ---- Спільний вхідний DTO для створення/редагування ----
// Імена властивостей збігаються з параметрами процедур dbo.usp_Document_Create / _Update.
public record DocumentInput(
    int DirectionId, int DocumentTypeId, int CurrentStatusId,
    int? DeliveryChannelId, int? CorrespondentId, int? SenderDepartmentId,
    int? ReceiverDepartmentId, int? AuthorEmployeeId,
    string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
    bool IsControlled, string? Note,
    string? RegistrationNumber, int RegistrationYear, int RegistrarEmployeeId);

public class DocumentInputValidator : AbstractValidator<DocumentInput>
{
    public DocumentInputValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Вкажіть тему документа").MaximumLength(300);
        RuleFor(x => x.DirectionId).GreaterThan(0).WithMessage("Оберіть напрям");
        RuleFor(x => x.DocumentTypeId).GreaterThan(0).WithMessage("Оберіть тип документа");
        RuleFor(x => x.CurrentStatusId).GreaterThan(0).WithMessage("Оберіть статус");
        // Номер можна не вказувати — буде згенеровано автоматично за патерном організації
        RuleFor(x => x.RegistrationNumber).MaximumLength(30);
        RuleFor(x => x.RegistrationYear).InclusiveBetween(1990, 2100);
        RuleFor(x => x.RegistrarEmployeeId).GreaterThan(0).WithMessage("Оберіть реєстратора");
        RuleFor(x => x.DocumentDate).NotEqual(default(DateOnly)).WithMessage("Вкажіть дату документа");
    }
}

// ---- Ендпойнти фічі ----
public static class DocumentsEndpoints
{
    public static void MapDocuments(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/documents").WithTags("Documents");

        g.MapGet("", async (ISender m, [AsParameters] GetDocuments.Query q, CancellationToken ct) =>
            Results.Ok(await m.Send(q, ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDocumentById.Query(id), ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/{id:int}/edit", async (ISender m, int id, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetDocumentForEdit.Query(id), ct)))
            .RequireAuthorization(Permissions.DocumentsReadAny);

        g.MapGet("/next-number", async (ISender m, int directionId, int documentTypeId, int year, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetNextNumber.Query(directionId, documentTypeId, year), ct)))
            .RequireAuthorization(Permissions.DocumentsCreate);

        g.MapPost("", async (ISender m, DocumentInput input, CancellationToken ct) =>
        {
            var id = await m.Send(new CreateDocument.Command(input), ct);
            return Results.Created($"/api/documents/{id}", new { id });
        }).RequireAuthorization(Permissions.DocumentsCreate);

        g.MapPut("/{id:int}", async (ISender m, int id, DocumentInput input, CancellationToken ct) =>
        {
            await m.Send(new UpdateDocument.Command(id, input), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DocumentsUpdate);

        g.MapDelete("/{id:int}", async (ISender m, int id, CancellationToken ct) =>
        {
            await m.Send(new DeleteDocument.Command(id), ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.DocumentsDelete);
    }
}

// ---- Слайс: список документів (пошук, фільтри, сортування, сторінки) ----
public static class GetDocuments
{
    public class Query : IRequest<PagedResult<ListItem>>
    {
        public string? Search { get; set; }
        public int? DirectionId { get; set; }
        public int? DocumentTypeId { get; set; }
        public int? StatusId { get; set; }
        public bool? IsControlled { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public string? SortBy { get; set; }   // date | number | title
        public bool? Desc { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public record ListItem(
        int Id, string RegistrationNumber, int RegistrationYear, string Direction,
        string DocumentType, string Title, string? Correspondent, DateOnly DocumentDate,
        string Status, bool IsControlled);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, PagedResult<ListItem>>
    {
        public async Task<PagedResult<ListItem>> Handle(Query q, CancellationToken ct)
        {
            var p = SqlScope.Documents(user);
            var where = new List<string> { SqlScope.DocumentFilter("d") };

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                p.Add("Search", "%" + SqlText.EscapeLike(q.Search.Trim()) + "%");
                where.Add($"(d.Title LIKE @Search ESCAPE '{SqlText.LikeEscape}' " +
                          $"OR r.RegistrationNumber LIKE @Search ESCAPE '{SqlText.LikeEscape}')");
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
                "number" => $"r.RegistrationNumber {direction}",
                "title" => $"d.Title {direction}",
                _ => $"d.DocumentDate {direction}",
            };

            var page = q.Page is null or < 1 ? 1 : q.Page.Value;
            var size = q.PageSize is null or < 1 or > 200 ? 10 : q.PageSize.Value;
            p.Add("Skip", (page - 1) * size);
            p.Add("Take", size);

            var filter = string.Join(" AND ", where);
            var sql = $"""
                SELECT COUNT(*)
                FROM dbo.Document d
                LEFT JOIN dbo.RegistrationRecord r ON r.DocumentId = d.Id
                WHERE {filter};

                SELECT d.Id,
                       ISNULL(r.RegistrationNumber, N'') AS RegistrationNumber,
                       ISNULL(r.RegistrationYear, 0)     AS RegistrationYear,
                       dir.Name AS Direction, t.Name AS DocumentType, d.Title,
                       c.Name AS Correspondent, d.DocumentDate, s.Name AS Status, d.IsControlled
                FROM dbo.Document d
                LEFT JOIN dbo.RegistrationRecord r ON r.DocumentId = d.Id
                JOIN dbo.DocumentDirection dir     ON dir.Id = d.DirectionId
                JOIN dbo.DocumentType t            ON t.Id = d.DocumentTypeId
                JOIN dbo.DocumentStatus s          ON s.Id = d.CurrentStatusId
                LEFT JOIN dbo.Correspondent c      ON c.Id = d.CorrespondentId
                WHERE {filter}
                ORDER BY {orderBy}, d.Id {direction}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            await using var conn = await db.OpenAsync(ct);
            using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
            var total = await grid.ReadSingleAsync<int>();
            var items = (await grid.ReadAsync<ListItem>()).AsList();
            return new PagedResult<ListItem>(items, total, page, size);
        }
    }
}

// ---- Слайс: картка документа ----
public static class GetDocumentById
{
    public record Query(int Id) : IRequest<Detail>;

    public record TaskItem(int Id, string Executor, string TaskText, DateOnly DueDate,
        string Status, DateTime? CompletedAt);

    public record Detail(
        int Id, string RegistrationNumber, int RegistrationYear, DateTime RegisteredAt,
        string Registrar, string Direction, string DocumentType, string Status, bool IsControlled,
        string? Channel, string? Correspondent, string? SenderDepartment, string? ReceiverDepartment,
        string? Author, string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
        string? Note, IReadOnlyList<TaskItem> Tasks);

    private sealed record DetailRow(
        int Id, string RegistrationNumber, int RegistrationYear, DateTime RegisteredAt,
        string Registrar, string Direction, string DocumentType, string Status, bool IsControlled,
        string? Channel, string? Correspondent, string? SenderDepartment, string? ReceiverDepartment,
        string? Author, string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
        string? Note);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, Detail>
    {
        public async Task<Detail> Handle(Query q, CancellationToken ct)
        {
            var p = SqlScope.Documents(user);
            p.Add("Id", q.Id);

            var sql = $"""
                SELECT d.Id,
                       ISNULL(r.RegistrationNumber, N'') AS RegistrationNumber,
                       ISNULL(r.RegistrationYear, 0) AS RegistrationYear,
                       ISNULL(r.RegisteredAt, CAST('0001-01-01' AS datetime2)) AS RegisteredAt,
                       ISNULL(reg.FullName, N'') AS Registrar,
                       dir.Name AS Direction, t.Name AS DocumentType, s.Name AS Status, d.IsControlled,
                       ch.Name AS Channel, c.Name AS Correspondent,
                       sd.Name AS SenderDepartment, rd.Name AS ReceiverDepartment, a.FullName AS Author,
                       d.Title, d.Summary, d.DocumentDate, d.IncomingNumber, d.Note
                FROM dbo.Document d
                LEFT JOIN dbo.RegistrationRecord r ON r.DocumentId = d.Id
                LEFT JOIN dbo.Employee reg         ON reg.Id = r.RegistrarEmployeeId
                JOIN dbo.DocumentDirection dir     ON dir.Id = d.DirectionId
                JOIN dbo.DocumentType t            ON t.Id = d.DocumentTypeId
                JOIN dbo.DocumentStatus s          ON s.Id = d.CurrentStatusId
                LEFT JOIN dbo.DeliveryChannel ch   ON ch.Id = d.DeliveryChannelId
                LEFT JOIN dbo.Correspondent c      ON c.Id = d.CorrespondentId
                LEFT JOIN dbo.Department sd        ON sd.Id = d.SenderDepartmentId
                LEFT JOIN dbo.Department rd        ON rd.Id = d.ReceiverDepartmentId
                LEFT JOIN dbo.Employee a           ON a.Id = d.AuthorEmployeeId
                WHERE d.Id = @Id AND {SqlScope.DocumentFilter("d")};

                SELECT t.Id, e.FullName AS Executor, t.TaskText, t.DueDate, s.Name AS Status, t.CompletedAt
                FROM dbo.ExecutionTask t
                JOIN dbo.Employee e   ON e.Id = t.ExecutorEmployeeId
                JOIN dbo.TaskStatus s ON s.Id = t.TaskStatusId
                WHERE t.DocumentId = @Id
                ORDER BY t.Id;
                """;

            await using var conn = await db.OpenAsync(ct);
            using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
            var d = await grid.ReadSingleOrDefaultAsync<DetailRow>()
                ?? throw new NotFoundException($"Документ #{q.Id} не знайдено");
            var tasks = (await grid.ReadAsync<TaskItem>()).AsList();

            return new Detail(d.Id, d.RegistrationNumber, d.RegistrationYear, d.RegisteredAt, d.Registrar,
                d.Direction, d.DocumentType, d.Status, d.IsControlled, d.Channel, d.Correspondent,
                d.SenderDepartment, d.ReceiverDepartment, d.Author, d.Title, d.Summary, d.DocumentDate,
                d.IncomingNumber, d.Note, tasks);
        }
    }
}

// ---- Слайс: сирі поля документа для форми редагування ----
public static class GetDocumentForEdit
{
    public record Query(int Id) : IRequest<DocumentInput>;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, DocumentInput>
    {
        public async Task<DocumentInput> Handle(Query q, CancellationToken ct)
        {
            var p = SqlScope.Documents(user);
            p.Add("Id", q.Id);
            p.Add("CurrentYear", DateTime.Now.Year);

            var d = await db.QuerySingleOrDefaultAsync<DocumentInput>($"""
                SELECT d.DirectionId, d.DocumentTypeId, d.CurrentStatusId,
                       d.DeliveryChannelId, d.CorrespondentId, d.SenderDepartmentId,
                       d.ReceiverDepartmentId, d.AuthorEmployeeId,
                       d.Title, d.Summary, d.DocumentDate, d.IncomingNumber, d.IsControlled, d.Note,
                       ISNULL(r.RegistrationNumber, N'')        AS RegistrationNumber,
                       ISNULL(r.RegistrationYear, @CurrentYear) AS RegistrationYear,
                       ISNULL(r.RegistrarEmployeeId, 0)         AS RegistrarEmployeeId
                FROM dbo.Document d
                LEFT JOIN dbo.RegistrationRecord r ON r.DocumentId = d.Id
                WHERE d.Id = @Id AND {SqlScope.DocumentFilter("d")}
                """, p, ct);
            return d ?? throw new NotFoundException($"Документ #{q.Id} не знайдено");
        }
    }
}

// ---- Слайс: попередній перегляд наступного реєстраційного номера ----
public static class GetNextNumber
{
    public record Query(int DirectionId, int DocumentTypeId, int Year) : IRequest<Result>;
    public record Result(string Number);

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Query, Result>
    {
        public async Task<Result> Handle(Query q, CancellationToken ct)
        {
            // Для супер-адміна (без організації) попередній номер не обчислюємо —
            // він буде згенерований на сервері від організації реєстратора.
            if (user.OrganizationId is not int orgId) return new Result("");
            var year = q.Year is < 1990 or > 2100 ? DateTime.Now.Year : q.Year;

            var p = new DynamicParameters();
            p.Add("OrganizationId", orgId);
            p.Add("DirectionId", q.DirectionId);
            p.Add("DocumentTypeId", q.DocumentTypeId);
            p.Add("Year", year);
            p.Add("Number", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
            p.Add("Sequence", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await db.ExecuteProcAsync("dbo.usp_RegistrationNumber_Next", p, ct);
            return new Result(p.Get<string?>("Number") ?? "");
        }
    }
}

// ---- Слайс: створення документа (процедура dbo.usp_Document_Create) ----
public static class CreateDocument
{
    public record Command(DocumentInput Input) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new DocumentInputValidator());
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            // Організація документа = організація поточного користувача
            // (для супер-адміна процедура візьме її від реєстратора).
            var p = new DynamicParameters(c.Input);
            p.Add("OrganizationId", user.OrganizationId);
            return await db.ScalarProcAsync<int>("dbo.usp_Document_Create", p, ct);
        }
    }
}

// ---- Слайс: редагування документа (процедура dbo.usp_Document_Update) ----
public static class UpdateDocument
{
    public record Command(int Id, DocumentInput Input) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new DocumentInputValidator());
    }

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.AddDynamicParams(c.Input);
            p.Add("Id", c.Id);
            await db.ExecuteProcAsync("dbo.usp_Document_Update", p, ct);
        }
    }
}

// ---- Слайс: видалення документа (каскадно з обліковими записами) ----
public static class DeleteDocument
{
    public record Command(int Id) : IRequest;

    public class Handler(Db db, ICurrentUser user) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var p = SqlScope.Tenant(user);
            p.Add("Id", c.Id);
            await db.ExecuteProcAsync("dbo.usp_Document_Delete", p, ct);
        }
    }
}
