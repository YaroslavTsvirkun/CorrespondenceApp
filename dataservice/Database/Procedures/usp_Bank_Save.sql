/* Процедура: створення (@Id = NULL) або оновлення банку в довіднику. МФО унікальне.
   Новий банк одразу підхоплює збережені рахунки з його МФО, які раніше не знайшли банку в довіднику.
   Повертає: Id банку. */
CREATE OR ALTER PROCEDURE dbo.usp_Bank_Save
    @Id        int,
    @Mfo       char(6),
    @Name      nvarchar(200),
    @ShortName nvarchar(100),
    @Edrpou    char(8),
    @SwiftBic  varchar(11),
    @IsActive  bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message nvarchar(200);

    IF @Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Bank WHERE Id = @Id)
    BEGIN
        SET @Message = CONCAT(N'Банк #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Bank WHERE Mfo = @Mfo AND (@Id IS NULL OR Id <> @Id))
    BEGIN
        SET @Message = CONCAT(N'Банк з МФО ', @Mfo, N' уже є в довіднику');
        THROW 50400, @Message, 1;
    END;

    BEGIN TRANSACTION;

    IF @Id IS NULL
    BEGIN
        INSERT dbo.Bank (Mfo, Name, ShortName, Edrpou, SwiftBic, IsActive)
        VALUES (@Mfo, @Name, @ShortName, @Edrpou, @SwiftBic, @IsActive);
        SET @Id = CAST(SCOPE_IDENTITY() AS int);
    END
    ELSE
        UPDATE dbo.Bank
        SET Mfo = @Mfo, Name = @Name, ShortName = @ShortName, Edrpou = @Edrpou, SwiftBic = @SwiftBic, IsActive = @IsActive
        WHERE Id = @Id;

    -- Рахунки з цим МФО без банку (додані до появи банку в довіднику)
    UPDATE dbo.PartyBankAccount SET BankId = @Id
    WHERE BankId IS NULL AND Iban LIKE 'UA%' AND SUBSTRING(Iban, 5, 6) = @Mfo;

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
