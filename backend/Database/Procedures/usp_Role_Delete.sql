/* Процедура: видалення ролі (заборонено, якщо роль призначена користувачам). */
CREATE OR ALTER PROCEDURE dbo.usp_Role_Delete
    @Id int
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Id = @Id)
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Роль #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.UserRole WHERE RoleId = @Id)
        THROW 50400, N'Роль призначена користувачам — спочатку зніміть призначення', 1;

    DELETE dbo.Role WHERE Id = @Id;   -- RolePermission видаляються каскадно
END;
GO
