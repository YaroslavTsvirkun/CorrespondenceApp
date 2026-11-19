namespace CorrespondenceApi.Features.Reports.Dtos;

/// <summary>Звіт «Документи за типами»: тип і кількість.</summary>
public record DocumentsByTypeRow(string Type, int Count);
