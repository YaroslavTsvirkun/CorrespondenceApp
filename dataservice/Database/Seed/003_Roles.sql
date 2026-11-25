/* =====================================================================
   003_Roles.sql — базові ролі з пермішенами (потрібні в будь-якій БД).
   Одноразовий скрипт; пропускається, якщо ролі вже існують.
   Демо-користувачі — окремо, у DemoSeed/002_DemoUsers.sql (лише з Database:SeedDemoData).
   Першого супер-адміністратора для робочої БД створює сервіс даних із налаштування
   Database:InitialAdmin (див. InitialAdminBootstrapper).
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.Role) RETURN;

BEGIN TRANSACTION;

DECLARE @superAdmin int, @orgAdmin int, @registrar int, @manager int, @executor int;

INSERT dbo.Role (Name, Description) VALUES (N'Адміністратор', N'Супер-адміністратор (усі організації)');
SET @superAdmin = SCOPE_IDENTITY();
INSERT dbo.Role (Name, Description) VALUES (N'Адміністратор організації', N'Адміністрування власної організації');
SET @orgAdmin = SCOPE_IDENTITY();
INSERT dbo.Role (Name, Description) VALUES (N'Реєстратор', N'Канцелярія: реєстрація та облік');
SET @registrar = SCOPE_IDENTITY();
INSERT dbo.Role (Name, Description) VALUES (N'Керівник', N'Розгляд документів і резолюції');
SET @manager = SCOPE_IDENTITY();
INSERT dbo.Role (Name, Description) VALUES (N'Виконавець', N'Виконання доручень');
SET @executor = SCOPE_IDENTITY();

-- Супер-адмін — усі права; адмін організації — усі, крім organizations.manage
INSERT dbo.RolePermission (RoleId, PermissionId) SELECT @superAdmin, Id FROM dbo.Permission;
INSERT dbo.RolePermission (RoleId, PermissionId) SELECT @orgAdmin, Id FROM dbo.Permission WHERE Code <> N'organizations.manage';

INSERT dbo.RolePermission (RoleId, PermissionId)
SELECT @registrar, Id FROM dbo.Permission WHERE Code IN (
    N'documents.read.all', N'documents.create', N'documents.update', N'documents.delete',
    N'correspondents.manage', N'dictionaries.manage', N'employees.manage', N'departments.manage',
    N'reports.view', N'dashboard.overview', N'dashboard.control');

INSERT dbo.RolePermission (RoleId, PermissionId)
SELECT @manager, Id FROM dbo.Permission WHERE Code IN (
    N'documents.read.involved', N'documents.read.department', N'documents.update',
    N'resolutions.create', N'tasks.assign',
    N'reports.view', N'dashboard.overview', N'dashboard.control');

INSERT dbo.RolePermission (RoleId, PermissionId)
SELECT @executor, Id FROM dbo.Permission WHERE Code IN (
    N'documents.read.involved', N'tasks.execute', N'dashboard.my-tasks');

COMMIT TRANSACTION;
GO
