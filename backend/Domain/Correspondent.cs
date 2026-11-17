namespace CorrespondenceApi.Domain;

public class Correspondent
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int CorrespondentTypeId { get; set; }
    public CorrespondentType CorrespondentType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? Note { get; set; }
}
