/* Процедура: видалення доручення. */
CREATE OR ALTER PROCEDURE dbo.usp_Task_Delete
    @Id                   int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1
                   FROM dbo.ExecutionTask t
                   JOIN dbo.Document d ON d.Id = t.DocumentId
                   WHERE t.Id = @Id AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId))
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Доручення #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    DELETE dbo.ExecutionTask WHERE Id = @Id;
END;
GO
