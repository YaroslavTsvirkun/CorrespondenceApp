/* Процедура: видалення документа. Реєстраційний запис, резолюції, доручення,
   вкладення та історія статусів видаляються каскадно (ON DELETE CASCADE).
   Повертає StorageKey вкладень — застосунок видаляє ці об'єкти зі сховища файлів. */
CREATE OR ALTER PROCEDURE dbo.usp_Document_Delete
    @Id                   int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Document
                   WHERE Id = @Id AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId))
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Документ #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    DECLARE @Keys TABLE (StorageKey nvarchar(500));
    INSERT @Keys (StorageKey) SELECT StorageKey FROM dbo.Attachment WHERE DocumentId = @Id;
    -- Файли попередніх версій вкладень (рядки видаляються каскадно)
    INSERT @Keys (StorageKey)
    SELECT v.StorageKey
    FROM dbo.AttachmentVersion v
    JOIN dbo.Attachment a ON a.Id = v.AttachmentId
    WHERE a.DocumentId = @Id;

    DELETE dbo.Document WHERE Id = @Id;

    SELECT StorageKey FROM @Keys;
END;
GO
