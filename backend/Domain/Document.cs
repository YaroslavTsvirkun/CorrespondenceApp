namespace CorrespondenceApi.Domain;

public class Document
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int DirectionId { get; set; }
    public DocumentDirection Direction { get; set; } = null!;
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = null!;
    public int CurrentStatusId { get; set; }
    public DocumentStatus CurrentStatus { get; set; } = null!;
    public int? DeliveryChannelId { get; set; }
    public DeliveryChannel? DeliveryChannel { get; set; }
    public int? CorrespondentId { get; set; }
    public Correspondent? Correspondent { get; set; }
    public int? SenderDepartmentId { get; set; }
    public Department? SenderDepartment { get; set; }
    public int? ReceiverDepartmentId { get; set; }
    public Department? ReceiverDepartment { get; set; }
    public int? AuthorEmployeeId { get; set; }
    public Employee? AuthorEmployee { get; set; }
    public string Title { get; set; } = null!;
    public string? Summary { get; set; }
    public DateOnly DocumentDate { get; set; }
    public string? IncomingNumber { get; set; }
    public bool IsControlled { get; set; }
    public string? Note { get; set; }

    public RegistrationRecord? Registration { get; set; }
    public List<Resolution> Resolutions { get; set; } = [];
    public List<ExecutionTask> Tasks { get; set; } = [];
    public List<Attachment> Attachments { get; set; } = [];
    public List<StatusHistory> StatusHistory { get; set; } = [];
}
