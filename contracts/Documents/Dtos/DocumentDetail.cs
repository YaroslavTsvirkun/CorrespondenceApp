namespace CorrespondenceSystem.Contracts.Documents.Dtos;

/// <summary>
/// Картка документа: реквізити, реєстрація та доручення.
/// Correspondent — назва кореспондента на момент реєстрації; CorrespondentCurrentName — поточна назва
/// контрагента, якщо вона відтоді змінилася.
/// </summary>
public record DocumentDetail(
    int Id, string RegistrationNumber, int RegistrationYear, DateTime RegisteredAt,
    string Registrar, string Direction, string DocumentType, string Status, bool IsControlled,
    string? Channel, string? Correspondent, string? SenderDepartment, string? ReceiverDepartment,
    string? Author, string Title, string? Summary, DateOnly DocumentDate, string? IncomingNumber,
    string? Note, IReadOnlyList<DocumentTaskItem> Tasks)
{
    public string? CorrespondentCurrentName { get; init; }
    public IReadOnlyList<DocumentPartyItem> Addressees { get; init; } = [];
}

/// <summary>Додатковий адресат у картці: назва на момент додавання й поточна (якщо змінилася).</summary>
public record DocumentPartyItem(int PartyId, byte Role, string Name, string? CurrentName);
