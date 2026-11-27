/* Процедура: реєстрація нового документа разом із реєстраційним записом.
   - @OrganizationId = NULL (супер-адмін) — організація береться від реєстратора;
   - @RegistrationNumber порожній — номер генерується за патерном організації;
   - назва кореспондента фіксується знімком (CorrespondentName); @Addressees — додаткові
     адресати й копії (dbo.DocumentPartyList) зі знімками назв.
   Генерація номера серіалізується блокуванням sp_getapplock на організацію,
   тож паралельні реєстрації не отримають однаковий номер.
   Повертає: Id нового документа. */
CREATE OR ALTER PROCEDURE dbo.usp_Document_Create
    @OrganizationId       int,
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
    @Addressees           dbo.DocumentPartyList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @OrganizationId IS NULL
        SELECT @OrganizationId = OrganizationId FROM dbo.Employee WHERE Id = @RegistrarEmployeeId;
    IF @OrganizationId IS NULL
        THROW 50404, N'Не вдалося визначити організацію документа', 1;

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

    DECLARE @Id int, @Sequence int, @Message nvarchar(400);
    DECLARE @LockName nvarchar(255) = CONCAT(N'registration-number:', @OrganizationId);
    SET @RegistrationNumber = NULLIF(TRIM(@RegistrationNumber), N'');

    BEGIN TRANSACTION;

    EXEC sp_getapplock @Resource = @LockName, @LockMode = N'Exclusive',
                       @LockOwner = N'Transaction', @LockTimeout = 15000;

    IF @RegistrationNumber IS NOT NULL
    BEGIN
        IF dbo.fn_IsRegistrationNumberTaken(@OrganizationId, @RegistrationYear, @RegistrationNumber, NULL) = 1
        BEGIN
            SET @Message = CONCAT(N'Реєстраційний номер «', @RegistrationNumber, N'» уже використано в ',
                                  @RegistrationYear, N' році');
            THROW 50400, @Message, 1;
        END;
        SET @Sequence = NULL;   -- номер уведено вручну
    END
    ELSE
        EXEC dbo.usp_RegistrationNumber_Next @OrganizationId, @DirectionId, @DocumentTypeId, @RegistrationYear,
             @RegistrationNumber OUTPUT, @Sequence OUTPUT;

    -- Для тригера історії статусів: хто змінив (якщо застосунок не передав користувача)
    IF SESSION_CONTEXT(N'EmployeeId') IS NULL
        EXEC sp_set_session_context @key = N'EmployeeId', @value = @RegistrarEmployeeId;

    INSERT dbo.Document
        (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, CorrespondentName,
         SenderDepartmentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate,
         IncomingNumber, IsControlled, Note)
    VALUES
        (@OrganizationId, @DirectionId, @DocumentTypeId, @CurrentStatusId, @DeliveryChannelId, @CorrespondentId,
         (SELECT COALESCE(ShortName, FullName) FROM dbo.Party WHERE Id = @CorrespondentId),
         @SenderDepartmentId, @ReceiverDepartmentId, @AuthorEmployeeId, @Title, @Summary, @DocumentDate,
         @IncomingNumber, @IsControlled, @Note);

    SET @Id = CAST(SCOPE_IDENTITY() AS int);

    INSERT dbo.DocumentParty (DocumentId, PartyId, Role, PartyName, SortOrder)
    SELECT @Id, a.PartyId, a.Role, COALESCE(p.ShortName, p.FullName), a.RowNo
    FROM @Addressees a JOIN dbo.Party p ON p.Id = a.PartyId;

    INSERT dbo.RegistrationRecord
        (DocumentId, RegistrarEmployeeId, RegistrationNumber, RegistrationYear, OrganizationId, SequenceNumber, RegisteredAt)
    VALUES
        (@Id, @RegistrarEmployeeId, @RegistrationNumber, @RegistrationYear, @OrganizationId, @Sequence, SYSUTCDATETIME());

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
