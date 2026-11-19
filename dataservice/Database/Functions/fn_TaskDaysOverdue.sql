/* Скалярна функція: на скільки днів прострочене доручення.
   0 — якщо статус фінальний або строк ще не минув. */
CREATE OR ALTER FUNCTION dbo.fn_TaskDaysOverdue
(
    @DueDate      date,
    @TaskStatusId int
)
RETURNS int
AS
BEGIN
    DECLARE @today date = CAST(GETDATE() AS date);
    DECLARE @isFinal bit = ISNULL((SELECT IsFinal FROM dbo.TaskStatus WHERE Id = @TaskStatusId), 0);

    RETURN CASE WHEN @isFinal = 0 AND @DueDate < @today
                THEN DATEDIFF(day, @DueDate, @today)
                ELSE 0 END;
END;
GO
