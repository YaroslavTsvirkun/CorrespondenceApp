namespace CorrespondenceApi.Domain;

public class ExecutionTask
{
    public int Id { get; set; }
    public int ResolutionId { get; set; }
    public Resolution Resolution { get; set; } = null!;
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int ExecutorEmployeeId { get; set; }
    public Employee ExecutorEmployee { get; set; } = null!;
    public int TaskStatusId { get; set; }
    public TaskState TaskStatus { get; set; } = null!;
    public string TaskText { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultText { get; set; }
}
