/* Процедура: створення (@Id = NULL) або оновлення ролі разом із набором пермішенів.
   @PermissionCodes — табличний параметр dbo.CodeList (невідомі коди ігноруються).
   Повертає: Id ролі. */
CREATE OR ALTER PROCEDURE dbo.usp_Role_Save
    @Id              int,
    @Name            nvarchar(50),
    @Description     nvarchar(max),
    @PermissionCodes dbo.CodeList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Id = @Id)
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Роль #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    BEGIN TRANSACTION;

    IF @Id IS NULL
    BEGIN
        INSERT dbo.Role (Name, Description) VALUES (@Name, @Description);
        SET @Id = CAST(SCOPE_IDENTITY() AS int);
    END
    ELSE
        UPDATE dbo.Role SET Name = @Name, Description = @Description WHERE Id = @Id;

    DELETE dbo.RolePermission WHERE RoleId = @Id;

    INSERT dbo.RolePermission (RoleId, PermissionId)
    SELECT @Id, p.Id
    FROM @PermissionCodes x
    JOIN dbo.Permission p ON p.Code = x.Code;

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
