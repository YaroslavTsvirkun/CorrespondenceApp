/* =====================================================================
   004_ManagerDocumentsUpdate.sql — роль «Керівник» отримує право
   редагувати документи (documents.update).
   Для нових БД це право вже видається в 003_RolesAndUsers.sql; скрипт
   доповнює БД, розгорнуті раніше. Ідемпотентний.
   ===================================================================== */
SET NOCOUNT ON;

INSERT dbo.RolePermission (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.Role r
CROSS JOIN dbo.Permission p
WHERE r.Name = N'Керівник'
  AND p.Code = N'documents.update'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
GO
