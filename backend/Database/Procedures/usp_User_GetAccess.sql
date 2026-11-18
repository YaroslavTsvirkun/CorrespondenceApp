/* Процедура: дані користувача для автентифікації/авторизації.
   Пошук за @UserId або (якщо він NULL) за @UserName.
   Результати: 1) користувач; 2) назви ролей; 3) коди пермішенів (унікальні). */
CREATE OR ALTER PROCEDURE dbo.usp_User_GetAccess
    @UserId   int          = NULL,
    @UserName nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId IS NULL
        SELECT @UserId = Id FROM dbo.AppUser WHERE UserName = @UserName;

    SELECT u.Id, u.UserName, u.PasswordHash, u.FullName, u.EmployeeId,
           e.DepartmentId, u.OrganizationId, o.Name AS OrganizationName, u.IsActive
    FROM dbo.AppUser u
    LEFT JOIN dbo.Employee e     ON e.Id = u.EmployeeId
    LEFT JOIN dbo.Organization o ON o.Id = u.OrganizationId
    WHERE u.Id = @UserId;

    SELECT r.Name
    FROM dbo.UserRole ur
    JOIN dbo.Role r ON r.Id = ur.RoleId
    WHERE ur.UserId = @UserId
    ORDER BY r.Name;

    SELECT DISTINCT p.Code
    FROM dbo.UserRole ur
    JOIN dbo.RolePermission rp ON rp.RoleId = ur.RoleId
    JOIN dbo.Permission p      ON p.Id = rp.PermissionId
    WHERE ur.UserId = @UserId;
END;
GO
