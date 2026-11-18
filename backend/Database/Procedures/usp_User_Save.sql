/* Процедура: створення (@Id = NULL) або оновлення користувача разом із його ролями.
   @PasswordHash = NULL під час оновлення — пароль не змінюється.
   @RoleIds — список Id ролей через кому (напр. N'1,3').
   Повертає: Id користувача. */
CREATE OR ALTER PROCEDURE dbo.usp_User_Save
    @Id             int,
    @UserName       nvarchar(50),
    @PasswordHash   nvarchar(256),
    @FullName       nvarchar(150),
    @OrganizationId int,
    @EmployeeId     int,
    @IsActive       bit,
    @RoleIds        nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Message nvarchar(200);

    IF @Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE Id = @Id)
    BEGIN
        SET @Message = CONCAT(N'Користувача #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF @Id IS NULL AND @PasswordHash IS NULL
        THROW 50400, N'Вкажіть пароль', 1;

    BEGIN TRANSACTION;

    IF @Id IS NULL
    BEGIN
        INSERT dbo.AppUser (UserName, PasswordHash, FullName, OrganizationId, EmployeeId, IsActive)
        VALUES (@UserName, @PasswordHash, @FullName, @OrganizationId, @EmployeeId, @IsActive);
        SET @Id = CAST(SCOPE_IDENTITY() AS int);
    END
    ELSE
        UPDATE dbo.AppUser
        SET UserName       = @UserName,
            PasswordHash   = ISNULL(@PasswordHash, PasswordHash),
            FullName       = @FullName,
            OrganizationId = @OrganizationId,
            EmployeeId     = @EmployeeId,
            IsActive       = @IsActive
        WHERE Id = @Id;

    DELETE dbo.UserRole WHERE UserId = @Id;

    INSERT dbo.UserRole (UserId, RoleId)
    SELECT DISTINCT @Id, r.Id
    FROM STRING_SPLIT(ISNULL(@RoleIds, N''), N',') s
    JOIN dbo.Role r ON r.Id = TRY_CAST(TRIM(s.value) AS int);

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END;
GO
