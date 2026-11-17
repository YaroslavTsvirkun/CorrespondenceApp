namespace CorrespondenceApi.Domain;

public class RegistrationRecord
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int RegistrarEmployeeId { get; set; }
    public Employee RegistrarEmployee { get; set; } = null!;
    public string RegistrationNumber { get; set; } = null!;
    public int RegistrationYear { get; set; }
    // Організація реєстрації (денормалізовано з документа) — для унікальності номера в межах організації
    public int OrganizationId { get; set; }
    // Числове значення лічильника (за типом+роком); null = номер уведено вручну
    public int? SequenceNumber { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string? Comment { get; set; }
}
