namespace CorrespondenceSystem.Contracts.Lookups.Dtos;

/// <summary>Усі довідники для випадних списків інтерфейсу.</summary>
public record LookupsDto(
    IReadOnlyList<DirectionOption> Directions,
    IReadOnlyList<LookupOption> DocumentTypes,
    IReadOnlyList<StatusOption> DocumentStatuses,
    IReadOnlyList<LookupOption> Channels,
    IReadOnlyList<LookupOption> CorrespondentTypes,
    IReadOnlyList<StatusOption> TaskStatuses,
    IReadOnlyList<LookupOption> Departments,
    IReadOnlyList<LookupOption> Employees,
    IReadOnlyList<LookupOption> Correspondents);
