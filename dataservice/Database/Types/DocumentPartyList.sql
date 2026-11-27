/* Табличний тип: додаткові адресати й копії документа
   (процедури dbo.usp_Document_Create / dbo.usp_Document_Update). Role: 1 — адресат, 2 — копія. */
IF TYPE_ID(N'dbo.DocumentPartyList') IS NULL
    CREATE TYPE dbo.DocumentPartyList AS TABLE
    (
        RowNo   int     NOT NULL PRIMARY KEY,   -- порядок у списку
        PartyId int     NOT NULL,
        Role    tinyint NOT NULL
    );
GO
