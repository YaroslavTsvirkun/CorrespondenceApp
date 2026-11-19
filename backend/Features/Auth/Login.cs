using CorrespondenceApi.Common;
using CorrespondenceSystem.Contracts.Auth;
using CorrespondenceSystem.Contracts.Auth.Dtos;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Auth;

/// <summary>Слайс: вхід за логіном і паролем — перевірка в сервісі даних, видача JWT.</summary>
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

    public class Handler(ISender sender, TokenService tokens) : IRequestHandler<Command, LoginResponse>
    {
        public async Task<LoginResponse> Handle(Command c, CancellationToken ct)
        {
            var user = await sender.Send(new VerifyCredentials.Query(c.Request.UserName, c.Request.Password), ct);
            return new LoginResponse(tokens.Create(user.UserId, user.UserName, user.FullName), user);
        }
    }
}
