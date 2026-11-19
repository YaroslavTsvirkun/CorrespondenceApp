namespace CorrespondenceSystem.Contracts.Tasks.Dtos;

/// <summary>Id створених доручень у порядку рядків запиту.</summary>
public record TaskBatchResult(IReadOnlyList<int> Ids);
