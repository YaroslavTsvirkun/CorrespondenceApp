/* Процедура: видалення організації (заборонено за наявності підрозділів,
   користувачів або дочірніх організацій). */
CREATE OR ALTER PROCEDURE dbo.usp_Organization_Delete
    @Id int
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Organization WHERE Id = @Id)
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Організацію #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Organization WHERE ParentOrganizationId = @Id)
       OR EXISTS (SELECT 1 FROM dbo.Department WHERE OrganizationId = @Id)
       OR EXISTS (SELECT 1 FROM dbo.AppUser WHERE OrganizationId = @Id)
        THROW 50400, N'Організація має підрозділи, користувачів або дочірні організації — видалення заборонено', 1;

    DELETE dbo.Organization WHERE Id = @Id;
END;
GO
