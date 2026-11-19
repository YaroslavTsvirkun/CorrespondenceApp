/* Процедура: видалення вкладення. Повертає StorageKey — застосунок видаляє об'єкт зі сховища
   після успішного видалення запису. */
CREATE OR ALTER PROCEDURE dbo.usp_Attachment_Delete
    @Id                   int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Deleted TABLE (StorageKey nvarchar(500));

    DELETE a
    OUTPUT deleted.StorageKey INTO @Deleted
    FROM dbo.Attachment a
    JOIN dbo.Document d ON d.Id = a.DocumentId
    WHERE a.Id = @Id AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId);

    IF NOT EXISTS (SELECT 1 FROM @Deleted)
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Вкладення #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    SELECT StorageKey FROM @Deleted;
END;
GO
