namespace CorrespondenceApi.Features.Documents.Dtos;

/// <summary>Рядок реєстру документів (список із пошуком, фільтрами та сторінками).</summary>
public record DocumentListItem(
    int Id, string RegistrationNumber, int RegistrationYear, string Direction,
    string DocumentType, string Title, string? Correspondent, DateOnly DocumentDate,
    string Status, bool IsControlled);
