namespace CorrespondenceApi.Domain;

public class Resolution
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int AuthorEmployeeId { get; set; }
    public Employee AuthorEmployee { get; set; } = null!;
    public string ResolutionText { get; set; } = null!;
    public DateOnly ResolutionDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsControlled { get; set; }
    public List<ExecutionTask> Tasks { get; set; } = [];
}
