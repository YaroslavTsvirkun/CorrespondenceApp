/* Табличний тип: контакти контрагента (процедура dbo.usp_Party_Save).
   Kind: 1 — телефон, 2 — email, 3 — сайт, 4 — адреса для обміну документами. */
IF TYPE_ID(N'dbo.PartyContactList') IS NULL
    CREATE TYPE dbo.PartyContactList AS TABLE
    (
        RowNo     int           NOT NULL PRIMARY KEY,
        Kind      tinyint       NOT NULL,
        Value     nvarchar(200) NOT NULL,
        IsPrimary bit           NOT NULL,
        Note      nvarchar(200) NULL
    );
GO
