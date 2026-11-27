/* =====================================================================
   001_DemoBase.sql — демонстраційні дані (лише з Database:SeedDemoData): організації, підрозділи,
   працівники, контрагенти (з кодами, адресами, контактами й банківськими рахунками), документи,
   резолюції та доручення.
   Одноразовий скрипт; пропускається, якщо організації вже існують.
   Замінив 001_DemoData.sql (з появою реєстру контрагентів): у вже заповнених БД новий файл
   лише фіксується в журналі — їхніх кореспондентів переносить Schema/006_Parties.sql.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.Organization) RETURN;

BEGIN TRANSACTION;

-- ---- Довідники (ідентифікатори) ----
DECLARE @dIn  int = (SELECT Id FROM dbo.DocumentDirection WHERE Code = N'IN'),
        @dOut int = (SELECT Id FROM dbo.DocumentDirection WHERE Code = N'OUT'),
        @dInt int = (SELECT Id FROM dbo.DocumentDirection WHERE Code = N'INT');

DECLARE @tLetter  int = (SELECT Id FROM dbo.DocumentType WHERE Code = N'LST'),
        @tOrder   int = (SELECT Id FROM dbo.DocumentType WHERE Code = N'NAK'),
        @tRequest int = (SELECT Id FROM dbo.DocumentType WHERE Code = N'ZAP'),
        @tMemo    int = (SELECT Id FROM dbo.DocumentType WHERE Code = N'SZ'),
        @tAppeal  int = (SELECT Id FROM dbo.DocumentType WHERE Code = N'ZVN');

DECLARE @sReg    int = (SELECT Id FROM dbo.DocumentStatus WHERE Name = N'Зареєстровано'),
        @sReview int = (SELECT Id FROM dbo.DocumentStatus WHERE Name = N'На розгляді'),
        @sExec   int = (SELECT Id FROM dbo.DocumentStatus WHERE Name = N'На виконанні'),
        @sDone   int = (SELECT Id FROM dbo.DocumentStatus WHERE Name = N'Виконано'),
        @sArch   int = (SELECT Id FROM dbo.DocumentStatus WHERE Name = N'Архівовано');

DECLARE @chMail     int = (SELECT Id FROM dbo.DeliveryChannel WHERE Name = N'Пошта'),
        @chEmail    int = (SELECT Id FROM dbo.DeliveryChannel WHERE Name = N'Електронна пошта'),
        @chPersonal int = (SELECT Id FROM dbo.DeliveryChannel WHERE Name = N'Особисте подання');

DECLARE @lfTov int = (SELECT Id FROM dbo.LegalForm WHERE Code = N'ТОВ'),
        @lfAt  int = (SELECT Id FROM dbo.LegalForm WHERE Code = N'АТ');

DECLARE @bankPrivat   int = (SELECT Id FROM dbo.Bank WHERE Mfo = '305299'),
        @bankOschad   int = (SELECT Id FROM dbo.Bank WHERE Mfo = '300465'),
        @bankUniversal int = (SELECT Id FROM dbo.Bank WHERE Mfo = '322001'),
        @bankRaiff    int = (SELECT Id FROM dbo.Bank WHERE Mfo = '380805'),
        @bankTreasury int = (SELECT Id FROM dbo.Bank WHERE Mfo = '820172');

DECLARE @tsAssigned   int = (SELECT Id FROM dbo.TaskStatus WHERE Name = N'Призначено'),
        @tsInProgress int = (SELECT Id FROM dbo.TaskStatus WHERE Name = N'У роботі'),
        @tsDone       int = (SELECT Id FROM dbo.TaskStatus WHERE Name = N'Виконано'),
        @tsOverdue    int = (SELECT Id FROM dbo.TaskStatus WHERE Name = N'Прострочено');

-- ---- Організації (дерево) ----
DECLARE @org1 int, @org1South int, @org2 int;
INSERT dbo.Organization (Name, Code, RegistrationNumberPattern) VALUES (N'Підприємство «Промінь»', N'PROMIN', N'{type}-{seq:000}/{year}');
SET @org1 = SCOPE_IDENTITY();
INSERT dbo.Organization (Name, Code, ParentOrganizationId) VALUES (N'Філія «Промінь-Південь»', N'PROMIN-S', @org1);
SET @org1South = SCOPE_IDENTITY();
INSERT dbo.Organization (Name, Code, RegistrationNumberPattern) VALUES (N'ТОВ «Вектор»', N'VEKTOR', N'{dir}-{seq:0000}/{yy}');
SET @org2 = SCOPE_IDENTITY();

-- ---- Підрозділи ----
DECLARE @admin int, @canc int, @legal int, @hr int, @acc int, @dir2 int, @gen2 int;
INSERT dbo.Department (OrganizationId, Name, ShortName) VALUES (@org1, N'Адміністрація', N'Адмін.');
SET @admin = SCOPE_IDENTITY();
INSERT dbo.Department (OrganizationId, Name, ShortName, ParentDepartmentId) VALUES (@org1, N'Канцелярія', N'Канц.', @admin);
SET @canc = SCOPE_IDENTITY();
INSERT dbo.Department (OrganizationId, Name, ShortName, ParentDepartmentId) VALUES (@org1, N'Юридичний відділ', N'Юрвідділ', @admin);
SET @legal = SCOPE_IDENTITY();
INSERT dbo.Department (OrganizationId, Name, ShortName, ParentDepartmentId) VALUES (@org1, N'Відділ кадрів', N'ВК', @admin);
SET @hr = SCOPE_IDENTITY();
INSERT dbo.Department (OrganizationId, Name, ShortName, ParentDepartmentId) VALUES (@org1, N'Бухгалтерія', N'Бух.', @admin);
SET @acc = SCOPE_IDENTITY();
INSERT dbo.Department (OrganizationId, Name, ShortName) VALUES (@org2, N'Дирекція', N'Дир.');
SET @dir2 = SCOPE_IDENTITY();
INSERT dbo.Department (OrganizationId, Name, ShortName, ParentDepartmentId) VALUES (@org2, N'Загальний відділ', N'ЗВ', @dir2);
SET @gen2 = SCOPE_IDENTITY();

-- ---- Працівники ----
DECLARE @e1 int, @e2 int, @e3 int, @e4 int, @e5 int, @e6 int, @e7 int;
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Phone, Email) VALUES (@org1, @canc, N'Іваненко Іван Іванович', N'Реєстратор', N'050-111-22-33', N'ivanenko@promin.ua');
SET @e1 = SCOPE_IDENTITY();
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Phone, Email) VALUES (@org1, @admin, N'Петренко Петро Петрович', N'Директор', N'050-222-33-44', N'petrenko@promin.ua');
SET @e2 = SCOPE_IDENTITY();
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Phone, Email) VALUES (@org1, @legal, N'Сидоренко Олена Василівна', N'Юрисконсульт', N'050-333-44-55', N'sydorenko@promin.ua');
SET @e3 = SCOPE_IDENTITY();
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Email) VALUES (@org1, @hr, N'Коваленко Марія Степанівна', N'Інспектор з кадрів', N'kovalenko@promin.ua');
SET @e4 = SCOPE_IDENTITY();
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName) VALUES (@org1, @acc, N'Бондаренко Андрій Олегович', N'Головний бухгалтер');
SET @e5 = SCOPE_IDENTITY();
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Email) VALUES (@org2, @dir2, N'Орлов Сергій Миколайович', N'Директор', N'orlov@vektor.ua');
SET @e6 = SCOPE_IDENTITY();
INSERT dbo.Employee (OrganizationId, DepartmentId, FullName, PositionName, Email) VALUES (@org2, @gen2, N'Левченко Ірина Олександрівна', N'Діловод', N'levchenko@vektor.ua');
SET @e7 = SCOPE_IDENTITY();

-- ---- Контрагенти (Kind: 1 — юрособа, 2 — ФОП, 3 — фізособа, 4 — орган влади) ----
-- Коди ЄДРПОУ / РНОКПП та IBAN вигадані, але з правильними контрольними сумами
DECLARE @catSupplier int, @catGov int;
INSERT dbo.PartyCategory (Name) VALUES (N'Постачальник');  SET @catSupplier = SCOPE_IDENTITY();
INSERT dbo.PartyCategory (Name) VALUES (N'Орган влади');   SET @catGov = SCOPE_IDENTITY();

DECLARE @cor1 int, @cor2 int, @cor3 int, @cor4 int, @cor5 int, @cor4Branch int, @cor6 int;
INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code, LegalFormId)
VALUES (@org1, 1, N'ТОВАРИСТВО З ОБМЕЖЕНОЮ ВІДПОВІДАЛЬНІСТЮ «ПРОМТЕХБУД»', N'ТОВ «Промтехбуд»', '32145678', @lfTov);
SET @cor1 = SCOPE_IDENTITY();
INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code)
VALUES (@org1, 4, N'Департамент освіти Харківської обласної державної адміністрації', N'Департамент освіти ХОДА', '02142632');
SET @cor2 = SCOPE_IDENTITY();
INSERT dbo.Party (OrganizationId, Kind, FullName, Code)
VALUES (@org1, 3, N'Шевченко Тарас Григорович', '3124567896');
SET @cor3 = SCOPE_IDENTITY();
INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code, LegalFormId)
VALUES (@org1, 1, N'АКЦІОНЕРНЕ ТОВАРИСТВО «ЕНЕРГОПОСТАЧ»', N'АТ «Енергопостач»', '23456108', @lfAt);
SET @cor4 = SCOPE_IDENTITY();
INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code, ParentPartyId)
VALUES (@org1, 1, N'Харківська філія АТ «Енергопостач»', N'Харківська філія «Енергопостач»', '43512783', @cor4);
SET @cor4Branch = SCOPE_IDENTITY();
INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code)
VALUES (@org1, 2, N'Фізична особа-підприємець Коваль Андрій Миколайович', N'ФОП Коваль А.М.', '2987654320');
SET @cor6 = SCOPE_IDENTITY();
INSERT dbo.Party (OrganizationId, Kind, FullName, ShortName, Code, LegalFormId)
VALUES (@org2, 1, N'ТОВАРИСТВО З ОБМЕЖЕНОЮ ВІДПОВІДАЛЬНІСТЮ «ЛОГІСТИК-ПЛЮС»', N'ТОВ «Логістик-Плюс»', '36987412', @lfTov);
SET @cor5 = SCOPE_IDENTITY();

INSERT dbo.PartyCategoryLink (PartyId, CategoryId) VALUES
    (@cor1, @catSupplier), (@cor4, @catSupplier), (@cor6, @catSupplier), (@cor2, @catGov);

-- Адреси (Kind: 1 — юридична, 2 — фактична, 3 — поштова)
INSERT dbo.PartyAddress (PartyId, Kind, Country, PostalCode, Region, City, Line) VALUES
    (@cor1, 1, N'Україна', N'61002', N'Харківська', N'Харків', N'вул. Сумська, 10'),
    (@cor1, 3, N'Україна', N'61002', N'Харківська', N'Харків', N'вул. Сумська, 10, оф. 4'),
    (@cor2, 1, N'Україна', N'61022', N'Харківська', N'Харків', N'пл. Свободи, 5'),
    (@cor3, 3, N'Україна', N'61000', N'Харківська', N'Харків', N'вул. Полтавський шлях, 44'),
    (@cor4, 1, N'Україна', N'01001', NULL,          N'Київ',   N'вул. Хрещатик, 1'),
    (@cor4Branch, 2, N'Україна', N'61057', N'Харківська', N'Харків', N'вул. Пушкінська, 32'),
    (@cor6, 1, N'Україна', N'62300', N'Харківська', N'Дергачі', N'вул. Садова, 3'),
    (@cor5, 1, N'Україна', N'49000', N'Дніпропетровська', N'Дніпро', N'просп. Яворницького, 7');

-- Контакти (Kind: 1 — телефон, 2 — email, 3 — сайт, 4 — адреса для обміну документами)
INSERT dbo.PartyContact (PartyId, Kind, Value, IsPrimary) VALUES
    (@cor1, 1, N'057-700-10-20', 1),
    (@cor1, 2, N'office@promtechbud.example', 1),
    (@cor1, 3, N'https://promtechbud.example', 0),
    (@cor2, 2, N'osvita@kharkiv-oda.example', 1),
    (@cor2, 4, N'СЕВ ОВВ: 02142632', 0),
    (@cor3, 1, N'067-123-45-67', 1),
    (@cor4, 1, N'044-200-00-01', 1),
    (@cor4, 2, N'info@energopostach.example', 1),
    (@cor6, 1, N'050-111-22-33', 1);

INSERT dbo.PartyPerson (PartyId, FullName, PositionName, Phone, IsSignatory) VALUES
    (@cor1, N'Гнатюк Олег Петрович', N'Директор', N'057-700-10-21', 1),
    (@cor1, N'Литвин Ірина Сергіївна', N'Головний бухгалтер', N'057-700-10-22', 0),
    (@cor2, N'Бойко Наталія Іванівна', N'Директор департаменту', NULL, 1),
    (@cor4, N'Мороз Василь Васильович', N'Начальник юридичного відділу', NULL, 0);

INSERT dbo.PartyBankAccount (PartyId, Iban, BankId, Currency, Purpose, IsPrimary) VALUES
    (@cor1, 'UA183052990000026007052612345', @bankPrivat, 'UAH', N'Поточний', 1),
    (@cor1, 'UA963052990000026003052698765', @bankPrivat, 'USD', N'Поточний валютний', 1),
    (@cor2, 'UA368201720000035413001012345', @bankTreasury, 'UAH', N'Бюджетний', 1),
    (@cor4, 'UA433004650000026004500123456', @bankOschad, 'UAH', N'Поточний', 1),
    (@cor6, 'UA693220010000026009310011223', @bankUniversal, 'UAH', N'Поточний ФОП', 1),
    (@cor5, 'UA943808050000026003380077777', @bankRaiff, 'UAH', N'Поточний', 1);

-- ---- Документи (історію статусів пише тригер trg_Document_StatusHistory) ----
DECLARE @doc1 int, @doc2 int, @doc3 int, @doc4 int, @doc5 int, @doc6 int, @doc7 int;

INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate, IsControlled)
VALUES (@org1, @dIn, @tLetter, @sExec, @chMail, @cor1, @legal, @e1, N'Лист щодо укладення договору постачання', N'Пропозиція про співпрацю та укладення договору постачання матеріалів.', '2026-05-12', 1);
SET @doc1 = SCOPE_IDENTITY();

INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate, IsControlled)
VALUES (@org1, @dIn, @tRequest, @sReview, @chEmail, @cor2, @hr, @e1, N'Запит про надання інформації', N'Запит щодо кількості працівників із вищою освітою.', '2026-05-20', 1);
SET @doc2 = SCOPE_IDENTITY();

INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, SenderDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate, IsControlled)
VALUES (@org1, @dOut, @tLetter, @sDone, @chMail, @cor4, @acc, @e5, N'Відповідь на претензію щодо оплати', N'Відповідь на претензію АТ «Енергопостач» щодо заборгованості.', '2026-05-22', 0);
SET @doc3 = SCOPE_IDENTITY();

INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, SenderDepartmentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate, IsControlled)
VALUES (@org1, @dInt, @tMemo, @sReg, @chPersonal, @hr, @admin, @e4, N'Службова записка про преміювання працівників', N'Пропозиція щодо преміювання за результатами кварталу.', '2026-06-01', 0);
SET @doc4 = SCOPE_IDENTITY();

INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate, IsControlled)
VALUES (@org1, @dIn, @tAppeal, @sExec, @chPersonal, @cor3, @admin, @e1, N'Звернення громадянина щодо особистого прийому', N'Звернення з проханням про особистий прийом керівником.', '2026-06-05', 1);
SET @doc5 = SCOPE_IDENTITY();

INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, SenderDepartmentId, AuthorEmployeeId, Title, DocumentDate, IsControlled)
VALUES (@org1, @dOut, @tOrder, @sArch, @chMail, @cor1, @admin, @e2, N'Наказ про затвердження плану заходів', '2026-04-30', 0);
SET @doc6 = SCOPE_IDENTITY();

-- Документ іншої організації (для перевірки тенант-ізоляції)
INSERT dbo.Document (OrganizationId, DirectionId, DocumentTypeId, CurrentStatusId, DeliveryChannelId, CorrespondentId, ReceiverDepartmentId, AuthorEmployeeId, Title, Summary, DocumentDate, IsControlled)
VALUES (@org2, @dIn, @tLetter, @sReg, @chEmail, @cor5, @gen2, @e7, N'Лист про співпрацю з логістики', N'Пропозиція логістичних послуг для ТОВ «Вектор».', '2026-06-10', 1);
SET @doc7 = SCOPE_IDENTITY();

-- ---- Реєстраційні записи: номери генеруються функцією dbo.fn_FormatRegistrationNumber
--      за патерном організації (лічильник за організацією + типом + роком) ----
INSERT dbo.RegistrationRecord (DocumentId, RegistrarEmployeeId, RegistrationNumber, RegistrationYear, OrganizationId, SequenceNumber, RegisteredAt)
SELECT x.DocumentId, x.RegistrarId,
       dbo.fn_FormatRegistrationNumber(o.RegistrationNumberPattern, x.Seq, 2026, t.Code, dir.Code),
       2026, d.OrganizationId, x.Seq, x.RegisteredAt
FROM
(
    SELECT v.DocumentId, v.RegistrarId, CAST(v.RegisteredAt AS datetime2) AS RegisteredAt,
           ROW_NUMBER() OVER (PARTITION BY dd.OrganizationId, dd.DocumentTypeId ORDER BY v.DocumentId) AS Seq
    FROM (VALUES
            (@doc1, @e1, '2026-05-12T09:30:00'),
            (@doc2, @e1, '2026-05-20T09:30:00'),
            (@doc3, @e1, '2026-05-22T09:30:00'),
            (@doc4, @e1, '2026-06-01T09:30:00'),
            (@doc5, @e1, '2026-06-05T09:30:00'),
            (@doc6, @e1, '2026-04-30T09:30:00'),
            (@doc7, @e7, '2026-06-10T09:30:00')
         ) v (DocumentId, RegistrarId, RegisteredAt)
    JOIN dbo.Document dd ON dd.Id = v.DocumentId
) x
JOIN dbo.Document d            ON d.Id = x.DocumentId
JOIN dbo.Organization o        ON o.Id = d.OrganizationId
JOIN dbo.DocumentType t        ON t.Id = d.DocumentTypeId
JOIN dbo.DocumentDirection dir ON dir.Id = d.DirectionId;

-- ---- Резолюції та доручення ----
DECLARE @res1 int, @res2 int, @res5 int;

INSERT dbo.Resolution (DocumentId, AuthorEmployeeId, ResolutionText, ResolutionDate, DueDate, IsControlled)
VALUES (@doc1, @e2, N'Підготувати проєкт договору', '2026-05-13', '2026-05-25', 1);
SET @res1 = SCOPE_IDENTITY();
INSERT dbo.ExecutionTask (ResolutionId, DocumentId, ExecutorEmployeeId, TaskStatusId, TaskText, AssignedAt, DueDate)
VALUES (@res1, @doc1, @e3, @tsInProgress, N'Опрацювати умови та підготувати проєкт договору постачання', '2026-05-13T09:30:00', '2026-05-25');
INSERT dbo.ExecutionTask (ResolutionId, DocumentId, ExecutorEmployeeId, TaskStatusId, TaskText, AssignedAt, DueDate, CompletedAt, ResultText)
VALUES (@res1, @doc1, @e5, @tsDone, N'Погодити фінансові умови договору', '2026-05-13T09:30:00', '2026-05-20', '2026-05-19T09:30:00', N'Умови погоджено');

INSERT dbo.Resolution (DocumentId, AuthorEmployeeId, ResolutionText, ResolutionDate, DueDate, IsControlled)
VALUES (@doc2, @e2, N'Надати відповідь', '2026-05-21', '2026-05-28', 1);
SET @res2 = SCOPE_IDENTITY();
INSERT dbo.ExecutionTask (ResolutionId, DocumentId, ExecutorEmployeeId, TaskStatusId, TaskText, AssignedAt, DueDate)
VALUES (@res2, @doc2, @e4, @tsOverdue, N'Підготувати довідку про кількість працівників із вищою освітою', '2026-05-21T09:30:00', '2026-05-28');

INSERT dbo.Resolution (DocumentId, AuthorEmployeeId, ResolutionText, ResolutionDate, DueDate, IsControlled)
VALUES (@doc5, @e2, N'Розглянути звернення', '2026-06-06', '2026-06-20', 1);
SET @res5 = SCOPE_IDENTITY();
INSERT dbo.ExecutionTask (ResolutionId, DocumentId, ExecutorEmployeeId, TaskStatusId, TaskText, AssignedAt, DueDate)
VALUES (@res5, @doc5, @e2, @tsAssigned, N'Розглянути звернення та призначити дату прийому', '2026-06-06T09:30:00', '2026-06-20');

COMMIT TRANSACTION;
GO
