/* =====================================================================
   006_Parties.sql — реєстр контрагентів замість довідника кореспондентів.
   Одноразовий скрипт:
     1. створює таблиці контрагентів (dbo.Party і дочірні: адреси, контакти,
        контактні особи, категорії, банківські рахунки) та довідники
        (правові форми, банки);
     2. переносить наявних кореспондентів у dbo.Party з тими самими Id
        (адреса, телефон, email, контактна особа -> дочірні таблиці;
        тип -> вид контрагента, нестандартні типи -> категорії);
     3. переводить зовнішній ключ dbo.Document.CorrespondentId на dbo.Party
        і видаляє dbo.Correspondent.
   Таблицю dbo.CorrespondentType видаляє Seed/006_Parties.sql — після того, як
   її заповнить Seed/001_ReferenceData.sql у новій БД.
   ===================================================================== */
-- ---------- Довідники ----------
CREATE TABLE dbo.LegalForm
(
    Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LegalForm PRIMARY KEY,
    Name        nvarchar(100) NOT NULL,          -- Товариство з обмеженою відповідальністю
    Code        nvarchar(20)  NULL,              -- скорочення: ТОВ
    Description nvarchar(max) NULL
);
CREATE UNIQUE INDEX IX_LegalForm_Name ON dbo.LegalForm (Name);
GO

CREATE TABLE dbo.PartyCategory
(
    Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyCategory PRIMARY KEY,
    Name        nvarchar(100) NOT NULL,
    Description nvarchar(max) NULL
);
CREATE UNIQUE INDEX IX_PartyCategory_Name ON dbo.PartyCategory (Name);
GO

-- Довідник банків України (МФО — 6 цифр; входить до IBAN на позиціях 5–10)
CREATE TABLE dbo.Bank
(
    Id        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bank PRIMARY KEY,
    Mfo       char(6)       NOT NULL,
    Name      nvarchar(200) NOT NULL,
    ShortName nvarchar(100) NULL,
    Edrpou    char(8)       NULL,
    SwiftBic  varchar(11)   NULL,
    IsActive  bit           NOT NULL CONSTRAINT DF_Bank_IsActive DEFAULT (1)
);
CREATE UNIQUE INDEX IX_Bank_Mfo ON dbo.Bank (Mfo);
GO

-- ---------- Контрагент ----------
CREATE TABLE dbo.Party
(
    Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Party PRIMARY KEY,
    OrganizationId int           NOT NULL
        CONSTRAINT FK_Party_Organization_OrganizationId REFERENCES dbo.Organization (Id),
    -- 1 — юридична особа, 2 — ФОП, 3 — фізична особа, 4 — орган влади / державна установа
    Kind          tinyint       NOT NULL CONSTRAINT CK_Party_Kind CHECK (Kind BETWEEN 1 AND 4),
    FullName      nvarchar(300) NOT NULL,
    ShortName     nvarchar(150) NULL,
    -- ЄДРПОУ (8 цифр) для юросіб і органів влади, РНОКПП (10 цифр) для ФОП і фізосіб
    Code          varchar(10)   NULL CONSTRAINT CK_Party_Code CHECK (Code NOT LIKE '%[^0-9]%'),
    LegalFormId   int           NULL
        CONSTRAINT FK_Party_LegalForm_LegalFormId REFERENCES dbo.LegalForm (Id),
    -- Філія / відокремлений підрозділ -> головна юридична особа
    ParentPartyId int           NULL
        CONSTRAINT FK_Party_Party_ParentPartyId REFERENCES dbo.Party (Id),
    IsActive      bit           NOT NULL CONSTRAINT DF_Party_IsActive DEFAULT (1),
    Note          nvarchar(max) NULL,
    CreatedAt     datetime2     NOT NULL CONSTRAINT DF_Party_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Party_OrganizationId ON dbo.Party (OrganizationId);
CREATE INDEX IX_Party_ParentPartyId ON dbo.Party (ParentPartyId);
-- Один код — один контрагент в організації (у філій код буває спільним з головною — тому лише для головних)
CREATE UNIQUE INDEX IX_Party_OrganizationId_Code ON dbo.Party (OrganizationId, Code)
    WHERE Code IS NOT NULL AND ParentPartyId IS NULL;
GO

CREATE TABLE dbo.PartyCategoryLink
(
    PartyId    int NOT NULL CONSTRAINT FK_PartyCategoryLink_Party_PartyId REFERENCES dbo.Party (Id) ON DELETE CASCADE,
    CategoryId int NOT NULL CONSTRAINT FK_PartyCategoryLink_PartyCategory_CategoryId REFERENCES dbo.PartyCategory (Id),
    CONSTRAINT PK_PartyCategoryLink PRIMARY KEY (PartyId, CategoryId)
);
CREATE INDEX IX_PartyCategoryLink_CategoryId ON dbo.PartyCategoryLink (CategoryId);
GO

CREATE TABLE dbo.PartyAddress
(
    Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyAddress PRIMARY KEY,
    PartyId    int           NOT NULL CONSTRAINT FK_PartyAddress_Party_PartyId REFERENCES dbo.Party (Id) ON DELETE CASCADE,
    -- 1 — юридична, 2 — фактична, 3 — поштова
    Kind       tinyint       NOT NULL CONSTRAINT CK_PartyAddress_Kind CHECK (Kind BETWEEN 1 AND 3),
    Country    nvarchar(100) NULL,
    PostalCode nvarchar(10)  NULL,
    Region     nvarchar(100) NULL,
    City       nvarchar(100) NULL,
    Line       nvarchar(300) NULL     -- вулиця, будинок, офіс (або вся адреса одним рядком)
);
CREATE INDEX IX_PartyAddress_PartyId ON dbo.PartyAddress (PartyId);
GO

CREATE TABLE dbo.PartyContact
(
    Id        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyContact PRIMARY KEY,
    PartyId   int           NOT NULL CONSTRAINT FK_PartyContact_Party_PartyId REFERENCES dbo.Party (Id) ON DELETE CASCADE,
    -- 1 — телефон, 2 — email, 3 — сайт, 4 — електронна адреса для обміну документами (СЕВ ОВВ тощо)
    Kind      tinyint       NOT NULL CONSTRAINT CK_PartyContact_Kind CHECK (Kind BETWEEN 1 AND 4),
    Value     nvarchar(200) NOT NULL,
    IsPrimary bit           NOT NULL CONSTRAINT DF_PartyContact_IsPrimary DEFAULT (0),
    Note      nvarchar(200) NULL
);
CREATE INDEX IX_PartyContact_PartyId ON dbo.PartyContact (PartyId);
GO

CREATE TABLE dbo.PartyPerson
(
    Id           int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyPerson PRIMARY KEY,
    PartyId      int           NOT NULL CONSTRAINT FK_PartyPerson_Party_PartyId REFERENCES dbo.Party (Id) ON DELETE CASCADE,
    FullName     nvarchar(150) NOT NULL,
    PositionName nvarchar(150) NULL,
    Phone        nvarchar(50)  NULL,
    Email        nvarchar(100) NULL,
    IsSignatory  bit           NOT NULL CONSTRAINT DF_PartyPerson_IsSignatory DEFAULT (0),   -- має право підпису
    Note         nvarchar(200) NULL
);
CREATE INDEX IX_PartyPerson_PartyId ON dbo.PartyPerson (PartyId);
GO

CREATE TABLE dbo.PartyBankAccount
(
    Id                int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyBankAccount PRIMARY KEY,
    PartyId           int           NOT NULL CONSTRAINT FK_PartyBankAccount_Party_PartyId REFERENCES dbo.Party (Id) ON DELETE CASCADE,
    Iban              varchar(34)   NOT NULL,   -- без пробілів, великими літерами
    -- Банк з довідника (для українських IBAN — за МФО з IBAN); NULL — іноземний або невідомий банк
    BankId            int           NULL CONSTRAINT FK_PartyBankAccount_Bank_BankId REFERENCES dbo.Bank (Id),
    BankName          nvarchar(200) NULL,       -- назва банку, якщо його немає в довіднику
    SwiftBic          varchar(11)   NULL,
    Currency          char(3)       NOT NULL CONSTRAINT DF_PartyBankAccount_Currency DEFAULT ('UAH'),
    Purpose           nvarchar(100) NULL,       -- поточний, бюджетний, депозитний…
    AccountHolderName nvarchar(300) NULL,       -- якщо власник рахунку відрізняється від контрагента
    IsPrimary         bit           NOT NULL CONSTRAINT DF_PartyBankAccount_IsPrimary DEFAULT (0),
    ClosedAt          date          NULL,       -- закритий рахунок лишається в історії
    Note              nvarchar(200) NULL
);
CREATE UNIQUE INDEX IX_PartyBankAccount_PartyId_Iban ON dbo.PartyBankAccount (PartyId, Iban);
-- Не більше одного основного відкритого рахунку на валюту
CREATE UNIQUE INDEX IX_PartyBankAccount_Primary ON dbo.PartyBankAccount (PartyId, Currency)
    WHERE IsPrimary = 1 AND ClosedAt IS NULL;
CREATE INDEX IX_PartyBankAccount_BankId ON dbo.PartyBankAccount (BankId);
GO

-- ---------- Перенесення кореспондентів (увесь скрипт деплоєр виконує в одній транзакції) ----------
-- Типи, що відповідають видам контрагента; решта (додані користувачами) стають категоріями
DECLARE @Kinds TABLE (TypeName nvarchar(100) PRIMARY KEY, Kind tinyint);
INSERT @Kinds VALUES (N'Організація', 1), (N'Фізична особа', 3), (N'Державна установа', 4);

INSERT dbo.PartyCategory (Name, Description)
SELECT t.Name, t.Description
FROM dbo.CorrespondentType t
WHERE NOT EXISTS (SELECT 1 FROM @Kinds k WHERE k.TypeName = t.Name)
  AND EXISTS (SELECT 1 FROM dbo.Correspondent c WHERE c.CorrespondentTypeId = t.Id);

SET IDENTITY_INSERT dbo.Party ON;
INSERT dbo.Party (Id, OrganizationId, Kind, FullName, Note)
SELECT c.Id, c.OrganizationId, COALESCE(k.Kind, 1), c.Name, c.Note
FROM dbo.Correspondent c
JOIN dbo.CorrespondentType t ON t.Id = c.CorrespondentTypeId
LEFT JOIN @Kinds k ON k.TypeName = t.Name;
SET IDENTITY_INSERT dbo.Party OFF;

INSERT dbo.PartyCategoryLink (PartyId, CategoryId)
SELECT c.Id, pc.Id
FROM dbo.Correspondent c
JOIN dbo.CorrespondentType t ON t.Id = c.CorrespondentTypeId
JOIN dbo.PartyCategory pc ON pc.Name = t.Name;

-- Адреса кореспондента — поштова, одним рядком
INSERT dbo.PartyAddress (PartyId, Kind, Line)
SELECT c.Id, 3, LEFT(LTRIM(RTRIM(c.Address)), 300)
FROM dbo.Correspondent c WHERE NULLIF(LTRIM(RTRIM(c.Address)), N'') IS NOT NULL;

INSERT dbo.PartyContact (PartyId, Kind, Value, IsPrimary)
SELECT c.Id, 1, LEFT(LTRIM(RTRIM(c.Phone)), 200), 1
FROM dbo.Correspondent c WHERE NULLIF(LTRIM(RTRIM(c.Phone)), N'') IS NOT NULL;

INSERT dbo.PartyContact (PartyId, Kind, Value, IsPrimary)
SELECT c.Id, 2, LEFT(LTRIM(RTRIM(c.Email)), 200), 1
FROM dbo.Correspondent c WHERE NULLIF(LTRIM(RTRIM(c.Email)), N'') IS NOT NULL;

INSERT dbo.PartyPerson (PartyId, FullName)
SELECT c.Id, LEFT(LTRIM(RTRIM(c.ContactPerson)), 150)
FROM dbo.Correspondent c WHERE NULLIF(LTRIM(RTRIM(c.ContactPerson)), N'') IS NOT NULL;

-- Документ посилається на контрагента; видалити контрагента, що є в документах, не можна
ALTER TABLE dbo.Document DROP CONSTRAINT FK_Document_Correspondent_CorrespondentId;
ALTER TABLE dbo.Document ADD CONSTRAINT FK_Document_Party_CorrespondentId
    FOREIGN KEY (CorrespondentId) REFERENCES dbo.Party (Id);
CREATE INDEX IX_Document_CorrespondentId ON dbo.Document (CorrespondentId);

DROP TABLE dbo.Correspondent;
GO
