/* Табличний тип: список рядкових кодів (напр. коди пермішенів ролі). */
IF TYPE_ID(N'dbo.CodeList') IS NULL
    CREATE TYPE dbo.CodeList AS TABLE
    (
        Code nvarchar(80) NOT NULL PRIMARY KEY
    );
GO
