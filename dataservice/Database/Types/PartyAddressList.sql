/* Табличний тип: адреси контрагента (процедура dbo.usp_Party_Save).
   Kind: 1 — юридична, 2 — фактична, 3 — поштова. */
IF TYPE_ID(N'dbo.PartyAddressList') IS NULL
    CREATE TYPE dbo.PartyAddressList AS TABLE
    (
        RowNo      int           NOT NULL PRIMARY KEY,   -- порядок рядків у запиті
        Kind       tinyint       NOT NULL,
        Country    nvarchar(100) NULL,
        PostalCode nvarchar(10)  NULL,
        Region     nvarchar(100) NULL,
        City       nvarchar(100) NULL,
        Line       nvarchar(300) NULL
    );
GO
