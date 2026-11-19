using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Auth.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Auth;

/// <summary>Слайс: вхід за логіном і паролем, видача JWT.</summary>
public static class Login
{
    public record Command(LoginRequest Request) : IRequest<LoginResponse>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Request.UserName).NotEmpty();
            RuleFor(x => x.Request.Password).NotEmpty();
        }
    }

    public class Handler(Db db, TokenService tokens) : IRequestHandler<Command, LoginResponse>
    {
        public async Task<LoginResponse> Handle(Command c, CancellationToken ct)
        {
            var access = await UserAccess.LoadAsync(db, null, c.Request.UserName, ct);

            if (access is null || !access.User.IsActive
                || !PasswordHasher.Verify(c.Request.Password, access.User.PasswordHash))
                throw new UnauthorizedException("Невірний логін або пароль");

            var u = access.User;
            return new LoginResponse(tokens.Create(u.Id, u.UserName, u.FullName), access.ToInfo());
        }
    }
}
