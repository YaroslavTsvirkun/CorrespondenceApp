/* Процедура: редагування доручення та тексту його резолюції. */
CREATE OR ALTER PROCEDURE dbo.usp_Task_Update
    @Id                   int,
    @ResolutionText       nvarchar(1000),
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

    DECLARE @ResolutionId int;

    SELECT @ResolutionId = t.ResolutionId
    FROM dbo.ExecutionTask t
    JOIN dbo.Document d ON d.Id = t.DocumentId
    WHERE t.Id = @Id AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId);

    IF @ResolutionId IS NULL
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Доручення #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

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
