/* =====================================================================
   008_BankAccountHistory.sql — журнал змін банківських рахунків контрагентів.
   Кожне створення, зміну й видалення рахунку записує тригер
   dbo.trg_PartyBankAccount_History: хто (SESSION_CONTEXT 'UserId'), коли,
   значення до й після (JSON). Журнал переживає видалення рахунку й
   контрагента — тому без зовнішніх ключів. Одноразовий скрипт.
   ===================================================================== */
IF OBJECT_ID(N'dbo.PartyBankAccountHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartyBankAccountHistory
    (
        Id              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyBankAccountHistory PRIMARY KEY,
        AccountId       int           NOT NULL,
        PartyId         int           NOT NULL,
        -- 1 — створено, 2 — змінено, 3 — видалено
        Action          tinyint       NOT NULL CONSTRAINT CK_PartyBankAccountHistory_Action CHECK (Action BETWEEN 1 AND 3),
        ChangedAt       datetime2     NOT NULL CONSTRAINT DF_PartyBankAccountHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
        ChangedByUserId int           NULL,   -- NULL — зміна поза застосунком (скрипт, демо-дані)
        OldValues       nvarchar(max) NULL,   -- JSON рахунку до зміни
        NewValues       nvarchar(max) NULL    -- JSON рахунку після зміни
    );
    CREATE INDEX IX_PartyBankAccountHistory_PartyId ON dbo.PartyBankAccountHistory (PartyId, ChangedAt);
    CREATE INDEX IX_PartyBankAccountHistory_AccountId ON dbo.PartyBankAccountHistory (AccountId, ChangedAt);
END;
GO
