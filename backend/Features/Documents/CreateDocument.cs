using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Documents.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using Dapper;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Documents;

/// <summary>Слайс: реєстрація документа (процедура dbo.usp_Document_Create).</summary>
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
