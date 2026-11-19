/* Тригер: автоматичне ведення історії статусів документа.
   На створення документа і на кожну зміну CurrentStatusId додає запис у dbo.StatusHistory.
   «Хто змінив» — працівник поточного користувача, якого застосунок передає через
   SESSION_CONTEXT(N'EmployeeId'); якщо його немає — автор документа. */
CREATE OR ALTER TRIGGER dbo.trg_Document_StatusHistory
ON dbo.Document
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    IF EXISTS (SELECT 1 FROM deleted) AND NOT UPDATE(CurrentStatusId) RETURN;

    DECLARE @SessionEmployeeId int = TRY_CAST(SESSION_CONTEXT(N'EmployeeId') AS int);

    INSERT dbo.StatusHistory (DocumentId, PreviousStatusId, NewStatusId, ChangedByEmployeeId, ChangedAt, Comment)
    SELECT i.Id,
           d.CurrentStatusId,
           i.CurrentStatusId,
           COALESCE(@SessionEmployeeId, i.AuthorEmployeeId),
           SYSUTCDATETIME(),
           CASE WHEN d.Id IS NULL THEN N'Документ зареєстровано' ELSE N'Зміна статусу' END
    FROM inserted i
    LEFT JOIN deleted d ON d.Id = i.Id
    WHERE (d.Id IS NULL OR d.CurrentStatusId <> i.CurrentStatusId)
      AND COALESCE(@SessionEmployeeId, i.AuthorEmployeeId) IS NOT NULL;
END;
GO
