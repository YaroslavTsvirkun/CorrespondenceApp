/* Процедура: реєстрація нового документа разом із реєстраційним записом.
   - @OrganizationId = NULL (супер-адмін) — організація береться від реєстратора;
   - @RegistrationNumber порожній — номер генерується за патерном організації.
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
    @RegistrarEmployeeId  int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @OrganizationId IS NULL
        SELECT @OrganizationId = OrganizationId FROM dbo.Employee WHERE Id = @RegistrarEmployeeId;
    IF @OrganizationId IS NULL
        THROW 50404, N'Не вдалося визначити організацію документа', 1;

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
        (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId,
         SenderDepartmentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate,
         IncomingNumber, IsControlled, Note)
    VALUES
        (@OrganizationId, @DirectionId, @DocumentTypeId, @CurrentStatusId, @DeliveryChannelId, @CorrespondentId,
         @SenderDepartmentId, @ReceiverDepartmentId, @AuthorEmployeeId, @Title, @Summary, @DocumentDate,
         @IncomingNumber, @IsControlled, @Note);

    SET @Id = CAST(SCOPE_IDENTITY() AS int);

    INSERT dbo.RegistrationRecord
        (DocumentId, RegistrarEmployeeId, RegistrationNumber, RegistrationYear, OrganizationId, SequenceNumber, RegisteredAt)
    VALUES
        (@Id, @RegistrarEmployeeId, @RegistrationNumber, @RegistrationYear, @OrganizationId, @Sequence, SYSUTCDATETIME());

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
