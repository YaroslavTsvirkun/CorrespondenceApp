/* Процедура: видалення вкладення разом з його версіями. Повертає StorageKey поточного вмісту
   й усіх версій — застосунок видаляє ці об'єкти зі сховища після успішного видалення запису. */
CREATE OR ALTER PROCEDURE dbo.usp_Attachment_Delete
    @Id                   int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Deleted TABLE (StorageKey nvarchar(500));

    BEGIN TRANSACTION;

    -- Файли попередніх версій (рядки видаляються каскадно разом із вкладенням)
    INSERT @Deleted (StorageKey)
    SELECT v.StorageKey
    FROM dbo.AttachmentVersion v
    JOIN dbo.Attachment a ON a.Id = v.AttachmentId
    JOIN dbo.Document d ON d.Id = a.DocumentId
    WHERE a.Id = @Id AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId);

    DELETE a
    OUTPUT deleted.StorageKey INTO @Deleted
    FROM dbo.Attachment a
    JOIN dbo.Document d ON d.Id = a.DocumentId
    WHERE a.Id = @Id AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId);

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK;
        DECLARE @Message nvarchar(200) = CONCAT(N'Вкладення #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    COMMIT;

    SELECT StorageKey FROM @Deleted;
END;
GO
