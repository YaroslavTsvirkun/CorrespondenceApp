/* =====================================================================
   005_RevokedTokens.sql — відкликані JWT (вихід із системи).
   TokenId — claim jti; ExpiresAtUtc — коли токен однаково закінчився б
   (після цього запис не потрібен і видаляється). Логіка — RevokeTokenHandler, MeHandler.
   ===================================================================== */

IF OBJECT_ID(N'dbo.RevokedToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RevokedToken
    (
        TokenId      nvarchar(64) NOT NULL CONSTRAINT PK_RevokedToken PRIMARY KEY,
        UserId       int          NOT NULL
            CONSTRAINT FK_RevokedToken_AppUser_UserId REFERENCES dbo.AppUser (Id) ON DELETE CASCADE,
        ExpiresAtUtc datetime2    NOT NULL,
        RevokedAtUtc datetime2    NOT NULL CONSTRAINT DF_RevokedToken_RevokedAtUtc DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_RevokedToken_ExpiresAtUtc ON dbo.RevokedToken (ExpiresAtUtc);
END;
GO
