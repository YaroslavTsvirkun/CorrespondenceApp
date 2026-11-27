/* Процедура: видалення контрагента разом з адресами, контактами, особами й рахунками (каскадно).
   Заборонено, якщо контрагент є в документах (історія листування — його роблять неактивним)
   або має філії. */
CREATE OR ALTER PROCEDURE dbo.usp_Party_Delete
    @Id                   int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Message nvarchar(300);

    IF NOT EXISTS (SELECT 1 FROM dbo.Party
                   WHERE Id = @Id AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId))
    BEGIN
        SET @Message = CONCAT(N'Контрагента #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    DECLARE @Documents int = (SELECT COUNT(*) FROM dbo.Document WHERE CorrespondentId = @Id);
    IF @Documents > 0
    BEGIN
        SET @Message = CONCAT(N'Контрагент є в документах (', @Documents,
                              N') — видалення заборонено. Позначте його як неактивного');
        THROW 50400, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Party WHERE ParentPartyId = @Id)
        THROW 50400, N'Контрагент має філії — спершу видаліть їх або змініть головного контрагента', 1;

    DELETE dbo.Party WHERE Id = @Id;
END;
GO
