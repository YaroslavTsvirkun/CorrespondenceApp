using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Auth.Dtos;
using CorrespondenceApi.Infrastructure.Database;
using MediatR;

namespace CorrespondenceApi.Features.Auth;

/// <summary>Слайс: дані поточного користувача.</summary>
public static class Me
{
    public record Query : IRequest<UserInfo>;

    public class Handler(Db db, ICurrentUser current) : IRequestHandler<Query, UserInfo>
    {
        public async Task<UserInfo> Handle(Query q, CancellationToken ct)
        {
            var access = current.UserId is int id ? await UserAccess.LoadAsync(db, id, null, ct) : null;
            return access?.ToInfo() ?? throw new UnauthorizedException("Користувача не знайдено");
        }
    }
}
