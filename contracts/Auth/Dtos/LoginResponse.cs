namespace CorrespondenceSystem.Contracts.Auth.Dtos;

/// <summary>Результат входу: JWT і дані користувача.</summary>
public record LoginResponse(string Token, UserInfo User);
