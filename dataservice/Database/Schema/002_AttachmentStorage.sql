/* Вкладення документів в об'єктному сховищі (MinIO / S3).
   Сам файл лежить у бакеті під ключем StorageKey, у БД — лише метадані.
   Скрипт ідемпотентний: кожна зміна перевіряє поточний стан таблиці. */

-- FilePath -> StorageKey: ключ об'єкта в бакеті ({організація}/{рік}/{місяць}/{guid}),
-- ім'я файлу від користувача в ключ не потрапляє.
IF COL_LENGTH(N'dbo.Attachment', N'FilePath') IS NOT NULL
   AND COL_LENGTH(N'dbo.Attachment', N'StorageKey') IS NULL
    EXEC sp_rename N'dbo.Attachment.FilePath', N'StorageKey', N'COLUMN';
GO

-- FileType nvarchar(max) -> ContentType nvarchar(127) (MIME-тип)
IF COL_LENGTH(N'dbo.Attachment', N'FileType') IS NOT NULL
   AND COL_LENGTH(N'dbo.Attachment', N'ContentType') IS NULL
    EXEC sp_rename N'dbo.Attachment.FileType', N'ContentType', N'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.Attachment') AND name = N'ContentType' AND max_length = -1)
BEGIN
    UPDATE dbo.Attachment SET ContentType = N'application/octet-stream'
    WHERE ContentType IS NULL OR LEN(ContentType) > 127;
    ALTER TABLE dbo.Attachment ALTER COLUMN ContentType nvarchar(127) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Attachment', N'SizeBytes') IS NULL
    ALTER TABLE dbo.Attachment ADD SizeBytes bigint NOT NULL
        CONSTRAINT DF_Attachment_SizeBytes DEFAULT (0);
GO

-- SHA-256 вмісту: контроль цілісності файлу в сховищі
IF COL_LENGTH(N'dbo.Attachment', N'Sha256') IS NULL
    ALTER TABLE dbo.Attachment ADD Sha256 binary(32) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.Attachment') AND name = N'UX_Attachment_StorageKey')
    CREATE UNIQUE INDEX UX_Attachment_StorageKey ON dbo.Attachment (StorageKey);
GO
