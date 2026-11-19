namespace CorrespondenceSystem.Contracts.Dictionaries.Dtos;

/// <summary>Запис простого довідника (Code — лише для типів документів, інакше null).</summary>
public record DictionaryItemDto(int Id, string Name, string? Description, string? Code);
