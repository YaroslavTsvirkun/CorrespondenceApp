/* Процедура: редагування документа та його реєстраційного запису.
   @Bypass = 1 — супер-адмін (без тенант-обмеження), інакше документ має належати
   @TenantOrganizationId. Якщо реєстраційного запису немає — створюється
   (з автогенерацією номера, якщо його не вказано).
   Знімок назви кореспондента зберігається, поки кореспондент не змінився.
   @ReplaceAddressees = 1 — додаткові адресати й копії замінюються на @Addressees. */
CREATE OR ALTER PROCEDURE dbo.usp_Document_Update
    @Id                   int,
    @DirectionId          int,
    @DocumentTypeId       int,
    @CurrentStatusId      int,
    @DeliveryChannelId    int,
    @CorrespondentId      int,
    @SenderDepartmentId   int,
    @ReceiverDepartmentId int,
    @AuthorEmployeeId     int,
    @Title                nvarchar(300),
    @Summary              nvarchar(1000),
    @DocumentDate         date,
    @IncomingNumber       nvarchar(max),
    @IsControlled         bit,
    @Note                 nvarchar(max),
    @RegistrationNumber   nvarchar(30),
    @RegistrationYear     int,
    @RegistrarEmployeeId  int,
    @Addressees           dbo.DocumentPartyList READONLY,
    @ReplaceAddressees    bit,   -- 0 — адресатів не змінювати (клієнт їх не передав)
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OrganizationId int, @Sequence int, @Message nvarchar(400);

    SELECT @OrganizationId = OrganizationId
    FROM dbo.Document
    WHERE Id = @Id AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId);

    IF @OrganizationId IS NULL
    BEGIN
        SET @Message = CONCAT(N'Документ #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    -- Кореспондент — контрагент з реєстру організації документа
    IF @CorrespondentId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Party WHERE Id = @CorrespondentId AND OrganizationId = @OrganizationId)
        THROW 50400, N'Кореспондента не знайдено в реєстрі контрагентів організації документа', 1;

    -- Підрозділи, автор і реєстратор — з організації документа
    IF EXISTS (SELECT 1 FROM (VALUES (@SenderDepartmentId), (@ReceiverDepartmentId)) AS x (Id)
               WHERE x.Id IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM dbo.Department d WHERE d.Id = x.Id AND d.OrganizationId = @OrganizationId))
        THROW 50400, N'Підрозділ не належить організації документа', 1;
    IF EXISTS (SELECT 1 FROM (VALUES (@AuthorEmployeeId), (@RegistrarEmployeeId)) AS x (Id)
               WHERE x.Id IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM dbo.Employee e WHERE e.Id = x.Id AND e.OrganizationId = @OrganizationId))
        THROW 50400, N'Автор або реєстратор не належить організації документа', 1;

    -- Додаткові адресати й копії — контрагенти організації документа, без повторів і без головного кореспондента
    IF EXISTS (SELECT 1 FROM @Addressees a
               WHERE a.Role NOT IN (1, 2)
                  OR a.PartyId = @CorrespondentId
                  OR NOT EXISTS (SELECT 1 FROM dbo.Party p WHERE p.Id = a.PartyId AND p.OrganizationId = @OrganizationId))
        THROW 50400, N'Адресати документа мають бути контрагентами його організації (без головного кореспондента)', 1;
    IF EXISTS (SELECT PartyId FROM @Addressees GROUP BY PartyId HAVING COUNT(*) > 1)
        THROW 50400, N'Контрагента додано до адресатів документа двічі', 1;

    SET @RegistrationNumber = NULLIF(TRIM(@RegistrationNumber), N'');
    IF @RegistrationNumber IS NOT NULL
       AND dbo.fn_IsRegistrationNumberTaken(@OrganizationId, @RegistrationYear, @RegistrationNumber, @Id) = 1
    BEGIN
        SET @Message = CONCAT(N'Реєстраційний номер «', @RegistrationNumber, N'» уже використано в ',
                              @RegistrationYear, N' році');
        THROW 50400, @Message, 1;
    END;

    BEGIN TRANSACTION;

    UPDATE dbo.Document
    SET DirectionId          = @DirectionId,
        DocumentTypeId       = @DocumentTypeId,
        CurrentStatusId      = @CurrentStatusId,
        DeliveryChannelId    = @DeliveryChannelId,
        CorrespondentId      = @CorrespondentId,
        -- Знімок назви зберігається, поки кореспондент той самий; новий кореспондент — поточна назва
        CorrespondentName    = CASE
                                   WHEN @CorrespondentId IS NULL THEN NULL
                                   WHEN @CorrespondentId = CorrespondentId AND CorrespondentName IS NOT NULL THEN CorrespondentName
                                   ELSE (SELECT COALESCE(ShortName, FullName) FROM dbo.Party WHERE Id = @CorrespondentId)
                               END,
        SenderDepartmentId   = @SenderDepartmentId,
        ReceiverDepartmentId = @ReceiverDepartmentId,
        AuthorEmployeeId     = @AuthorEmployeeId,
        Title                = @Title,
        Summary              = @Summary,
        DocumentDate         = @DocumentDate,
        IncomingNumber       = @IncomingNumber,
        IsControlled         = @IsControlled,
        Note                 = @Note
    WHERE Id = @Id;

    -- Адресати замінюються повністю; у тих, хто лишився, зберігається знімок назви
    IF @ReplaceAddressees = 1
    BEGIN
        DECLARE @Kept TABLE (PartyId int PRIMARY KEY, PartyName nvarchar(300));
        INSERT @Kept SELECT PartyId, PartyName FROM dbo.DocumentParty WHERE DocumentId = @Id;

        DELETE dbo.DocumentParty WHERE DocumentId = @Id;

        INSERT dbo.DocumentParty (DocumentId, PartyId, Role, PartyName, SortOrder)
        SELECT @Id, a.PartyId, a.Role, COALESCE(k.PartyName, p.ShortName, p.FullName), a.RowNo
        FROM @Addressees a
        JOIN dbo.Party p ON p.Id = a.PartyId
        LEFT JOIN @Kept k ON k.PartyId = a.PartyId;
    END;

    IF EXISTS (SELECT 1 FROM dbo.RegistrationRecord WHERE DocumentId = @Id)
        UPDATE dbo.RegistrationRecord
        SET RegistrationNumber  = ISNULL(@RegistrationNumber, RegistrationNumber),
            RegistrationYear    = @RegistrationYear,
            RegistrarEmployeeId = @RegistrarEmployeeId
        WHERE DocumentId = @Id;
    ELSE
    BEGIN
        IF @RegistrationNumber IS NULL
            EXEC dbo.usp_RegistrationNumber_Next @OrganizationId, @DirectionId, @DocumentTypeId, @RegistrationYear,
                 @RegistrationNumber OUTPUT, @Sequence OUTPUT;

        INSERT dbo.RegistrationRecord
            (DocumentId, RegistrarEmployeeId, RegistrationNumber, RegistrationYear, OrganizationId, SequenceNumber, RegisteredAt)
        VALUES
            (@Id, @RegistrarEmployeeId, @RegistrationNumber, @RegistrationYear, @OrganizationId, @Sequence, SYSUTCDATETIME());
    END;

    COMMIT TRANSACTION;
END;
GO
