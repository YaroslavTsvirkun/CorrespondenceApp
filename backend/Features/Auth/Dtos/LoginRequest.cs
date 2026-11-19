namespace CorrespondenceApi.Features.Auth.Dtos;

/// <summary>Облікові дані для входу.</summary>
public record LoginRequest(string UserName, string Password);
