/* =====================================================================
   007_DocumentParties.sql — контрагенти документа.
   Одноразовий скрипт:
     1. dbo.Document.CorrespondentName — назва кореспондента на момент
        реєстрації (знімок): перейменування контрагента не змінює
        старих документів. Для наявних документів заповнюється поточною
        назвою; NULL — знімка немає, показується поточна назва;
     2. dbo.DocumentParty — додаткові адресати й копії документа
        (головний кореспондент лишається в dbo.Document.CorrespondentId).
   ===================================================================== */
IF COL_LENGTH(N'dbo.Document', N'CorrespondentName') IS NULL
    ALTER TABLE dbo.Document ADD CorrespondentName nvarchar(300) NULL;
GO

UPDATE d SET CorrespondentName = COALESCE(p.ShortName, p.FullName)
FROM dbo.Document d
JOIN dbo.Party p ON p.Id = d.CorrespondentId
WHERE d.CorrespondentName IS NULL;
GO

IF OBJECT_ID(N'dbo.DocumentParty', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentParty
    (
        Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentParty PRIMARY KEY,
        DocumentId int           NOT NULL
            CONSTRAINT FK_DocumentParty_Document_DocumentId REFERENCES dbo.Document (Id) ON DELETE CASCADE,
        -- Контрагента з документів не видаляють (див. dbo.usp_Party_Delete)
        PartyId    int           NOT NULL
            CONSTRAINT FK_DocumentParty_Party_PartyId REFERENCES dbo.Party (Id),
        -- 1 — адресат, 2 — копія
        Role       tinyint       NOT NULL CONSTRAINT CK_DocumentParty_Role CHECK (Role IN (1, 2)),
        PartyName  nvarchar(300) NOT NULL,   -- назва на момент додавання (знімок)
        SortOrder  int           NOT NULL
    );
    CREATE UNIQUE INDEX IX_DocumentParty_DocumentId_PartyId ON dbo.DocumentParty (DocumentId, PartyId);
    CREATE INDEX IX_DocumentParty_PartyId ON dbo.DocumentParty (PartyId);
END;
GO
