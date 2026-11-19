namespace CorrespondenceSystem.Contracts.Correspondents.Dtos;

/// <summary>Кореспондент із назвою типу.</summary>
public record CorrespondentDto(int Id, string Name, int CorrespondentTypeId, string TypeName,
    string? Address, string? Phone, string? Email, string? ContactPerson, string? Note);
