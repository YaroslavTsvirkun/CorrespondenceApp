namespace CorrespondenceApi.Domain;

public class CorrespondentType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<Correspondent> Correspondents { get; set; } = [];
}
