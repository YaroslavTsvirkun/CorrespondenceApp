namespace CorrespondenceApi.Features.Lookups.Dtos;

/// <summary>Статус документа або доручення; IsFinal — завершальний.</summary>
public record StatusOption(int Id, string Name, bool IsFinal);
