/* =====================================================================
   001_ReferenceData.sql — спільні довідники та каталог пермішенів.
   Одноразовий скрипт; кожен блок виконується лише для порожньої таблиці.
   ===================================================================== */

IF NOT EXISTS (SELECT 1 FROM dbo.DocumentDirection)
    INSERT dbo.DocumentDirection (Code, Name) VALUES
        (N'IN',  N'Вхідна'),
        (N'OUT', N'Вихідна'),
        (N'INT', N'Внутрішня');

IF NOT EXISTS (SELECT 1 FROM dbo.DocumentType)
    INSERT dbo.DocumentType (Name, Code) VALUES
        (N'Лист',             N'LST'),
        (N'Наказ',            N'NAK'),
        (N'Запит',            N'ZAP'),
        (N'Службова записка', N'SZ'),
        (N'Договір',          N'DOG'),
        (N'Звернення',        N'ZVN');

IF NOT EXISTS (SELECT 1 FROM dbo.DocumentStatus)
    INSERT dbo.DocumentStatus (Name, IsFinal) VALUES
        (N'Зареєстровано', 0),
        (N'На розгляді',   0),
        (N'На виконанні',  0),
        (N'Виконано',      1),
        (N'Архівовано',    1);

IF NOT EXISTS (SELECT 1 FROM dbo.DeliveryChannel)
    INSERT dbo.DeliveryChannel (Name) VALUES
        (N'Пошта'), (N'Електронна пошта'), (N'Кур''єр'), (N'Особисте подання');

IF NOT EXISTS (SELECT 1 FROM dbo.CorrespondentType)
    INSERT dbo.CorrespondentType (Name) VALUES
        (N'Організація'), (N'Фізична особа'), (N'Державна установа');

IF NOT EXISTS (SELECT 1 FROM dbo.TaskStatus)
    INSERT dbo.TaskStatus (Name, IsFinal) VALUES
        (N'Призначено',  0),
        (N'У роботі',    0),
        (N'Виконано',    1),
        (N'Прострочено', 0),
        (N'Скасовано',   1);

-- Каталог пермішенів (коди мають збігатися з константами Common/Auth.cs → Permissions)
IF NOT EXISTS (SELECT 1 FROM dbo.Permission)
    INSERT dbo.Permission (Code, Category, Description) VALUES
        (N'documents.read.all',        N'Документи',       N'Перегляд усієї кореспонденції'),
        (N'documents.read.department', N'Документи',       N'Перегляд кореспонденції свого підрозділу'),
        (N'documents.read.involved',   N'Документи',       N'Перегляд лише задіяних документів'),
        (N'documents.create',          N'Документи',       N'Реєстрація документів'),
        (N'documents.update',          N'Документи',       N'Редагування документів'),
        (N'documents.delete',          N'Документи',       N'Видалення документів'),
        (N'resolutions.create',        N'Резолюції',       N'Накладання резолюцій'),
        (N'tasks.assign',              N'Доручення',       N'Призначення доручень'),
        (N'tasks.execute',             N'Доручення',       N'Виконання доручень'),
        (N'correspondents.manage',     N'Довідники',       N'Ведення кореспондентів'),
        (N'dictionaries.manage',       N'Довідники',       N'Ведення довідників'),
        (N'employees.manage',          N'Довідники',       N'Ведення працівників'),
        (N'departments.manage',        N'Довідники',       N'Ведення підрозділів'),
        (N'reports.view',              N'Звіти',           N'Перегляд звітів'),
        (N'dashboard.overview',        N'Дашборди',        N'Дашборд «Огляд»'),
        (N'dashboard.control',         N'Дашборди',        N'Дашборд контролю'),
        (N'dashboard.my-tasks',        N'Дашборди',        N'Дашборд «Мої доручення»'),
        (N'users.manage',              N'Адміністрування', N'Керування користувачами та ролями'),
        (N'organizations.manage',      N'Адміністрування', N'Керування організаціями (супер-адмін)'),
        (N'settings.manage',           N'Адміністрування', N'Налаштування організації (нумерація документів)');
GO
