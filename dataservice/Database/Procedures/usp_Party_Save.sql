/* Процедура: створення (@Id = NULL) або оновлення контрагента разом з адресами, контактами,
   контактними особами й категоріями (дочірні записи замінюються повністю; банківські рахунки —
   окремо, dbo.usp_PartyBankAccount_Save).
   - новий контрагент належить @OrganizationId (організації користувача), наявний — своїй;
   - головна юрособа (@ParentPartyId) — з тієї ж організації, без циклів;
   - код (ЄДРПОУ / РНОКПП) унікальний серед головних контрагентів організації;
   - невідомі категорії ігноруються.
   Формат коду перевіряє валідатор застосунку (контрольна сума).
   Повертає: Id контрагента. */
CREATE OR ALTER PROCEDURE dbo.usp_Party_Save
    @Id                   int,
    @OrganizationId       int,
    @Kind                 tinyint,
    @FullName             nvarchar(300),
    @ShortName            nvarchar(150),
    @Code                 varchar(10),
    @LegalFormId          int,
    @ParentPartyId        int,
    @IsActive             bit,
    @Note                 nvarchar(max),
    @CategoryIds          dbo.IdList READONLY,
    @Addresses            dbo.PartyAddressList READONLY,
    @Contacts             dbo.PartyContactList READONLY,
    @Persons              dbo.PartyPersonList READONLY,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message nvarchar(400);
    SET @Code = NULLIF(TRIM(@Code), '');

    IF @Id IS NOT NULL
    BEGIN
        SET @OrganizationId = NULL;
        SELECT @OrganizationId = OrganizationId FROM dbo.Party
        WHERE Id = @Id AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId);
        IF @OrganizationId IS NULL
        BEGIN
            SET @Message = CONCAT(N'Контрагента #', @Id, N' не знайдено');
            THROW 50404, @Message, 1;
        END;
    END
    ELSE IF @OrganizationId IS NULL
        THROW 50400, N'Створення доступне лише в межах організації', 1;

    IF @ParentPartyId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Party WHERE Id = @ParentPartyId AND OrganizationId = @OrganizationId)
            THROW 50400, N'Головного контрагента не знайдено в реєстрі організації', 1;

        IF @Id IS NOT NULL AND EXISTS (
            SELECT 1 FROM dbo.fn_PartyAncestors(@ParentPartyId) WHERE Id = @Id)
            THROW 50400, N'Філія не може бути головною для власного головного контрагента', 1;
    END;

    IF @Code IS NOT NULL AND @ParentPartyId IS NULL
    BEGIN
        DECLARE @Duplicate nvarchar(300) = (
            SELECT TOP (1) COALESCE(ShortName, FullName) FROM dbo.Party
            WHERE OrganizationId = @OrganizationId AND Code = @Code AND ParentPartyId IS NULL
              AND (@Id IS NULL OR Id <> @Id));
        IF @Duplicate IS NOT NULL
        BEGIN
            SET @Message = CONCAT(N'Контрагент з кодом ', @Code, N' уже є в реєстрі: ', @Duplicate);
            THROW 50400, @Message, 1;
        END;
    END;

    BEGIN TRANSACTION;

    IF @Id IS NULL
    BEGIN
        INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code, LegalFormId, ParentPartyId, IsActive, Note)
        VALUES (@OrganizationId, @Kind, @FullName, @ShortName, @Code, @LegalFormId, @ParentPartyId, @IsActive, @Note);
        SET @Id = CAST(SCOPE_IDENTITY() AS int);
    END
    ELSE
    BEGIN
        UPDATE dbo.Party
        SET Kind = @Kind, FullName = @FullName, ShortName = @ShortName, Code = @Code, LegalFormId = @LegalFormId,
            ParentPartyId = @ParentPartyId, IsActive = @IsActive, Note = @Note
        WHERE Id = @Id;

        DELETE dbo.PartyCategoryLink WHERE PartyId = @Id;
        DELETE dbo.PartyAddress WHERE PartyId = @Id;
        DELETE dbo.PartyContact WHERE PartyId = @Id;
        DELETE dbo.PartyPerson WHERE PartyId = @Id;
    END;

    INSERT dbo.PartyCategoryLink (PartyId, CategoryId)
    SELECT @Id, c.Id FROM @CategoryIds x JOIN dbo.PartyCategory c ON c.Id = x.Id;

    INSERT dbo.PartyAddress (PartyId, Kind, Country, PostalCode, Region, City, Line)
    SELECT @Id, Kind, Country, PostalCode, Region, City, Line FROM @Addresses ORDER BY RowNo;

    INSERT dbo.PartyContact (PartyId, Kind, Value, IsPrimary, Note)
    SELECT @Id, Kind, Value, IsPrimary, Note FROM @Contacts ORDER BY RowNo;

    INSERT dbo.PartyPerson (PartyId, FullName, PositionName, Phone, Email, IsSignatory, Note)
    SELECT @Id, FullName, PositionName, Phone, Email, IsSignatory, Note FROM @Persons ORDER BY RowNo;

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
