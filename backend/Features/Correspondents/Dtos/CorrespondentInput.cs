namespace CorrespondenceApi.Features.Correspondents.Dtos;

/// <summary>Поля кореспондента для створення / редагування.</summary>
public record CorrespondentInput(int CorrespondentTypeId, string Name, string? Address,
    string? Phone, string? Email, string? ContactPerson, string? Note);
