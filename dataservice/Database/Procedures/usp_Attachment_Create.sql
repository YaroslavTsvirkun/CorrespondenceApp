/* Процедура: реєстрація вкладення документа (файл уже завантажено у сховище під @StorageKey).
   Документ має належати організації користувача (@Bypass = 1 — супер-адмін).
   Повертає Id нового вкладення. */
CREATE OR ALTER PROCEDURE dbo.usp_Attachment_Create
    @DocumentId           int,
    @FileName             nvarchar(255),
    @ContentType          nvarchar(127),
    @SizeBytes            bigint,
    @Sha256               binary(32),
    @StorageKey           nvarchar(500),
    @Description          nvarchar(max),
    @AddedByEmployeeId    int,
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

    INSERT dbo.Attachment (DocumentId, AddedByEmployeeId, FileName, ContentType, SizeBytes, Sha256,
                           StorageKey, Description)
    VALUES (@DocumentId, @AddedByEmployeeId, @FileName, @ContentType, @SizeBytes, @Sha256,
            @StorageKey, NULLIF(TRIM(@Description), N''));

    SELECT CAST(SCOPE_IDENTITY() AS int);
END;
GO
