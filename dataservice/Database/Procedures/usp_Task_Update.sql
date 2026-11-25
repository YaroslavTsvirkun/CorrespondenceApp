/* Процедура: редагування доручення та тексту його резолюції.
   Документ доручення має бути видимим користувачу (тенант + зона видимості).
   @CanAssign = 1 (право tasks.assign) — редагуються всі поля; новий виконавець має належати організації документа.
   @CanAssign = 0 (лише tasks.execute) — лише власне доручення (виконавець = @EmployeeId) і лише хід виконання:
   статус, дата виконання, результат; решта полів (виконавець, завдання, строк, резолюція) не змінюються. */
CREATE OR ALTER PROCEDURE dbo.usp_Task_Update
    @Id                   int,
    @ResolutionText       nvarchar(1000),
    @ExecutorEmployeeId   int,
    @TaskStatusId         int,
    @TaskText             nvarchar(1000),
    @DueDate              date,
    @CompletedAt          datetime2,
    @ResultText           nvarchar(max),
    @CanAssign            bit,
    @Bypass               bit,
    @TenantOrganizationId int,
    @ScopeLevel           tinyint,
    @EmployeeId           int,
    @DepartmentId         int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ResolutionId int, @CurrentExecutorId int, @DocumentOrganizationId int;
    DECLARE @Message nvarchar(200);

    SELECT @ResolutionId = t.ResolutionId,
           @CurrentExecutorId = t.ExecutorEmployeeId,
           @DocumentOrganizationId = d.OrganizationId
    FROM dbo.ExecutionTask t
    JOIN dbo.Document d ON d.Id = t.DocumentId
    WHERE t.Id = @Id
      AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId)
      AND dbo.fn_CanReadDocument(d.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1;

    IF @ResolutionId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Доручення #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF @CanAssign = 0
    BEGIN
        IF @EmployeeId IS NULL OR @CurrentExecutorId <> @EmployeeId
            THROW 50403, N'Можна змінювати лише хід виконання власних доручень', 1;

        UPDATE dbo.ExecutionTask
        SET TaskStatusId = @TaskStatusId,
            CompletedAt  = @CompletedAt,
            ResultText   = @ResultText
        WHERE Id = @Id;
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Employee
                   WHERE Id = @ExecutorEmployeeId AND OrganizationId = @DocumentOrganizationId)
        THROW 50400, N'Виконавця не знайдено в організації документа', 1;

    BEGIN TRANSACTION;

    UPDATE dbo.ExecutionTask
    SET ExecutorEmployeeId = @ExecutorEmployeeId,
        TaskStatusId       = @TaskStatusId,
        TaskText           = @TaskText,
        DueDate            = @DueDate,
        CompletedAt        = @CompletedAt,
        ResultText         = @ResultText
    WHERE Id = @Id;

    UPDATE dbo.Resolution SET ResolutionText = @ResolutionText WHERE Id = @ResolutionId;

    COMMIT TRANSACTION;
END;
GO
