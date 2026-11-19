/* Inline-таблична функція: ролі, що містять заданий пермішен
   (напр. «супер-» ролі з organizations.manage). */
CREATE OR ALTER FUNCTION dbo.fn_RolesWithPermission
(
    @PermissionCode nvarchar(80)
)
RETURNS TABLE
AS
RETURN
    SELECT rp.RoleId
    FROM dbo.RolePermission rp
    JOIN dbo.Permission p ON p.Id = rp.PermissionId
    WHERE p.Code = @PermissionCode;
GO
