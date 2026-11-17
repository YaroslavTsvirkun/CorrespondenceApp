namespace CorrespondenceApi.Domain;

public class DocumentStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsFinal { get; set; }
    public string? Description { get; set; }
}
