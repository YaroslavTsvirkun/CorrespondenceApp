using MediatR;

namespace CorrespondenceSystem.Contracts.Documents;

/// <summary>
/// Видалення документа в БД (каскадно з реєстрацією, резолюціями, дорученнями, вкладеннями) —
/// dbo.usp_Document_Delete. Повертає ключі файлів вкладень, які backend видаляє зі сховища.
/// </summary>
public static class DeleteDocumentRecord
{
    public record Command(int Id) : IRequest<IReadOnlyList<string>>;
}
