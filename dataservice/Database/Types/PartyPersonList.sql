/* Табличний тип: контактні особи контрагента (процедура dbo.usp_Party_Save). */
IF TYPE_ID(N'dbo.PartyPersonList') IS NULL
    CREATE TYPE dbo.PartyPersonList AS TABLE
    (
        RowNo        int           NOT NULL PRIMARY KEY,
        FullName     nvarchar(150) NOT NULL,
        PositionName nvarchar(150) NULL,
        Phone        nvarchar(50)  NULL,
        Email        nvarchar(100) NULL,
        IsSignatory  bit           NOT NULL,
        Note         nvarchar(200) NULL
    );
GO
