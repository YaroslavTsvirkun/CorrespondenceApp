namespace CorrespondenceApi.Domain;

public class StatusHistory
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int? PreviousStatusId { get; set; }
    public DocumentStatus? PreviousStatus { get; set; }
    public int NewStatusId { get; set; }
    public DocumentStatus NewStatus { get; set; } = null!;
    public int ChangedByEmployeeId { get; set; }
    public Employee ChangedByEmployee { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
}
