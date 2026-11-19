/* Процедура: дані користувача для автентифікації/авторизації.
   Пошук за @UserId або (якщо він NULL) за @UserName.
   Результати: 1) користувач (vw_UserAccount + хеш пароля); 2) назви ролей;
   3) коди пермішенів (таблична функція dbo.fn_UserPermissions). */
CREATE OR ALTER PROCEDURE dbo.usp_User_GetAccess
    @UserId   int          = NULL,
    @UserName nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId IS NULL
        SELECT @UserId = Id FROM dbo.AppUser WHERE UserName = @UserName;

    SELECT a.Id, a.UserName, u.PasswordHash, a.FullName, a.EmployeeId,
           a.DepartmentId, a.OrganizationId, a.OrganizationName, a.IsActive
    FROM dbo.vw_UserAccount a
    JOIN dbo.AppUser u ON u.Id = a.Id
    WHERE a.Id = @UserId;

    SELECT r.Name
    FROM dbo.UserRole ur
    JOIN dbo.Role r ON r.Id = ur.RoleId
    WHERE ur.UserId = @UserId
    ORDER BY r.Name;

    SELECT Code FROM dbo.fn_UserPermissions(@UserId);
END;
GO
