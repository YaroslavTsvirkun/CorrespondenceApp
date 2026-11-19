using CorrespondenceSystem.Contracts.Auth.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Auth;

/// <summary>
/// Перевірка логіна й пароля в БД. Хеш пароля не залишає сервіс даних;
/// у разі успіху повертаються дані користувача (JWT видає backend).
/// </summary>
public static class VerifyCredentials
{
    public record Query(string UserName, string Password) : IRequest<UserInfo>;
}
