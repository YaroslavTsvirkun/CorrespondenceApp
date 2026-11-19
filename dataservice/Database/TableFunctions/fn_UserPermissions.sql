/* Inline-таблична функція: унікальні коди пермішенів користувача (через усі його ролі). */
CREATE OR ALTER FUNCTION dbo.fn_UserPermissions
(
    @UserId int
)
RETURNS TABLE
AS
RETURN
    SELECT DISTINCT p.Code
    FROM dbo.UserRole ur
    JOIN dbo.RolePermission rp ON rp.RoleId = ur.RoleId
    JOIN dbo.Permission p      ON p.Id = rp.PermissionId
    WHERE ur.UserId = @UserId;
GO
