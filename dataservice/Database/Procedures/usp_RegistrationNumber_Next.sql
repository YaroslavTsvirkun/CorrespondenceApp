/* Процедура: наступний вільний реєстраційний номер за патерном організації.
   Лічильник — за (організація, тип документа, рік); пропускає вже зайняті номери
   (ручні номери або патерн без {type}, що дає однакові рядки для різних типів).
   Помилки: 50400 — бізнес-правило (HTTP 400), 50404 — не знайдено (HTTP 404). */
CREATE OR ALTER PROCEDURE dbo.usp_RegistrationNumber_Next
    @OrganizationId int,
    @DirectionId    int,
    @DocumentTypeId int,
    @Year           int,
    @Number         nvarchar(30) OUTPUT,
    @Sequence       int          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern  nvarchar(100) = (SELECT RegistrationNumberPattern FROM dbo.Organization WHERE Id = @OrganizationId);
    DECLARE @TypeCode nvarchar(20)  = (SELECT Code FROM dbo.DocumentType WHERE Id = @DocumentTypeId);
    DECLARE @DirCode  nvarchar(10)  = (SELECT Code FROM dbo.DocumentDirection WHERE Id = @DirectionId);
    DECLARE @Attempt  int = 0;

    SET @Sequence = dbo.fn_NextRegistrationSequence(@OrganizationId, @DocumentTypeId, @Year);

    WHILE @Attempt < 1000
    BEGIN
        SET @Number = dbo.fn_FormatRegistrationNumber(@Pattern, @Sequence, @Year, @TypeCode, @DirCode);
        IF dbo.fn_IsRegistrationNumberTaken(@OrganizationId, @Year, @Number, NULL) = 0
            RETURN 0;
        SET @Sequence += 1;
        SET @Attempt += 1;
    END;

    THROW 50400, N'Не вдалося згенерувати унікальний реєстраційний номер', 1;
END;
GO
