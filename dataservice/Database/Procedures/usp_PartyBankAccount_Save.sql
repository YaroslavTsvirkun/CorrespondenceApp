/* Процедура: створення (@Id = NULL) або оновлення банківського рахунку контрагента.
   - банк визначається за @Mfo (МФО з українського IBAN); немає в довіднику — BankId = NULL;
   - один IBAN — один рахунок контрагента;
   - новий основний відкритий рахунок знімає позначку «основний» з інших рахунків тієї ж валюти.
   Формат IBAN (контрольну суму) перевіряє валідатор застосунку.
   Повертає: Id рахунку. */
CREATE OR ALTER PROCEDURE dbo.usp_PartyBankAccount_Save
    @Id                   int,
    @PartyId              int,
    @Iban                 varchar(34),
    @Mfo                  char(6),
    @BankName             nvarchar(200),
    @SwiftBic             varchar(11),
    @Currency             char(3),
    @Purpose              nvarchar(100),
    @AccountHolderName    nvarchar(300),
    @IsPrimary            bit,
    @ClosedAt             date,
    @Note                 nvarchar(200),
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message nvarchar(200);

    IF NOT EXISTS (SELECT 1 FROM dbo.Party
                   WHERE Id = @PartyId AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId))
    BEGIN
        SET @Message = CONCAT(N'Контрагента #', @PartyId, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF @Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.PartyBankAccount WHERE Id = @Id AND PartyId = @PartyId)
    BEGIN
        SET @Message = CONCAT(N'Рахунок #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.PartyBankAccount
               WHERE PartyId = @PartyId AND Iban = @Iban AND (@Id IS NULL OR Id <> @Id))
        THROW 50400, N'Рахунок з таким IBAN у контрагента вже є', 1;

    DECLARE @BankId int = (SELECT Id FROM dbo.Bank WHERE Mfo = @Mfo);

    BEGIN TRANSACTION;

    IF @IsPrimary = 1 AND @ClosedAt IS NULL
        UPDATE dbo.PartyBankAccount SET IsPrimary = 0
        WHERE PartyId = @PartyId AND Currency = @Currency AND ClosedAt IS NULL AND (@Id IS NULL OR Id <> @Id);

    IF @Id IS NULL
    BEGIN
        INSERT dbo.PartyBankAccount (PartyId, Iban, BankId, BankName, SwiftBic, Currency, Purpose,
                                     AccountHolderName, IsPrimary, ClosedAt, Note)
        VALUES (@PartyId, @Iban, @BankId, @BankName, @SwiftBic, @Currency, @Purpose,
                @AccountHolderName, @IsPrimary, @ClosedAt, @Note);
        SET @Id = CAST(SCOPE_IDENTITY() AS int);
    END
    ELSE
        UPDATE dbo.PartyBankAccount
        SET Iban = @Iban, BankId = @BankId, BankName = @BankName, SwiftBic = @SwiftBic, Currency = @Currency,
            Purpose = @Purpose, AccountHolderName = @AccountHolderName, IsPrimary = @IsPrimary,
            ClosedAt = @ClosedAt, Note = @Note
        WHERE Id = @Id;

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
