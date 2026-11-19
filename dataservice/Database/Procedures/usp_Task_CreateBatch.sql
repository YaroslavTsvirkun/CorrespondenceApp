/* Процедура: одна резолюція + кілька доручень (різним виконавцям) в одній транзакції.
   @Tasks — табличний параметр dbo.TaskAssignmentList.
   Повертає: Id створених доручень у порядку рядків @Tasks (RowNo). */
CREATE OR ALTER PROCEDURE dbo.usp_Task_CreateBatch
    @DocumentId           int,
    @ResolutionText       nvarchar(1000),
    @ResolutionDueDate    date,
    @AuthorEmployeeId     int,
    @Tasks                dbo.TaskAssignmentList READONLY,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message nvarchar(200);

    IF NOT EXISTS (SELECT 1 FROM dbo.Document
                   WHERE Id = @DocumentId AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId))
    BEGIN
        SET @Message = CONCAT(N'Документ #', @DocumentId, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM @Tasks)
        THROW 50400, N'Додайте хоча б одне доручення', 1;

    -- Виконавці мають належати організації документа
    IF EXISTS (SELECT 1
               FROM @Tasks x
               JOIN dbo.Document d ON d.Id = @DocumentId
               LEFT JOIN dbo.Employee e ON e.Id = x.ExecutorEmployeeId AND e.OrganizationId = d.OrganizationId
               WHERE e.Id IS NULL)
        THROW 50400, N'Деяких виконавців не знайдено в організації документа', 1;

    DECLARE @ResolutionId int;
    DECLARE @Created TABLE (RowNo int NOT NULL, Id int NOT NULL);

    BEGIN TRANSACTION;

    INSERT dbo.Resolution (DocumentId, AuthorEmployeeId, ResolutionText, ResolutionDate, DueDate, IsControlled)
    VALUES (@DocumentId,
            ISNULL(@AuthorEmployeeId, (SELECT TOP (1) ExecutorEmployeeId FROM @Tasks ORDER BY RowNo)),
            @ResolutionText, CAST(GETDATE() AS date),
            ISNULL(@ResolutionDueDate, (SELECT MAX(DueDate) FROM @Tasks)), 1);
    SET @ResolutionId = CAST(SCOPE_IDENTITY() AS int);

    -- MERGE (а не INSERT) — щоб у OUTPUT був доступний RowNo з джерела
    MERGE dbo.ExecutionTask AS target
    USING @Tasks AS src ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (ResolutionId, DocumentId, ExecutorEmployeeId, TaskStatusId, TaskText, AssignedAt, DueDate)
        VALUES (@ResolutionId, @DocumentId, src.ExecutorEmployeeId, src.TaskStatusId, src.TaskText,
                SYSUTCDATETIME(), src.DueDate)
    OUTPUT src.RowNo, inserted.Id INTO @Created (RowNo, Id);

    COMMIT TRANSACTION;

    SELECT Id FROM @Created ORDER BY RowNo;
END;
GO
