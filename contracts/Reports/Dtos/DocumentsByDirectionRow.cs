namespace CorrespondenceSystem.Contracts.Reports.Dtos;

/// <summary>Звіт «Розподіл за напрямом»: напрям, кількість, частка у відсотках.</summary>
public record DocumentsByDirectionRow(string Direction, int Count, int Percent);
