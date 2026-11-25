/* =====================================================================
   004_LoginLockout.sql — тимчасове блокування облікового запису після
   серії невдалих спроб входу (захист від перебору паролів).
   FailedLoginCount — невдалі спроби поспіль; LockoutEndUtc — до якого
   моменту (UTC) вхід заборонено. Логіка — VerifyCredentialsHandler.
   ===================================================================== */

IF COL_LENGTH(N'dbo.AppUser', N'FailedLoginCount') IS NULL
    ALTER TABLE dbo.AppUser
        ADD FailedLoginCount int NOT NULL CONSTRAINT DF_AppUser_FailedLoginCount DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.AppUser', N'LockoutEndUtc') IS NULL
    ALTER TABLE dbo.AppUser ADD LockoutEndUtc datetime2 NULL;
GO
