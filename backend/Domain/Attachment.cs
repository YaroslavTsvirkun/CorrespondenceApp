namespace CorrespondenceApi.Domain;

public class Attachment
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int? AddedByEmployeeId { get; set; }
    public Employee? AddedByEmployee { get; set; }
    public string FileName { get; set; } = null!;
    public string? FileType { get; set; }
    public string FilePath { get; set; } = null!;
    public DateTime AddedAt { get; set; }
    public string? Description { get; set; }
}
