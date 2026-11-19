/* Тригер: автоматично проставляє дату виконання доручення.
   Якщо доручення переведено у статус «Виконано», а CompletedAt не задано —
   записується поточний час. */
CREATE OR ALTER TRIGGER dbo.trg_ExecutionTask_CompletedAt
ON dbo.ExecutionTask
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    UPDATE t
    SET CompletedAt = SYSUTCDATETIME()
    FROM dbo.ExecutionTask t
    JOIN inserted i       ON i.Id = t.Id
    JOIN dbo.TaskStatus s ON s.Id = i.TaskStatusId
    WHERE i.CompletedAt IS NULL
      AND s.IsFinal = 1
      AND s.Name = N'Виконано';
END;
GO
