/* Процедура: створення доручення. Доручення завжди належить резолюції —
   резолюція створюється автоматично в тій самій транзакції.
   Повертає: Id нового доручення. */
CREATE OR ALTER PROCEDURE dbo.usp_Task_Create
    @DocumentId           int,
    @ResolutionText       nvarchar(1000),
    @AuthorEmployeeId     int,
    @ExecutorEmployeeId   int,
    @TaskStatusId         int,
    @TaskText             nvarchar(1000),
    @DueDate              date,
    @CompletedAt          datetime2,
    @ResultText           nvarchar(max),
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Document
                   WHERE Id = @DocumentId AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId))
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Документ #', @DocumentId, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    DECLARE @ResolutionId int, @TaskId int;

    BEGIN TRANSACTION;

    INSERT dbo.Resolution (DocumentId, AuthorEmployeeId, ResolutionText, ResolutionDate, DueDate, IsControlled)
    VALUES (@DocumentId, ISNULL(@AuthorEmployeeId, @ExecutorEmployeeId), @ResolutionText,
            CAST(GETDATE() AS date), @DueDate, 1);
    SET @ResolutionId = CAST(SCOPE_IDENTITY() AS int);

    INSERT dbo.ExecutionTask
        (ResolutionId, DocumentId, ExecutorEmployeeId, TaskStatusId, TaskText, AssignedAt, DueDate, CompletedAt, ResultText)
    VALUES
        (@ResolutionId, @DocumentId, @ExecutorEmployeeId, @TaskStatusId, @TaskText, SYSUTCDATETIME(), @DueDate,
         @CompletedAt, @ResultText);
    SET @TaskId = CAST(SCOPE_IDENTITY() AS int);

    COMMIT TRANSACTION;

    SELECT @TaskId AS Id;
END;
GO
