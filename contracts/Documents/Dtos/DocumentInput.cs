namespace CorrespondenceSystem.Contracts.Documents.Dtos;

/// <summary>
/// Поля документа для створення / редагування (і для форми редагування).
/// Імена властивостей збігаються з параметрами процедур dbo.usp_Document_Create / _Update.
/// </summary>
public record DocumentInput(
    int DirectionId, int DocumentTypeId, int CurrentStatusId,
    int? DeliveryChannelId, int? CorrespondentId, int? SenderDepartmentId,
    int? ReceiverDepartmentId, int? AuthorEmployeeId,
    string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
    bool IsControlled, string? Note,
    string? RegistrationNumber, int RegistrationYear, int RegistrarEmployeeId)
{
    /// <summary>
    /// Додаткові адресати й копії (головний кореспондент — <see cref="CorrespondentId"/>).
    /// При редагуванні null — адресатів не змінювати, порожній список — прибрати всіх.
    /// </summary>
    public IReadOnlyList<DocumentPartyInput>? Addressees { get; init; }
}

/// <summary>Додатковий адресат документа. Role: 1 — адресат, 2 — копія.</summary>
public record DocumentPartyInput(int PartyId, byte Role);
