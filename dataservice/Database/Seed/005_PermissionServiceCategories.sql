/* =====================================================================
   005_PermissionServiceCategories.sql — категорії пермішенів за сервісами.
   Після розділення застосунку на сервіси (Кореспонденція, Контрагенти,
   Оргструктура, Адміністрування) редактор ролей групує пермішени за
   категорією, тож категорія має назву сервісу (і розділ — для кореспонденції).
   Для нових БД виконується після 001_ReferenceData.sql. Ідемпотентний.
   ===================================================================== */
SET NOCOUNT ON;

UPDATE p SET Category = v.Category, Description = COALESCE(v.Description, p.Description)
FROM dbo.Permission p
JOIN (VALUES
    (N'documents.read.all',        N'Кореспонденція · Документи',  NULL),
    (N'documents.read.department', N'Кореспонденція · Документи',  NULL),
    (N'documents.read.involved',   N'Кореспонденція · Документи',  NULL),
    (N'documents.create',          N'Кореспонденція · Документи',  NULL),
    (N'documents.update',          N'Кореспонденція · Документи',  NULL),
    (N'documents.delete',          N'Кореспонденція · Документи',  NULL),
    (N'resolutions.create',        N'Кореспонденція · Доручення',  NULL),
    (N'tasks.assign',              N'Кореспонденція · Доручення',  NULL),
    (N'tasks.execute',             N'Кореспонденція · Доручення',  NULL),
    (N'dashboard.overview',        N'Кореспонденція · Дашборди',   NULL),
    (N'dashboard.control',         N'Кореспонденція · Дашборди',   NULL),
    (N'dashboard.my-tasks',        N'Кореспонденція · Дашборди',   NULL),
    (N'dictionaries.manage',       N'Кореспонденція · Довідники та звіти', NULL),
    (N'reports.view',              N'Кореспонденція · Довідники та звіти', NULL),
    (N'settings.manage',           N'Кореспонденція · Довідники та звіти', NULL),
    (N'correspondents.manage',     N'Контрагенти',                 N'Ведення реєстру контрагентів'),
    (N'departments.manage',        N'Оргструктура',                NULL),
    (N'employees.manage',          N'Оргструктура',                NULL),
    (N'users.manage',              N'Адміністрування',             NULL),
    (N'organizations.manage',      N'Адміністрування',             NULL)
) AS v (Code, Category, Description) ON v.Code = p.Code;
GO
