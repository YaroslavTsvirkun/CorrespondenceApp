using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using CorrespondenceApi.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Features.Documents;

// ---- Спільний вхідний DTO для створення/редагування ----
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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, PagedResult<ListItem>>
    {
        public async Task<PagedResult<ListItem>> Handle(Query q, CancellationToken ct)
        {
            var query = DocumentScope.ApplyRead(db.Documents.AsNoTracking(), user);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(d => d.Title.Contains(s)
                    || (d.Registration != null && d.Registration.RegistrationNumber.Contains(s)));
            }
            if (q.DirectionId is int dir) query = query.Where(d => d.DirectionId == dir);
            if (q.DocumentTypeId is int dt) query = query.Where(d => d.DocumentTypeId == dt);
            if (q.StatusId is int st) query = query.Where(d => d.CurrentStatusId == st);
            if (q.IsControlled is bool c) query = query.Where(d => d.IsControlled == c);
            if (q.DateFrom is DateOnly from) query = query.Where(d => d.DocumentDate >= from);
            if (q.DateTo is DateOnly to) query = query.Where(d => d.DocumentDate <= to);

            var desc = q.Desc ?? true;
            query = (q.SortBy, desc) switch
            {
                ("number", true) => query.OrderByDescending(d => d.Registration!.RegistrationNumber),
                ("number", false) => query.OrderBy(d => d.Registration!.RegistrationNumber),
                ("title", true) => query.OrderByDescending(d => d.Title),
                ("title", false) => query.OrderBy(d => d.Title),
                (_, false) => query.OrderBy(d => d.DocumentDate),
                _ => query.OrderByDescending(d => d.DocumentDate),
            };

            var total = await query.CountAsync(ct);
            var page = q.Page is null or < 1 ? 1 : q.Page.Value;
            var size = q.PageSize is null or < 1 or > 200 ? 10 : q.PageSize.Value;

            var items = await query
                .Skip((page - 1) * size).Take(size)
                .Select(d => new ListItem(
                    d.Id,
                    d.Registration != null ? d.Registration.RegistrationNumber : "",
                    d.Registration != null ? d.Registration.RegistrationYear : 0,
                    d.Direction.Name,
                    d.DocumentType.Name,
                    d.Title,
                    d.Correspondent != null ? d.Correspondent.Name : null,
                    d.DocumentDate,
                    d.CurrentStatus.Name,
                    d.IsControlled))
                .ToListAsync(ct);

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

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, Detail>
    {
        public async Task<Detail> Handle(Query q, CancellationToken ct)
        {
            var d = await DocumentScope.ApplyRead(db.Documents.AsNoTracking(), user)
                .Where(x => x.Id == q.Id)
                .Select(x => new Detail(
                    x.Id,
                    x.Registration != null ? x.Registration.RegistrationNumber : "",
                    x.Registration != null ? x.Registration.RegistrationYear : 0,
                    x.Registration != null ? x.Registration.RegisteredAt : default,
                    x.Registration != null ? x.Registration.RegistrarEmployee.FullName : "",
                    x.Direction.Name, x.DocumentType.Name, x.CurrentStatus.Name, x.IsControlled,
                    x.DeliveryChannel != null ? x.DeliveryChannel.Name : null,
                    x.Correspondent != null ? x.Correspondent.Name : null,
                    x.SenderDepartment != null ? x.SenderDepartment.Name : null,
                    x.ReceiverDepartment != null ? x.ReceiverDepartment.Name : null,
                    x.AuthorEmployee != null ? x.AuthorEmployee.FullName : null,
                    x.Title, x.Summary, x.DocumentDate, x.IncomingNumber, x.Note,
                    x.Tasks.Select(t => new TaskItem(
                        t.Id, t.ExecutorEmployee.FullName, t.TaskText, t.DueDate,
                        t.TaskStatus.Name, t.CompletedAt)).ToList()))
                .FirstOrDefaultAsync(ct);

            return d ?? throw new NotFoundException($"Документ #{q.Id} не знайдено");
        }
    }
}

// ---- Слайс: сирі поля документа для форми редагування ----
public static class GetDocumentForEdit
{
    public record Query(int Id) : IRequest<DocumentInput>;

    public class Handler(AppDbContext db, ICurrentUser user) : IRequestHandler<Query, DocumentInput>
    {
        public async Task<DocumentInput> Handle(Query q, CancellationToken ct)
        {
            var d = await DocumentScope.ApplyRead(db.Documents.AsNoTracking(), user)
                .Where(x => x.Id == q.Id)
                .Select(x => new DocumentInput(
                    x.DirectionId, x.DocumentTypeId, x.CurrentStatusId,
                    x.DeliveryChannelId, x.CorrespondentId, x.SenderDepartmentId,
                    x.ReceiverDepartmentId, x.AuthorEmployeeId,
                    x.Title, x.Summary, x.DocumentDate, x.IncomingNumber, x.IsControlled, x.Note,
                    x.Registration != null ? x.Registration.RegistrationNumber : "",
                    x.Registration != null ? x.Registration.RegistrationYear : DateTime.Now.Year,
                    x.Registration != null ? x.Registration.RegistrarEmployeeId : 0))
                .FirstOrDefaultAsync(ct);
            return d ?? throw new NotFoundException($"Документ #{q.Id} не знайдено");
        }
    }
}

// ---- Слайс: попередній перегляд наступного реєстраційного номера ----
public static class GetNextNumber
{
    public record Query(int DirectionId, int DocumentTypeId, int Year) : IRequest<Result>;
    public record Result(string Number);

    public class Handler(RegistrationNumberService numbers, ICurrentUser user)
        : IRequestHandler<Query, Result>
    {
        public async Task<Result> Handle(Query q, CancellationToken ct)
        {
            // Для супер-адміна (без організації) попередній номер не обчислюємо —
            // він буде згенерований на сервері від організації реєстратора.
            if (user.OrganizationId is not int orgId) return new Result("");
            var year = q.Year is < 1990 or > 2100 ? DateTime.Now.Year : q.Year;
            var (number, _) = await numbers.NextAsync(orgId, q.DirectionId, q.DocumentTypeId, year, ct);
            return new Result(number);
        }
    }
}

// ---- Слайс: створення документа (разом із реєстраційним записом) ----
public static class CreateDocument
{
    public record Command(DocumentInput Input) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new DocumentInputValidator());
    }

    public class Handler(AppDbContext db, ICurrentUser user, RegistrationNumberService numbers)
        : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            var i = c.Input;
            // Організація документа = організація поточного користувача
            // (для супер-адміна — береться від реєстратора).
            var organizationId = user.OrganizationId
                ?? await db.Employees.Where(e => e.Id == i.RegistrarEmployeeId)
                    .Select(e => (int?)e.OrganizationId).FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("Не вдалося визначити організацію документа");

            // Реєстраційний номер: ручний (якщо вказано) або автогенерація за патерном організації
            string registrationNumber;
            int? sequence;
            if (!string.IsNullOrWhiteSpace(i.RegistrationNumber))
            {
                registrationNumber = i.RegistrationNumber.Trim();
                sequence = null;
                var taken = await db.Documents.IgnoreQueryFilters().AnyAsync(d =>
                    d.OrganizationId == organizationId && d.Registration != null
                    && d.Registration.RegistrationYear == i.RegistrationYear
                    && d.Registration.RegistrationNumber == registrationNumber, ct);
                if (taken)
                    throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                        nameof(DocumentInput.RegistrationNumber),
                        $"Реєстраційний номер «{registrationNumber}» уже використано в {i.RegistrationYear} році")]);
            }
            else
            {
                (registrationNumber, var seq) = await numbers.NextAsync(
                    organizationId, i.DirectionId, i.DocumentTypeId, i.RegistrationYear, ct);
                sequence = seq;
            }

            var doc = new Document
            {
                OrganizationId = organizationId,
                DirectionId = i.DirectionId,
                DocumentTypeId = i.DocumentTypeId,
                CurrentStatusId = i.CurrentStatusId,
                DeliveryChannelId = i.DeliveryChannelId,
                CorrespondentId = i.CorrespondentId,
                SenderDepartmentId = i.SenderDepartmentId,
                ReceiverDepartmentId = i.ReceiverDepartmentId,
                AuthorEmployeeId = i.AuthorEmployeeId,
                Title = i.Title,
                Summary = i.Summary,
                DocumentDate = i.DocumentDate,
                IncomingNumber = i.IncomingNumber,
                IsControlled = i.IsControlled,
                Note = i.Note,
                Registration = new RegistrationRecord
                {
                    OrganizationId = organizationId,
                    RegistrationNumber = registrationNumber,
                    SequenceNumber = sequence,
                    RegistrationYear = i.RegistrationYear,
                    RegistrarEmployeeId = i.RegistrarEmployeeId,
                    RegisteredAt = DateTime.UtcNow,
                },
            };
            db.Documents.Add(doc);
            await db.SaveChangesAsync(ct);
            return doc.Id;
        }
    }
}

// ---- Слайс: редагування документа ----
public static class UpdateDocument
{
    public record Command(int Id, DocumentInput Input) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Input).SetValidator(new DocumentInputValidator());
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var doc = await db.Documents.Include(d => d.Registration)
                .FirstOrDefaultAsync(d => d.Id == c.Id, ct)
                ?? throw new NotFoundException($"Документ #{c.Id} не знайдено");

            var i = c.Input;
            doc.DirectionId = i.DirectionId;
            doc.DocumentTypeId = i.DocumentTypeId;
            doc.CurrentStatusId = i.CurrentStatusId;
            doc.DeliveryChannelId = i.DeliveryChannelId;
            doc.CorrespondentId = i.CorrespondentId;
            doc.SenderDepartmentId = i.SenderDepartmentId;
            doc.ReceiverDepartmentId = i.ReceiverDepartmentId;
            doc.AuthorEmployeeId = i.AuthorEmployeeId;
            doc.Title = i.Title;
            doc.Summary = i.Summary;
            doc.DocumentDate = i.DocumentDate;
            doc.IncomingNumber = i.IncomingNumber;
            doc.IsControlled = i.IsControlled;
            doc.Note = i.Note;

            doc.Registration ??= new RegistrationRecord();
            doc.Registration.OrganizationId = doc.OrganizationId;
            if (!string.IsNullOrWhiteSpace(i.RegistrationNumber))
                doc.Registration.RegistrationNumber = i.RegistrationNumber.Trim();
            doc.Registration.RegistrationYear = i.RegistrationYear;
            doc.Registration.RegistrarEmployeeId = i.RegistrarEmployeeId;

            await db.SaveChangesAsync(ct);
        }
    }
}

// ---- Слайс: видалення документа (каскадно з обліковими записами) ----
public static class DeleteDocument
{
    public record Command(int Id) : IRequest;

    public class Handler(AppDbContext db) : IRequestHandler<Command>
    {
        public async Task Handle(Command c, CancellationToken ct)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == c.Id, ct)
                ?? throw new NotFoundException($"Документ #{c.Id} не знайдено");
            db.Documents.Remove(doc);
            await db.SaveChangesAsync(ct);
        }
    }
}
