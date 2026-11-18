/* Процедура: редагування документа та його реєстраційного запису.
   @Bypass = 1 — супер-адмін (без тенант-обмеження), інакше документ має належати
   @TenantOrganizationId. Якщо реєстраційного запису немає — створюється
   (з автогенерацією номера, якщо його не вказано). */
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
