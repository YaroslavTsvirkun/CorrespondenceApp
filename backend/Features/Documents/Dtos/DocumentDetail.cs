namespace CorrespondenceApi.Features.Documents.Dtos;

/// <summary>Картка документа: реквізити, реєстрація та доручення.</summary>
public record DocumentDetail(
    int Id, string RegistrationNumber, int RegistrationYear, DateTime RegisteredAt,
    string Registrar, string Direction, string DocumentType, string Status, bool IsControlled,
    string? Channel, string? Correspondent, string? SenderDepartment, string? ReceiverDepartment,
    string? Author, string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
    string? Note, IReadOnlyList<DocumentTaskItem> Tasks);
