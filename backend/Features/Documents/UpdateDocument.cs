using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Documents.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>Слайс: редагування документа (процедура dbo.usp_Document_Update).</summary>
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
