/* =====================================================================
   003_RolesAndUsers.sql — ролі з пермішенами та демо-користувачі.
   Одноразовий скрипт; пропускається, якщо ролі вже існують.
   Паролі зберігаються як PBKDF2-SHA256 (100 000 ітерацій):
   base64(salt).base64(hash) — той самий формат, що й PasswordHasher у C#.
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

-- ---- Користувачі ----
DECLARE @org1 int = (SELECT Id FROM dbo.Organization WHERE Code = N'PROMIN'),
        @org2 int = (SELECT Id FROM dbo.Organization WHERE Code = N'VEKTOR');

DECLARE @Users TABLE (UserName nvarchar(50), PasswordHash nvarchar(256), FullName nvarchar(150),
                      OrganizationId int NULL, EmployeeSurname nvarchar(50) NULL, RoleId int);
INSERT @Users VALUES
    (N'admin',     N'6tSIpmQ+5oY7HRxRi9ocmw==.I2Il2PD1Vgg+t7RbCv8DWllFpAaAksYD+1c829wWr3g=', N'Супер-адміністратор',       NULL,  NULL,          @superAdmin), -- admin123
    (N'registrar', N'MtRUyzm4VjSrA90D3Mq57w==.ydfUHpBKe+4zVjVVfhXAsmtNqI3GVbtHd0BSlzkjuO4=', N'Іваненко Іван Іванович',    @org1, N'Іваненко',   @registrar),  -- registrar123
    (N'manager',   N'g80W9FIQ/Wlp26NUpS+o0w==.WqLxxSbdH0Qtydl11dpEqgGod79VMKIRZjllwNvjkl0=', N'Петренко Петро Петрович',   @org1, N'Петренко',   @manager),    -- manager123
    (N'executor',  N'8UHwN36a6EL1H8hzk6Cmcw==.8o9BKyev/7kJ7KvbJMZCfeD4DaS5AnNXAo5Ehdgu3Zk=', N'Сидоренко Олена Василівна', @org1, N'Сидоренко',  @executor),   -- executor123
    (N'orgadmin1', N'bnCoBLt+Ofewm0bM7WpxGw==.K1b4Pc9MlJcd6sUiJGZvoYQvZOEOANVrICylm5P9aDo=', N'Адмін «Промінь»',           @org1, N'Коваленко',  @orgAdmin),   -- orgadmin123
    (N'orgadmin2', N'bnCoBLt+Ofewm0bM7WpxGw==.K1b4Pc9MlJcd6sUiJGZvoYQvZOEOANVrICylm5P9aDo=', N'Адмін «Вектор»',            @org2, N'Орлов',      @orgAdmin);   -- orgadmin123

INSERT dbo.AppUser (UserName, PasswordHash, FullName, OrganizationId, EmployeeId)
SELECT u.UserName, u.PasswordHash, u.FullName, u.OrganizationId,
       (SELECT TOP (1) e.Id FROM dbo.Employee e WHERE e.FullName LIKE u.EmployeeSurname + N'%' ORDER BY e.Id)
FROM @Users u;

INSERT dbo.UserRole (UserId, RoleId)
SELECT a.Id, u.RoleId FROM @Users u JOIN dbo.AppUser a ON a.UserName = u.UserName;

COMMIT TRANSACTION;
GO
