/* Процедура: заміна вмісту вкладення (збереження з редактора або відновлення версії).
   Новий файл уже покладено у сховище під @StorageKey. Поточний вміст переноситься
   в dbo.AttachmentVersion (файл лишається у сховищі), вкладення отримує новий вміст.
   Повертає попередній StorageKey (застосунок видаляє його кешовані похідні, напр. PDF-копію). */
CREATE OR ALTER PROCEDURE dbo.usp_Attachment_ReplaceContent
    @Id                   int,
    @FileName             nvarchar(255),
    @ContentType          nvarchar(127),
    @SizeBytes            bigint,
    @Sha256               binary(32),
    @StorageKey           nvarchar(500),
    @EditorEmployeeId     int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @OldKey nvarchar(500);

    SELECT @OldKey = a.StorageKey
    FROM dbo.Attachment a WITH (UPDLOCK, HOLDLOCK)
    JOIN dbo.Document d ON d.Id = a.DocumentId
    WHERE a.Id = @Id AND (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId);

    IF @OldKey IS NULL
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Вкладення #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    INSERT dbo.AttachmentVersion (AttachmentId, VersionNo, FileName, ContentType, SizeBytes, Sha256,
                                  StorageKey, CreatedAt, CreatedByEmployeeId)
    SELECT a.Id,
           ISNULL((SELECT MAX(v.VersionNo) FROM dbo.AttachmentVersion v WHERE v.AttachmentId = a.Id), 0) + 1,
           a.FileName, a.ContentType, a.SizeBytes, a.Sha256, a.StorageKey,
           ISNULL(a.ModifiedAt, a.AddedAt), IIF(a.ModifiedAt IS NULL, a.AddedByEmployeeId, a.ModifiedByEmployeeId)
    FROM dbo.Attachment a
    WHERE a.Id = @Id;

    UPDATE dbo.Attachment
    SET FileName = @FileName, ContentType = @ContentType, SizeBytes = @SizeBytes, Sha256 = @Sha256,
        StorageKey = @StorageKey, ModifiedAt = SYSUTCDATETIME(), ModifiedByEmployeeId = @EditorEmployeeId
    WHERE Id = @Id;

    COMMIT;

    SELECT @OldKey;
END;
GO
