namespace CorrespondenceApi.Features.Dictionaries.Dtos;

/// <summary>
/// Поля запису простого довідника. Code заповнюється лише для document-types
/// (токен {type} у патерні реєстраційного номера).
/// </summary>
public record DictionaryItemInput(string Name, string? Description, string? Code);
